using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using PureCrack.Setup;
using PureCrack.Util;
using PureCrack.Verify;

namespace PureCrack.Cli;

/// <summary>
/// Read-only diagnostic report of the kit's installation state on this
/// machine. No modifications. Useful as a sanity check before / after a
/// launch, or when debugging "why isn't this working".
///
/// Reports:
///   - Hosts entries present (vs expected 7)
///   - Root store certs we own (with thumbprint + expiry)
///   - data/ PFX files (size + last-modified)
///   - Asset hash drift (full <see cref="AssetVerifier"/> run)
/// </summary>
public static class DoctorCommand
{
    public static int Run()
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
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var ours = 0;
            foreach (var c in store.Certificates)
            {
                if (CertSubjects.IsOurCert(c))
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

        // 4) Asset hash drift
        var panelDir = Path.Combine(Workspace.Root, "panel");
        if (Directory.Exists(panelDir))
        {
            Log.Section("doctor: asset verify");
            var av = AssetVerifier.Verify(panelDir);
            AssetVerifier.Report(av);
        }

        Log.Ok("doctor: scan complete");
        return 0;
    }
}
