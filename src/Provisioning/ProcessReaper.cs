using System;
using System.Diagnostics;
using PureCrack.Util;

namespace PureCrack.Provisioning;

/// <summary>
/// Kills any stale <c>PureRAT.exe</c> processes before launch. The panel
/// uses a process-wide named mutex; if a previous session crashed without
/// cleanup, the mutex stays held and the next launch silently fails. The
/// official "wait for the OS to clean it up" path can take minutes — much
/// faster to terminate the orphaned process directly.
///
/// Equivalent to <c>Launch.ps1</c>'s <c>Get-Process PureRAT | Stop-Process</c>.
/// Best-effort: a process we can't kill (Defender process protection, antivirus
/// shielding) just stays running and the operator gets a clear "panel didn't
/// bind :56001" message later.
/// </summary>
internal static class ProcessReaper
{
    /// <summary>
    /// Kill every running <c>PureRAT.exe</c> on the box. Returns the count
    /// terminated (after waiting briefly for graceful exit).
    /// </summary>
    public static int KillStalePanels()
    {
        var killed = 0;
        Process[] panels;
        try
        {
            panels = Process.GetProcessesByName("PureRAT");
        }
        catch (Exception ex)
        {
            Log.Warn($"reaper: enumerate failed: {ex.Message}");
            return 0;
        }

        foreach (var p in panels)
        {
            using (p)
            {
                try
                {
                    Log.Bullet($"reaper: terminating stale PureRAT (PID {p.Id})");
                    p.Kill();
                    // WaitForExit is essential — without it the launch
                    // races the kernel cleanup of the process's mutex,
                    // and the next PureRAT we spawn could see the mutex
                    // still held momentarily.
                    p.WaitForExit(5000);
                    killed++;
                }
                catch (Exception ex)
                {
                    Log.Warn($"reaper: PID {p.Id}: {ex.Message}");
                }
            }
        }

        if (killed > 0)
            Log.Ok($"reaper: killed {killed} stale PureRAT process(es)");
        else
            Log.Info("reaper: no stale PureRAT processes found");
        return killed;
    }
}
