using System;
using PureCrack.Util;

namespace PureCrack.Provisioning;

/// <summary>
/// Sanity-checks the system clock. The kit's TLS cert validity, capture
/// timestamps, prune TTL, and (in some BCL paths) cipher negotiation all
/// depend on a roughly-correct system time. A wildly wrong clock — VM
/// without time-sync, dead CMOS battery, deliberate test rig — produces
/// confusing failures downstream that look like protocol bugs.
///
/// We don't try to fix the clock; that's an OS responsibility. We just
/// warn loudly so a future operator chasing "why did certs suddenly stop
/// validating" knows where to look.
/// </summary>
internal static class TimeSanity
{
    // The kit's lifetime envelope. Clocks outside this window are almost
    // certainly broken (year 2099 = uninitialised CMOS; year 2003 =
    // dead battery default for some chipsets).
    private static readonly DateTime LowerBound = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpperBound = new(2050, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Read the system clock and warn if it falls outside the plausible
    /// window. Returns true when the clock is OK, false when we warned.
    /// </summary>
    public static bool Check()
    {
        var now = DateTime.UtcNow;
        if (now >= LowerBound && now <= UpperBound)
        {
            Log.Info($"clock: {now:yyyy-MM-dd HH:mm:ss}Z (in plausible window)");
            return true;
        }

        Log.Warn(
            $"clock: system time is {now:yyyy-MM-dd HH:mm:ss}Z — outside expected window " +
            $"[{LowerBound:yyyy-MM-dd}..{UpperBound:yyyy-MM-dd}]. Cert validity, " +
            "TLS negotiation, and capture timestamps may misbehave. Fix the OS clock " +
            "(w32tm /resync) before reporting any 'random' failures.");
        return false;
    }
}
