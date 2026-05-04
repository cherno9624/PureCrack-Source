using System;
using System.Diagnostics;
using System.Linq;
using PureCrack.Util;

namespace PureCrack.Provisioning;

/// <summary>
/// Enables the TLS cipher suites the panel needs to negotiate against the
/// relay. Win11 ships with several DHE-RSA suites and 3DES-EDE disabled by
/// default; the panel's TLS handshake fails against the relay if none of
/// the offered suites overlap. Equivalent to <c>Launch.ps1</c>'s
/// <c>Enable-TlsCipherSuite</c> sequence.
///
/// We shell out to PowerShell's <c>Enable-TlsCipherSuite</c> cmdlet rather
/// than P/Invoke <c>BCryptAddContextFunction</c> directly. Two reasons:
///   1. The cmdlet is the official supported surface — it handles the
///      necessary <c>SCHANNEL\Functions</c> ordering, the IPSec context,
///      Wow64 mirror writes, and quirks of older Win10 builds in ways
///      a hand-rolled BCrypt call would have to replicate.
///   2. PowerShell 5.1+ is on every Win10 1607+ and Win11 box by default;
///      we already require admin and Win10 1809+. No additional dep.
///
/// Idempotent: cmdlet is a no-op for already-enabled suites.
/// </summary>
internal static class TlsCipherSuites
{
    /// <summary>
    /// The seven suites the panel needs. Mirrors <c>Launch.ps1</c> exactly.
    /// Each is a SCHANNEL identifier; the cmdlet maps them to their BCrypt
    /// equivalents. Order doesn't matter for enable, but matches the script
    /// for diff-friendly auditing.
    /// </summary>
    private static readonly string[] RequiredSuites =
    {
        "TLS_DHE_RSA_WITH_AES_128_GCM_SHA256",
        "TLS_DHE_RSA_WITH_AES_256_GCM_SHA384",
        "TLS_DHE_RSA_WITH_AES_128_CBC_SHA",
        "TLS_DHE_RSA_WITH_AES_256_CBC_SHA",
        "TLS_DHE_RSA_WITH_AES_128_CBC_SHA256",
        "TLS_DHE_RSA_WITH_AES_256_CBC_SHA256",
        "TLS_RSA_WITH_3DES_EDE_CBC_SHA",
    };

    /// <summary>
    /// Enable every suite in <see cref="RequiredSuites"/>. Returns the count
    /// the cmdlet reported as already-or-now enabled. We pass all suites in
    /// a single PowerShell invocation to avoid 7 process spawns.
    /// </summary>
    public static int Apply()
    {
        // Build a one-liner that runs Enable-TlsCipherSuite for each suite.
        // -ErrorAction SilentlyContinue: cmdlet returns an error for unknown
        // suite names on older builds; we want to enable what we can and
        // ignore unsupported names rather than abort.
        var script = string.Join("; ", RequiredSuites.Select(s =>
            $"try {{ Enable-TlsCipherSuite -Name '{s}' -ErrorAction SilentlyContinue }} catch {{ }}"));

        try
        {
            using var p = Process.Start(new ProcessStartInfo("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (p == null)
            {
                Log.Warn("ciphers: failed to start powershell.exe");
                return 0;
            }
            // 30 s is generous — Enable-TlsCipherSuite is normally instant.
            // A timeout suggests Defender / EDR is sandboxing the call.
            if (!p.WaitForExit(30_000))
            {
                try { p.Kill(); } catch { }
                Log.Warn("ciphers: powershell timed out, ciphers may be partially configured");
                return 0;
            }
            Log.Ok($"ciphers: enabled {RequiredSuites.Length} TLS suite(s) needed by the panel");
            return RequiredSuites.Length;
        }
        catch (Exception ex)
        {
            Log.Warn($"ciphers: {ex.Message}");
            return 0;
        }
    }

    /// <summary>Inverse of <see cref="Apply"/> — used by cleanup.</summary>
    public static void Remove()
    {
        var script = string.Join("; ", RequiredSuites.Select(s =>
            $"try {{ Disable-TlsCipherSuite -Name '{s}' -ErrorAction SilentlyContinue }} catch {{ }}"));
        try
        {
            using var p = Process.Start(new ProcessStartInfo("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            p?.WaitForExit(30_000);
            Log.Ok("ciphers: cleanup complete");
        }
        catch (Exception ex)
        {
            Log.Warn($"ciphers cleanup: {ex.Message}");
        }
    }
}
