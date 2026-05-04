using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using PureCrack.Provisioning;
using PureCrack.Setup;
using PureCrack.Util;

namespace PureCrack.Cli;

/// <summary>
/// Removes every piece of state PureCrack writes to this machine:
/// hosts entries, Root store certs, data/ PFX files, HTTP.SYS bindings on
/// :443/:8443, .NET strong-crypto regkeys, and the TLS cipher suites we
/// enabled. Use this before installing other tools that bind :443 — or
/// just to leave no trace.
/// </summary>
public static class CleanupCommand
{
    public static int Run()
    {
        Log.Section("cleanup: removing PureCrack state");
        var failures = 0;

        // 1) Hosts file — remove entries we added (token-precise match)
        try { HostsManager.Remove(); }
        catch (Exception ex) { Log.Err($"hosts: {ex.Message}"); failures++; }

        // 2) Sweep Root store certs we installed
        try { SweepRootCerts(); }
        catch (Exception ex) { Log.Err($"root certs: {ex.Message}"); failures++; }

        // 3) HTTP.SYS bindings — clean :443 and :8443 (PureLogs's port too)
        foreach (var port in new[] { 443, 8443 })
        {
            try { CleanHttpSys(port); }
            catch (Exception ex) { Log.Err($"http.sys :{port}: {ex.Message}"); failures++; }
        }

        // 4) Local PFX files
        try { CertManager.Wipe(); }
        catch (Exception ex) { Log.Err($"data/: {ex.Message}"); failures++; }

        // 5) Provisioning state — undo regkeys + cipher suites
        try { Provision.Undo(); }
        catch (Exception ex) { Log.Err($"provision: {ex.Message}"); failures++; }

        if (failures == 0) Log.Ok("cleanup complete — system restored");
        else Log.Warn($"cleanup completed with {failures} non-fatal errors (see above)");
        return failures == 0 ? 0 : 1;
    }

    private static void SweepRootCerts()
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);

        var ours = new List<X509Certificate2>();
        foreach (var c in store.Certificates)
            if (CertSubjects.IsOurCert(c)) ours.Add(c);
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
            using var p = Process.Start(new ProcessStartInfo("netsh", args)
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
}
