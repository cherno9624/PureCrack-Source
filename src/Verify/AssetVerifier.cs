using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using PureCrack.Util;

namespace PureCrack.Verify;

/// <summary>
/// Frozen-target enforcement. Verifies that the panel binaries on disk match
/// the SHA-256 baseline from <see cref="ExpectedHashes"/>. The lifetime kit
/// targets exactly v4.0.9596.35655 — any drift (operator accidentally
/// installed a newer PureRAT, AV mangled a file, panel auto-update slipped
/// past the hosts file block) is detected here and fails the launch with a
/// clear actionable error rather than corrupting silently downstream.
/// </summary>
public static class AssetVerifier
{
    public sealed class Result
    {
        public List<Mismatch> Mismatches { get; } = new();
        public List<string> Missing { get; } = new();
        public bool Ok => Mismatches.Count == 0 && Missing.Count == 0;
    }

    public sealed class Mismatch
    {
        public string Path { get; init; } = "";
        public string Expected { get; init; } = "";
        public string Actual { get; init; } = "";
    }

    /// <summary>
    /// Hash every file in <see cref="ExpectedHashes.Panel"/> under
    /// <paramref name="panelDir"/> and report every drift. We don't bail at
    /// the first mismatch — operators get the full picture in one launch.
    /// </summary>
    public static Result Verify(string panelDir)
    {
        var r = new Result();
        foreach (var kv in ExpectedHashes.Panel)
        {
            var path = Path.Combine(panelDir, kv.Key.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                r.Missing.Add(kv.Key);
                continue;
            }
            var actual = HashHex(path);
            if (!string.Equals(actual, kv.Value, StringComparison.OrdinalIgnoreCase))
            {
                r.Mismatches.Add(new Mismatch
                {
                    Path = kv.Key,
                    Expected = kv.Value,
                    Actual = actual,
                });
            }
        }
        return r;
    }

    /// <summary>Pretty-print verification result to the log.</summary>
    public static void Report(Result r)
    {
        if (r.Ok)
        {
            Log.Ok($"asset verify: all {ExpectedHashes.Panel.Count} panel files match {ExpectedHashes.PanelVersion} baseline");
            return;
        }

        Log.Err($"asset verify FAILED — kit pinned to {ExpectedHashes.PanelVersion}:");
        foreach (var m in r.Missing)
            Log.Bullet($"  MISSING  panel/{m}");
        foreach (var m in r.Mismatches)
        {
            Log.Bullet($"  DRIFT    panel/{m.Path}");
            Log.Bullet($"           expected {m.Expected}");
            Log.Bullet($"           got      {m.Actual}");
        }
        Log.Bullet("");
        Log.Bullet("If your panel was updated by PureCoder, this kit no longer applies — wait for");
        Log.Bullet("a new kit baseline. To force-launch anyway (advanced, may break wire format),");
        Log.Bullet("set PURECRACK_SKIP_ASSET_VERIFY=1.");
    }

    /// <summary>
    /// True iff the operator has explicitly opted into running against a
    /// drifted panel. Useful only for kit developers who know what they're
    /// doing — every other case should fail the launch.
    /// </summary>
    public static bool SkipRequested() =>
        string.Equals(
            Environment.GetEnvironmentVariable("PURECRACK_SKIP_ASSET_VERIFY"),
            "1",
            StringComparison.Ordinal);

    private static string HashHex(string path)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        var hash = sha.ComputeHash(fs);
        var sb = new System.Text.StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
