using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace PureCrack.Util;

/// <summary>
/// Resolves runtime paths relative to the running EXE. Override the root via the
/// <c>PURECRACK_WORKSPACE</c> env var if you need to relocate captures/stubs/data.
/// </summary>
internal static class Workspace
{
    public static string Root { get; }
    public static string DataDir { get; }
    public static string RunsDir { get; }
    public static string CapturesDir { get; }
    public static string StubsDir { get; }
    public static string E2eDir { get; }

    static Workspace()
    {
        var envRoot = Environment.GetEnvironmentVariable("PURECRACK_WORKSPACE");
        if (!string.IsNullOrWhiteSpace(envRoot))
        {
            Root = envRoot!;
        }
        else
        {
            var exe = Assembly.GetEntryAssembly()?.Location
                      ?? Process.GetCurrentProcess().MainModule!.FileName!;
            Root = Path.GetDirectoryName(exe)
                   ?? throw new InvalidOperationException("can't resolve EXE directory");
        }

        DataDir     = Path.Combine(Root, "data");
        RunsDir     = Path.Combine(Root, "runs");
        CapturesDir = Path.Combine(RunsDir, "captures");
        StubsDir    = Path.Combine(RunsDir, "stubs");
        E2eDir      = Path.Combine(RunsDir, "e2e");

        foreach (var d in new[] { DataDir, CapturesDir, StubsDir, E2eDir })
        {
            try { Directory.CreateDirectory(d); }
            catch (Exception ex)
            {
                // Disk full, ACL deny, path too long — non-fatal at init time.
                // The code that actually needs the directory will fail later
                // with a specific error instead of a cryptic TypeInitializationException.
                System.Diagnostics.Debug.WriteLine($"Workspace: can't create {d}: {ex.Message}");
            }
        }
    }
}
