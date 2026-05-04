using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace PureCrack.Util;

/// <summary>
/// Cross-process mutex preventing two PureCrack instances from racing on
/// shared state (data/ PFX writes, Root store, hosts file, regkeys).
///
/// Named in the <c>Global\</c> namespace so it's visible across user
/// sessions; ACL'd to allow Everyone so an unelevated probe by a future
/// "is PureCrack already running?" check works without admin. Released
/// automatically via <see cref="IDisposable"/> on normal exit and via
/// the OS on abnormal exit (the kernel cleans up named mutexes when the
/// last handle closes).
///
/// Usage:
///   using var _ = KitMutex.Acquire();   // throws if already held
///   ... do work ...
/// </summary>
internal sealed class KitMutex : IDisposable
{
    private const string MutexName = @"Global\PureCrack-v3-instance";

    private readonly Mutex _mutex;
    private readonly bool _owned;

    private KitMutex(Mutex mutex, bool owned)
    {
        _mutex = mutex;
        _owned = owned;
    }

    /// <summary>
    /// Try to acquire the global PureCrack mutex. Throws
    /// <see cref="InvalidOperationException"/> if another instance already
    /// holds it (with the holding PID if findable).
    /// </summary>
    public static KitMutex Acquire()
    {
        var security = BuildEveryoneFullControl();
        // Try to open existing first; create if absent. This pattern avoids
        // a race where two starting instances both Create-or-Open and one
        // gets the wrong ACL.
        var mutex = new Mutex(initiallyOwned: false, name: MutexName, out var createdNew);
        if (createdNew)
        {
            try { mutex.SetAccessControl(security); } catch { /* non-fatal */ }
        }

        // Wait briefly to give a previous-instance-shutting-down a chance
        // to release. 2 seconds is enough to ride out a graceful exit;
        // longer than that and the operator probably ran two by accident.
        var got = mutex.WaitOne(TimeSpan.FromSeconds(2), exitContext: false);
        if (!got)
        {
            mutex.Dispose();
            var holder = TryGetHoldingProcess();
            throw new InvalidOperationException(
                $"another PureCrack instance is already running{(holder != null ? $" ({holder})" : "")} — " +
                "exit it before launching a second copy");
        }
        return new KitMutex(mutex, owned: true);
    }

    public void Dispose()
    {
        try
        {
            if (_owned) _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // ReleaseMutex on a mutex we don't own throws — but we set _owned=true
            // only on successful WaitOne. If we land here something else corrupted
            // the state; nothing useful we can do.
        }
        finally
        {
            _mutex.Dispose();
        }
    }

    /// <summary>
    /// Allow Everyone full control on the named mutex so a non-admin process
    /// could (in principle) check whether the kit is running. We're admin-only
    /// today, but the ACL'd mutex is the right shape for the general case
    /// and costs nothing.
    /// </summary>
    private static MutexSecurity BuildEveryoneFullControl()
    {
        var sec = new MutexSecurity();
        var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        sec.AddAccessRule(new MutexAccessRule(
            everyone,
            MutexRights.FullControl,
            AccessControlType.Allow));
        return sec;
    }

    /// <summary>
    /// Best-effort: find any other PureCrack.exe process and report its PID.
    /// Used only to enrich the "already running" error; null on any failure.
    /// </summary>
    private static string? TryGetHoldingProcess()
    {
        try
        {
            var me = Process.GetCurrentProcess().Id;
            foreach (var p in Process.GetProcessesByName("PureCrack"))
            {
                using (p)
                {
                    if (p.Id != me) return $"PID {p.Id}";
                }
            }
        }
        catch { /* fall through */ }
        return null;
    }
}
