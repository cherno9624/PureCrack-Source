using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using PureCrack.Panel;
using PureCrack.Setup;
using PureCrack.Util;

namespace PureCrack;

/// <summary>
/// One-pass startup sanity checks. Fail loudly with every problem listed at
/// once — never make the operator restart five times to discover five
/// different missing prerequisites.
/// </summary>
public static class Preflight
{
    public sealed class Result
    {
        public List<string> Problems { get; } = new();
        public string? PanelExePath { get; set; }
        public bool Ok => Problems.Count == 0;
    }

    public static Result Run()
    {
        var r = new Result();

        // 1. Administrator? Required for binding :443, writing hosts, installing
        //    a Root cert. Without admin every other check below would also fail
        //    in confusing ways — list all of them anyway, so the operator sees
        //    the full picture in a single launch.
        if (!IsAdmin())
            r.Problems.Add("not running as administrator " +
                           "(need admin to bind :443 + write hosts + install root cert)");

        // 2. :443 free? Anything else holding it (IIS, Skype, our own old
        //    relay from a previous session) will block the bind. We bind+close
        //    briefly to test, then look up the holding process via netstat
        //    so the operator gets an actionable error message.
        if (!IsTcpPortFree(KitPorts.Relay))
        {
            var holder = LookupTcpListenerHolder(KitPorts.Relay);
            var who = holder ?? "another process";
            var killHint = holder != null && holder.StartsWith("PID ")
                ? $" (taskkill /F /PID {holder.Substring(4).Split(' ')[0]} to stop it)"
                : "";
            r.Problems.Add($":{KitPorts.Relay} is already bound by {who}{killHint}");
        }

        // 3. Hosts file writable? We need to add api*.purecoder.io entries.
        try
        {
            if (!HostsManager.IsWritable())
                r.Problems.Add($"{HostsManager.Path} is not writable " +
                               "(file marked read-only? AV blocking?)");
        }
        catch (Exception ex)
        {
            r.Problems.Add($"hosts file check threw: {ex.Message}");
        }

        // 4. Panel exe present? Resolve it now so we can fail fast with a
        //    helpful "I looked here, here, here" message.
        try
        {
            r.PanelExePath = PanelLauncher.FindExe();
        }
        catch (FileNotFoundException ex)
        {
            r.Problems.Add(ex.Message);
        }

        // 5. Roslyn loaded? Costura's AssemblyResolve hook will fault in the
        //    Microsoft.CodeAnalysis DLLs the first time we touch them. Touch
        //    one type now so we discover any embed/load failure during preflight,
        //    not on the first /compile request.
        try
        {
            _ = typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation).Assembly.FullName;
        }
        catch (Exception ex)
        {
            r.Problems.Add($"Roslyn (Microsoft.CodeAnalysis.CSharp) not loadable: {ex.Message}");
        }

        return r;
    }

    public static void Report(Result r)
    {
        if (r.Ok)
        {
            Log.Ok($"preflight passed (panel at {r.PanelExePath})");
            return;
        }
        Log.Err($"preflight: {r.Problems.Count} problem(s) — fix all then re-launch:");
        foreach (var p in r.Problems)
            Log.Bullet("  - " + p);
    }

    // ------------------------------------------------------------------------

    private static bool IsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTcpPortFree(int port)
    {
        // Bind to the SAME interface the relay actually uses (loopback by
        // default, all-interfaces only when PURECRACK_BIND_ALL=1). Testing
        // IPAddress.Any when the relay uses Loopback would falsely report
        // ":443 in use" if any non-loopback interface had something on :443
        // that wouldn't actually conflict with our loopback bind.
        var bindAll = string.Equals(
            Environment.GetEnvironmentVariable("PURECRACK_BIND_ALL"), "1",
            StringComparison.Ordinal);
        var addr = bindAll ? IPAddress.Any : IPAddress.Loopback;

        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(addr, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            try { listener?.Stop(); } catch { /* nothing to clean up */ }
        }
    }

    /// <summary>
    /// Shells out to <c>netstat -ano</c> and parses the LISTENING line for the
    /// given port. Returns "PID 1234 = python" when found, null on any failure.
    /// Used to enrich the ":443 already bound" error with a name + PID.
    /// </summary>
    private static string? LookupTcpListenerHolder(int port)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("netstat", "-ano")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            });
            if (p == null) return null;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);

            foreach (var raw in output.Split('\n'))
            {
                var line = raw.Trim();
                if (!line.StartsWith("TCP", StringComparison.Ordinal)) continue;
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;
                if (parts[3] != "LISTENING") continue;

                var local = parts[1]; // e.g. "0.0.0.0:443" or "[::]:443"
                if (!local.EndsWith(":" + port, StringComparison.Ordinal)) continue;
                if (!int.TryParse(parts[4], out var pid)) continue;

                try
                {
                    var proc = Process.GetProcessById(pid);
                    return $"PID {pid} ({proc.ProcessName})";
                }
                catch
                {
                    return $"PID {pid}";
                }
            }
        }
        catch { /* netstat missing or refused — fall through to null */ }
        return null;
    }
}
