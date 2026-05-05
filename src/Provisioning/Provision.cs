using System;
using System.IO;
using System.Reflection;
using PureCrack.Util;

namespace PureCrack.Provisioning;

public static class Provision
{
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
    /// Run right before panel launch. Re-strips MOTW over the panel tree
    /// and manages PluginSDK.dll placement.
    /// </summary>
    public static void ProvisionPanel(string panelExe)
    {
        FixPluginSdkPlacement(panelExe);
        var dir = Path.GetDirectoryName(panelExe);
        if (dir != null) MotwStripper.StripPanel(dir);
    }

    public static void Undo()
    {
        Log.Section("provision: undo");
        NetFrameworkCrypto.Remove();
        TlsCipherSuites.Remove();
    }

    // ============================================================================
    // PluginSDK placement
    // ============================================================================
    //
    // PureRAT's panel has PluginSDK types ILMerged into its managed assembly.
    // Deploying an external PluginSDK.dll creates a SECOND copy of every type
    // (same name and namespace, different assembly identity). When the panel
    // checks PureHelper.ServerPlugin with typeof(ICustomPlugin), and both
    // ICustomPlugin types come from different assemblies, IsAssignableFrom
    // returns false → "No ICustomPlugin Implementation found".
    //
    // The default strategy is therefore: DON'T deploy PluginSDK.dll. The
    // panel's internal AssemblyResolve handler provides PluginSDK when
    // PureHelper.dll needs it. Only deploy from embedded if the operator
    // sets PURECRACK_PLUGINSDK=force (last-resort fallback).
    // ============================================================================

    private static void FixPluginSdkPlacement(string panelExe)
    {
        var dir = Path.GetDirectoryName(panelExe);
        if (dir == null) return;

        var pluginsDir = Path.Combine(dir, "Plugins");
        var loadFromPath = Path.Combine(pluginsDir, "PluginSDK.dll");
        var appBasePath = Path.Combine(dir, "PluginSDK.dll");

        // ALWAYS clean up stale copies from previous versions.
        // These were deployed by the old code and must go.
        foreach (var stale in new[] { loadFromPath, appBasePath })
        {
            if (!File.Exists(stale)) continue;
            try
            {
                File.Delete(stale);
                Log.Info($"plugin-sdk: removed stale {Path.GetFileName(stale)} from " +
                         $"{Path.GetDirectoryName(stale)?.Replace(dir, "panel") ?? "panel"}");
            }
            catch (Exception ex)
            {
                Log.Warn($"plugin-sdk: can't remove {stale}: {ex.Message}");
            }
        }

        // Check env-var override.
        var envOverride = Environment.GetEnvironmentVariable("PURECRACK_PLUGINSDK");
        if (string.IsNullOrEmpty(envOverride))
        {
            // Default: no deployment. Panel resolves PluginSDK internally.
            return;
        }

        if (string.Equals(envOverride, "force", StringComparison.OrdinalIgnoreCase))
        {
            DeployTo(appBasePath);
            return;
        }

        if (string.Equals(envOverride, "none", StringComparison.OrdinalIgnoreCase))
        {
            // Explicit none — already cleaned above, nothing more to do.
            return;
        }

        // Custom path.
        DeployTo(envOverride);
    }

    private static void DeployTo(string target)
    {
        if (File.Exists(target)) return;

        try
        {
            var targetDir = Path.GetDirectoryName(target);
            if (targetDir != null && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            var bytes = EmbeddedAssets.PluginSdkDll;
            File.WriteAllBytes(target, bytes);
            Log.Ok($"plugin-sdk: deployed to {target} ({bytes.Length:N0}b)");

            // Verify the written file is a valid .NET assembly.
            try
            {
                var name = AssemblyName.GetAssemblyName(target);
                Log.Bullet($"plugin-sdk: verified {name.Name} v{name.Version}");
            }
            catch (Exception ex)
            {
                Log.Warn($"plugin-sdk: deployed but verification failed: {ex.Message.TrimEnd('.')}");
                Log.Warn("plugin-sdk: file may be corrupt or blocked by MOTW — try re-extracting the kit");
            }

            // Strip MOTW from the newly deployed file immediately.
            var adsPath = target + ":Zone.Identifier";
            try { if (File.Exists(adsPath)) File.Delete(adsPath); }
            catch { /* best-effort */ }
        }
        catch (Exception ex)
        {
            Log.Warn($"plugin-sdk: can't deploy to {target}: {ex.Message.TrimEnd('.')}");
        }
    }
}
