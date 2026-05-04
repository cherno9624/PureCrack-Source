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
    /// panel tree specifically, and deploys PluginSDK.dll if it was somehow
    /// lost between runs.
    ///
    /// Called from DefaultCommand right before panel launch so any files
    /// PureCrack itself creates (BootstrapSettingsJson, IPs reorder backups)
    /// don't inadvertently carry a Zone.Identifier through a temp-file rename.
    /// </summary>
    public static void ProvisionPanel(string panelExe)
    {
        DeployPluginSdk(panelExe);
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

    /// <summary>
    /// Ensure <c>PluginSDK.dll</c> exists in <c>panel/Plugins/</c> — the
    /// directory <c>Assembly.LoadFrom</c> probes when loading PureHelper.dll.
    /// If the file is missing (operator deleted it, extraction error, or MOTW
    /// security policy blocked it), extract a clean copy from the embedded
    /// resource inside PureCrack.exe. The embedded copy is the one from
    /// <c>PureRAT_SDK/ReferencesDLL/PluginSDK.dll</c> in the SDK zip.
    /// </summary>
    private static void DeployPluginSdk(string panelExe)
    {
        var dir = Path.GetDirectoryName(panelExe);
        if (dir == null) return;

        var pluginsDir = Path.Combine(dir, "Plugins");
        var target = Path.Combine(pluginsDir, "PluginSDK.dll");
        if (File.Exists(target)) return;

        try
        {
            Directory.CreateDirectory(pluginsDir);
            var bytes = EmbeddedAssets.PluginSdkDll;
            File.WriteAllBytes(target, bytes);
            Log.Ok($"deploy: wrote PluginSDK.dll ({bytes.Length:N0}b) to panel/Plugins/");
        }
        catch (Exception ex)
        {
            Log.Warn($"deploy: can't write PluginSDK.dll to {target}: {ex.Message.TrimEnd('.')}");
        }
    }
}
