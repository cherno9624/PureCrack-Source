using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using PureCrack.Util;

namespace PureCrack.Panel;

/// <summary>
/// Locates and launches the panel binary (<c>PureRAT.exe</c>), then waits for
/// it to bind <c>:56001</c> (the panel-side bot listener). The panel is
/// brought up via <c>ShellExecute</c> so it gets its own console + UAC handling
/// rather than inheriting ours.
///
/// Path resolution order — strictly bundled-first; we no longer probe random
/// install locations on the operator's box:
///   1. <c>PURE_PANEL_EXE</c> env var (explicit override)
///   2. <see cref="BundledPanelPath"/> — <c>&lt;workspace&gt;/panel/PureRAT.exe</c>
///
/// If neither is present the launch aborts in preflight with a clear "drop
/// the binary at this exact path" message. That keeps PureCrack pinned to a
/// known-tested panel build instead of accidentally running whatever
/// PureRAT.exe happens to be on the box.
/// </summary>
public static class PanelLauncher
{
    /// <summary>
    /// Canonical bundled location — sibling of the EXE. This is what we
    /// expect in a deployed install (<c>PureCrack/PureCrack.exe</c> +
    /// <c>PureCrack/panel/PureRAT.exe</c>).
    /// </summary>
    public static string BundledPanelPath => Path.Combine(Workspace.Root, "panel", "PureRAT.exe");

    /// <summary>
    /// Dev-mode fallback: when <c>PureCrack.exe</c> is run from
    /// <c>bin\Release\</c> in a built source tree, the project root's
    /// <c>panel/</c> directory is two levels up. Lets the same source tree
    /// run via <c>dotnet build</c> + direct EXE invocation without copying
    /// the 88 MB panel binary to <c>bin\Release\panel\</c>.
    /// </summary>
    public static string DevPanelPath =>
        Path.GetFullPath(Path.Combine(Workspace.Root, "..", "..", "panel", "PureRAT.exe"));

    /// <summary>
    /// Resolve the panel exe path. Search order:
    ///   1. <c>PURE_PANEL_EXE</c> env var (explicit override)
    ///   2. <see cref="BundledPanelPath"/> (deployed layout)
    ///   3. <see cref="DevPanelPath"/> (running from <c>bin\Release\</c> in source tree)
    /// </summary>
    public static string FindExe()
    {
        var fromEnv = Environment.GetEnvironmentVariable("PURE_PANEL_EXE");
        if (!string.IsNullOrEmpty(fromEnv))
        {
            if (File.Exists(fromEnv)) return fromEnv;
            Log.Warn($"PURE_PANEL_EXE points at {fromEnv} but file doesn't exist — " +
                     "falling back to bundled");
        }

        if (File.Exists(BundledPanelPath)) return BundledPanelPath;
        if (File.Exists(DevPanelPath))     return DevPanelPath;

        throw new FileNotFoundException(
            $"PureRAT.exe not found. Looked at:\n" +
            $"  - {BundledPanelPath}  (deployed layout)\n" +
            $"  - {DevPanelPath}  (running from bin\\Release\\ in source tree)\n" +
            $"Either copy PureRAT.exe to one of those, or set PURE_PANEL_EXE env var.\n" +
            $"See {Path.Combine(Path.GetDirectoryName(BundledPanelPath)!, "README.md")} " +
            $"for bundling instructions.");
    }

    /// <summary>
    /// Start the panel via <c>ShellExecute</c>. Returns the process handle so
    /// the caller can monitor / wait. The panel runs in its own session — we
    /// don't redirect its stdio.
    /// </summary>
    public static Process Launch(string panelExe)
    {
        Log.Info($"launching panel: {panelExe}");
        var psi = new ProcessStartInfo
        {
            FileName = panelExe,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(panelExe) ?? Workspace.Root,
        };
        var p = Process.Start(psi)
                ?? throw new InvalidOperationException("Process.Start returned null");
        Log.Ok($"panel started (PID {p.Id})");
        return p;
    }

    /// <summary>
    /// Poll <c>127.0.0.1:port</c> until something accepts a TCP connection
    /// (= panel has finished startup + clicked Login + bound the listener).
    /// </summary>
    /// <returns>true if the listener came up before the timeout.</returns>
    public static bool WaitForListener(int port, TimeSpan timeout, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        Log.Info($"waiting for panel to bind :{port} (timeout {timeout.TotalSeconds:0}s)");
        var attempt = 0;
        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            attempt++;
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
                if (connectTask.Wait(500, ct) && client.Connected)
                {
                    Log.Ok($":{port} is up (after {attempt} probes)");
                    return true;
                }
            }
            catch (Exception)
            {
                // Most attempts will throw SocketException 10061 (refused) until
                // the panel binds. That's fine; keep polling.
            }
            // Thread.Sleep doesn't accept a CancellationToken on net472 —
            // check ct on the next loop iteration instead.
            Thread.Sleep(500);
        }
        return false;
    }
}
