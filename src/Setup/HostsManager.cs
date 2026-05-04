using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PureCrack.Util;

namespace PureCrack.Setup;

/// <summary>
/// Manages <c>C:\Windows\System32\drivers\etc\hosts</c> so the panel's licence
/// API endpoints (<c>api*.purecoder.io</c>) resolve to <c>127.0.0.1</c> and
/// land on our relay. Idempotent — safe to call every launch.
///
/// Backs up the hosts file once (only on first modification) so the user
/// can recover the original if they ever uninstall.
/// </summary>
public static class HostsManager
{
    private const string HostsPath = @"C:\Windows\System32\drivers\etc\hosts";
    private const string BackupSuffix = ".purecrack-backup";

    /// <summary>
    /// Domains we redirect to loopback. Mirrors what <c>Launch.ps1</c> set up.
    /// Includes the geographic mirrors (<c>us./eu.</c>) on both <c>.io</c> and
    /// <c>.su</c> TLDs that PureCoder migrated some traffic to — without these
    /// the panel can fall through to a non-loopback DNS lookup and reach the
    /// real upstream. The relay's TLS cert SAN list (in <c>CertManager</c>)
    /// covers all of <c>*.purecoder.io</c>; the <c>.su</c> entries are
    /// hosts-blocked but not cert-served, since PureLogs runs on a separate
    /// listener with its own cert that this kit doesn't manage.
    /// </summary>
    public static readonly string[] Domains =
    {
        "api.purecoder.io",
        "api1.purecoder.io",
        "api2.purecoder.io",
        "us.purecoder.io",
        "eu.purecoder.io",
        "us.purecoder.su",
        "eu.purecoder.su",
    };

    public static string Path => HostsPath;
    public static string BackupPath => HostsPath + BackupSuffix;

    /// <summary>
    /// Add <c>127.0.0.1 api*.purecoder.io</c> entries if missing, then flush DNS.
    /// Returns true if any change was made (false = already configured).
    /// </summary>
    public static bool Ensure()
    {
        if (!File.Exists(HostsPath))
            throw new FileNotFoundException($"{HostsPath} missing — Windows install looks broken");

        var content = File.ReadAllText(HostsPath);
        var present = ScanPresent(content);
        var missing = Domains.Where(d => !present.Contains(d)).ToList();

        if (missing.Count == 0)
        {
            Log.Info($"hosts: all {Domains.Length} entries already present");
            return false;
        }

        // Back up exactly once. After that, the backup is a snapshot of the
        // original; we don't overwrite it on subsequent edits.
        if (!File.Exists(BackupPath))
        {
            File.Copy(HostsPath, BackupPath);
            Log.Bullet($"hosts: backup saved to {BackupPath}");
        }

        var newContent = EnsureTrailingNewline(content);
        foreach (var d in missing)
        {
            newContent += $"127.0.0.1 {d}\n";
            Log.Bullet($"hosts: add 127.0.0.1 {d}");
        }
        // Atomic write so a crash mid-write can't leave the OS without DNS.
        AtomicFile.WriteAllText(HostsPath, newContent);

        FlushDns();
        return true;
    }

    /// <summary>
    /// Remove the entries we added. Useful for an uninstall flow. Doesn't touch
    /// anything we didn't add (matches by exact domain text, not a wholesale
    /// restore from backup, so user-added entries on the same lines are safe).
    /// </summary>
    public static void Remove()
    {
        if (!File.Exists(HostsPath)) return;
        var lines = File.ReadAllLines(HostsPath);
        var kept = new List<string>(lines.Length);
        var removed = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#") || trimmed.Length == 0)
            {
                kept.Add(line);
                continue;
            }
            if (Domains.Any(d => LineMapsDomainToLoopback(trimmed, d)))
            {
                removed++;
                continue;
            }
            kept.Add(line);
        }
        if (removed > 0)
        {
            AtomicFile.WriteAllText(HostsPath, string.Join("\n", kept));
            Log.Ok($"hosts: removed {removed} entries");
            FlushDns();
        }
    }

    /// <summary>
    /// True if we can successfully read+write the hosts file with current
    /// privileges. Used by preflight to surface "you need admin" early.
    /// </summary>
    public static bool IsWritable()
    {
        try
        {
            using var _ = File.Open(HostsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (IOException) { return false; }
    }

    // ------------------------------------------------------------------------
    // Internals
    // ------------------------------------------------------------------------

    private static HashSet<string> ScanPresent(string content)
    {
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in content.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            foreach (var d in Domains)
                if (LineMapsDomainToLoopback(line, d))
                    present.Add(d);
        }
        return present;
    }

    private static bool LineMapsDomainToLoopback(string line, string domain)
    {
        // Looking for "<ip> <domain>" where ip is 127.0.0.1 (or any 127.x —
        // some users use 127.0.0.2 to test). Match domain as a whole token,
        // not a substring (so api.purecoder.io doesn't accidentally match
        // something.api.purecoder.io).
        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;
        if (!parts[0].StartsWith("127.")) return false;
        for (var i = 1; i < parts.Length; i++)
        {
            var tok = parts[i];
            // Stop at trailing comment.
            if (tok.StartsWith("#")) break;
            if (string.Equals(tok, domain, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string EnsureTrailingNewline(string content) =>
        (content.Length == 0 || content[content.Length - 1] == '\n')
            ? content
            : content + "\n";

    private static void FlushDns()
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("ipconfig", "/flushdns")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            p?.WaitForExit(5000);
            Log.Bullet("hosts: dns cache flushed");
        }
        catch (Exception ex)
        {
            // Non-fatal — entries are still in hosts, lookups will work
            // after the OS's per-process resolver expires its cache.
            Log.Warn($"ipconfig /flushdns failed (non-fatal): {ex.Message}");
        }
    }
}
