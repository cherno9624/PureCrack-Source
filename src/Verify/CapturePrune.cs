using System;
using System.IO;
using PureCrack.Util;

namespace PureCrack.Verify;

/// <summary>
/// TTL-based housekeeping on <c>runs/captures/</c>. Without this the captures
/// directory grows monotonically over the lifetime of the kit; over months
/// of operation that becomes a real disk-space issue (and slows directory
/// enumeration). The CaptureWriter already filters scanner noise; this
/// handles the legitimate-traffic accumulation.
///
/// Default TTL is 14 days, hardcoded — the kit targets stability over
/// configurability, and a constant means one fewer thing operators can
/// misconfigure in a way that bites them later.
/// </summary>
public static class CapturePrune
{
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromDays(14);

    /// <summary>
    /// Remove capture files older than the default TTL. Returns the count
    /// pruned. Best-effort — individual file delete failures are logged and
    /// skipped, never escalated.
    /// </summary>
    public static int Run() => Run(DefaultMaxAge);

    /// <summary>
    /// Remove stub files older than <paramref name="maxAge"/>. Returns the count
    /// pruned. Best-effort, same pattern as <see cref="Run()"/>.
    /// </summary>
    public static int PruneStubs(TimeSpan maxAge)
    {
        var dir = Workspace.StubsDir;
        if (!Directory.Exists(dir)) return 0;

        var cutoff = DateTime.UtcNow - maxAge;
        var pruned = 0;
        var failed = 0;

        foreach (var path in Directory.EnumerateFiles(dir, "stub_*.exe"))
        {
            try
            {
                var info = new FileInfo(path);
                if (info.LastWriteTimeUtc < cutoff)
                {
                    info.Delete();
                    pruned++;
                }
            }
            catch
            {
                failed++;
            }
        }

        if (pruned > 0)
            Log.Info($"stubs: pruned {pruned} file(s) older than {maxAge.TotalDays:0} days");
        if (failed > 0)
            Log.Warn($"stubs: {failed} file(s) could not be deleted");

        return pruned;
    }

    public static int Run(TimeSpan maxAge)
    {
        var dir = Workspace.CapturesDir;
        if (!Directory.Exists(dir)) return 0;

        var cutoff = DateTime.UtcNow - maxAge;
        var pruned = 0;
        var failed = 0;

        // Enumerate *.bin and *.txt, both produced per request by CaptureWriter.
        // We don't recurse — the captures dir is intentionally flat.
        foreach (var path in Directory.EnumerateFiles(dir))
        {
            try
            {
                var info = new FileInfo(path);
                if (info.LastWriteTimeUtc < cutoff)
                {
                    info.Delete();
                    pruned++;
                }
            }
            catch
            {
                failed++;
            }
        }

        if (pruned > 0)
            Log.Info($"captures: pruned {pruned} file(s) older than {maxAge.TotalDays:0} days");
        if (failed > 0)
            Log.Warn($"captures: {failed} file(s) could not be deleted (in use? AV holding?)");

        return pruned;
    }
}
