using System;
using System.IO;
using System.Text;
using PureCrack.Util;
using PureCrack.Wire;

namespace PureCrack.Relay;

/// <summary>
/// Persists licence-API requests to <c>runs/captures/</c> for after-the-fact
/// inspection. Three files per request:
///
///   {ts}_{path}.raw.bin   raw HTTP body bytes (IV + ciphertext)
///   {ts}_{path}.pt.bin    decrypted body bytes (if decrypt succeeded)
///   {ts}_{path}.pt.txt    hex dump + protobuf tree pretty-print
///
/// The .txt file is the one you'd actually open by hand. The two .bin files
/// are there so you can replay a captured request through any tool you write
/// later.
///
/// Path filter: only <c>/api/licence/*</c> traffic is captured. Scanner noise
/// (/, /favicon.ico, /sitemap.xml, random fuzzed paths) is dropped. The relay
/// sees this traffic only when bound publicly (PURECRACK_BIND_ALL=1) or when
/// proxied through some other path; either way it's not signal for protocol
/// analysis and would otherwise fill the captures dir with thousands of empty
/// .raw.bin files over time.
/// </summary>
public static class CaptureWriter
{
    /// <summary>
    /// Persist this request to <c>runs/captures/</c>, or skip silently if it's
    /// not a licence-API path. Returns the file prefix (no extension) when
    /// written, or null when the request was filtered out.
    /// </summary>
    public static string? Dump(string path, byte[] rawBody, byte[]? plaintext)
    {
        if (!IsCapturedPath(path)) return null;

        DiskGuard.WarnIfLow();

        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safe = SanitizePathForFilename(path);
        var prefix = Path.Combine(Workspace.CapturesDir, $"{ts}_{safe}");

        File.WriteAllBytes(prefix + ".raw.bin", rawBody);

        if (plaintext != null)
        {
            File.WriteAllBytes(prefix + ".pt.bin", plaintext);
            File.WriteAllText(prefix + ".pt.txt", BuildPrettyDump(path, plaintext), Encoding.UTF8);
        }
        return prefix;
    }

    /// <summary>
    /// True if the path is a licence-API endpoint worth persisting. Excludes
    /// /heartbeat and /update-plugins — the panel polls these every 30-60s
    /// and capturing every ping floods the disk (~720 files/day) with
    /// identical ACK responses. Captures the endpoints that carry actual
    /// signal: /validate (licence check) and /compile (stub build).
    /// </summary>
    public static bool IsCapturedPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (path.IndexOf("/api/licence/", StringComparison.OrdinalIgnoreCase) < 0) return false;
        if (path.IndexOf("/heartbeat", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (path.IndexOf("/update-plugins", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        return true;
    }

    private static string BuildPrettyDump(string path, byte[] pt)
    {
        var sb = new StringBuilder(pt.Length * 4);
        sb.Append("URL: ").Append(path).Append('\n');
        sb.Append("Decrypted ").Append(pt.Length).Append(" bytes\n\nHEX:\n");

        for (var off = 0; off < pt.Length; off += 32)
        {
            var len = Math.Min(32, pt.Length - off);
            sb.Append(off.ToString("x4")).Append("  ");
            for (var i = 0; i < len; i++) sb.Append(pt[off + i].ToString("x2")).Append(' ');
            for (var i = len; i < 32; i++) sb.Append("   ");
            sb.Append(" |");
            for (var i = 0; i < len; i++)
            {
                var b = pt[off + i];
                sb.Append((b >= 0x20 && b < 0x7F) ? (char)b : '.');
            }
            sb.Append("|\n");
        }

        sb.Append("\nPROTOBUF TREE:\n");
        try { sb.Append(ProtoNet.Dump(pt)); }
        catch (Exception ex) { sb.Append("<parse err: ").Append(ex.Message).Append(">\n"); }
        return sb.ToString();
    }

    private static string SanitizePathForFilename(string path)
    {
        // "/api/licence/validate" → "api_licence_validate"
        var s = path.Replace('/', '_').Trim('_');
        foreach (var bad in Path.GetInvalidFileNameChars())
            s = s.Replace(bad, '_');
        if (s.Length == 0) s = "root";
        if (s.Length > 80) s = s.Substring(0, 80);
        return s;
    }
}
