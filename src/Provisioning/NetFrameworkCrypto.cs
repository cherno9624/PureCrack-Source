using System;
using Microsoft.Win32;
using PureCrack.Util;

namespace PureCrack.Provisioning;

/// <summary>
/// Sets the .NET Framework "use strong crypto" registry keys. Without these,
/// some .NET 4.x apps default to TLS 1.0 / RC4 / SHA-1 ciphers, which fail
/// against the panel's TLS configuration on Win11 default. Equivalent to
/// what <c>Launch.ps1</c>'s <c>Set-ItemProperty SchUseStrongCrypto</c> stanza
/// does.
///
/// Two registry hives are written — the native and Wow64 mirror — so both
/// 64-bit and 32-bit .NET 4.x apps observe the setting. PureRAT itself is
/// 32-bit (the stub builds need x86), so the Wow64 path is the one that
/// actually matters; we set both for parity with the existing operator
/// runbook.
///
/// Idempotent: writes the same value every launch, no-op if already set.
/// </summary>
internal static class NetFrameworkCrypto
{
    private static readonly string[] SubKeys =
    {
        @"SOFTWARE\Microsoft\.NETFramework\v4.0.30319",
        @"SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v4.0.30319",
    };

    /// <summary>
    /// Apply the strong-crypto regkeys. Returns the count of values written
    /// (4 = both subkeys, both flags). Logs each path it touches so the
    /// operator can audit what's been changed.
    /// </summary>
    public static int Apply()
    {
        var count = 0;
        foreach (var sub in SubKeys)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(sub, writable: true);
                if (key == null)
                {
                    Log.Warn($"regkey: could not open HKLM\\{sub}");
                    continue;
                }
                key.SetValue("SchUseStrongCrypto",       1, RegistryValueKind.DWord);
                key.SetValue("SystemDefaultTlsVersions", 1, RegistryValueKind.DWord);
                count += 2;
            }
            catch (Exception ex)
            {
                Log.Warn($"regkey: HKLM\\{sub}: {ex.Message}");
            }
        }
        Log.Ok($"regkey: .NET strong-crypto applied to {count} value(s)");
        return count;
    }

    /// <summary>
    /// Inverse of <see cref="Apply"/> — used by the cleanup subcommand.
    /// Only removes the values we set, never the surrounding keys.
    /// </summary>
    public static void Remove()
    {
        foreach (var sub in SubKeys)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(sub, writable: true);
                if (key == null) continue;
                key.DeleteValue("SchUseStrongCrypto", throwOnMissingValue: false);
                key.DeleteValue("SystemDefaultTlsVersions", throwOnMissingValue: false);
            }
            catch (Exception ex)
            {
                Log.Warn($"regkey cleanup: HKLM\\{sub}: {ex.Message}");
            }
        }
        Log.Ok("regkey: .NET strong-crypto entries removed");
    }
}
