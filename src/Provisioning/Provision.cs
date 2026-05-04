using System;
using System.IO;
using PureCrack.Util;

namespace PureCrack.Provisioning;

/// <summary>
/// Orchestrates every host-environment step that <c>Launch.ps1</c> used to
/// do, plus a couple of additions. Runs at the start of every PureCrack
/// launch and is fully idempotent — second and subsequent invocations are
/// no-ops where the state is already correct.
///
/// Order matters:
///   1. <see cref="MotwStripper"/> — strip Zone.Identifier ADS first so any
///      assembly we (or the panel) load later isn't blocked by MOTW.
///   2. <see cref="NetFrameworkCrypto"/> — set strong-crypto regkeys so the
///      panel's .NET TLS calls negotiate TLS 1.2.
///   3. <see cref="TlsCipherSuites"/> — enable the suites the panel needs.
///   4. <see cref="ProcessReaper"/> — kill stale panels so our launch isn't
///      blocked by an orphaned mutex.
///   5. <see cref="TimeSanity"/> — last, just a warning; doesn't gate.
/// </summary>
public static class Provision
{
    /// <summary>
    /// Run the full provisioning pipeline. None of the steps are
    /// fatal — every one is best-effort with logged warnings. The launch
    /// keeps going even if all of them fail; preflight will still report
    /// the resulting symptoms in a more focused way.
    /// </summary>
    public static void Run()
    {
        Log.Section("provision");
        MotwStripper.Strip(Workspace.Root);
        NetFrameworkCrypto.Apply();
        TlsCipherSuites.Apply();
        ProcessReaper.KillStalePanels();
        TimeSanity.Check();
    }

    /// <summary>
    /// Run after SettingsAutoFix has written files (Settings.json, backups)
    /// and after the panel directory layout is stable. Strips MOTW from the
    /// panel tree specifically, and deploys PluginSDK.dll if configured.
    ///
    /// PluginSDK strategy (tried in order, first success wins):
    ///   1. <c>PURECRACK_PLUGINSDK=none</c>  — don't deploy, let panel resolve internally
    ///   2. <c>PURECRACK_PLUGINSDK=path</c>  — deploy to specified file path
    ///   3. (default) deploy to <c>panel/PluginSDK.dll</c> — application base,
    ///      loaded at panel startup so both panel and PureHelper share the
    ///      same ICustomPlugin type identity.
    ///
    /// Previous versions deployed to panel/Plugins/; we remove that copy
    /// because Assembly.LoadFrom probes the Plugin directory first, which
    /// loads PluginSDK AFTER the panel's internal copy, creating a type
    /// identity mismatch that causes "No ICustomPlugin Implementation found".
    ///
    /// Called from DefaultCommand right before panel launch so any files
    /// PureCrack itself creates (BootstrapSettingsJson, IPs reorder backups)
    /// don't inadvertently carry a Zone.Identifier through a temp-file rename.
    /// </summary>
    public static void ProvisionPanel(string panelExe)
    {
        FixPluginSdkPlacement(panelExe);
        var dir = Path.GetDirectoryName(panelExe);
        if (dir != null) MotwStripper.StripPanel(dir);
    }

    /// <summary>
    /// Inverse of <see cref="Run"/> — undo regkeys + ciphers.
    /// MOTW strip and process reap don't have a meaningful reverse.
    /// Used by the cleanup subcommand.
    /// </summary>
    public static void Undo()
    {
        Log.Section("provision: undo");
        NetFrameworkCrypto.Remove();
        TlsCipherSuites.Remove();
    }

    // ============================================================================
    // PluginSDK placement — multi-fallback with type identity safety
    // ============================================================================

    private static void FixPluginSdkPlacement(string panelExe)
    {
        var dir = Path.GetDirectoryName(panelExe);
        if (dir == null) return;

        var pluginsDir = Path.Combine(dir, "Plugins");
        var appBasePath = Path.Combine(dir, "PluginSDK.dll");
        var loadFromPath = Path.Combine(pluginsDir, "PluginSDK.dll");

        // Clean up any PluginSDK.dll left in Plugins/ by a previous version.
        // It creates a type-identity conflict because Assembly.LoadFrom probes
        // the Plugin directory first, loading a second PluginSDK copy after
        // the panel already has one from its internal resources.
        if (File.Exists(loadFromPath))
        {
            try
            {
                File.Delete(loadFromPath);
                Log.Info("plugin-sdk: removed conflicting copy from panel/Plugins/");
            }
            catch (Exception ex)
            {
                Log.Warn($"plugin-sdk: can't remove {loadFromPath}: {ex.Message}");
            }
        }

        // Check env-var override.
        var envOverride = Environment.GetEnvironmentVariable("PURECRACK_PLUGINSDK");
        if (!string.IsNullOrEmpty(envOverride))
        {
            if (string.Equals(envOverride, "none", StringComparison.OrdinalIgnoreCase))
            {
                // Operator explicitly requested no PluginSDK deployment.
                // Also remove the app-base copy if present.
                if (File.Exists(appBasePath))
                {
                    try { File.Delete(appBasePath); }
                    catch { /* best-effort */ }
                }
                Log.Info("plugin-sdk: PURECRACK_PLUGINSDK=none — relying on panel internal resolution");
                return;
            }

            // Operator specified a custom path. Deploy there.
            DeployTo(envOverride);
            return;
        }

        // Default: deploy to application base (panel/PluginSDK.dll).
        // Loaded early by the panel's own code before PureHelper loads,
        // ensuring both share the same PluginSDK assembly identity.
        DeployTo(appBasePath);
    }

    private static void DeployTo(string target)
    {
        if (File.Exists(target)) return;

        try
        {
            var dir = Path.GetDirectoryName(target);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var bytes = EmbeddedAssets.PluginSdkDll;
            File.WriteAllBytes(target, bytes);
            Log.Ok($"plugin-sdk: deployed to {target} ({bytes.Length:N0}b)");
        }
        catch (Exception ex)
        {
            Log.Warn($"plugin-sdk: can't deploy to {target}: {ex.Message.TrimEnd('.')}");
        }
    }
}
