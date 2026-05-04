using System;
using System.Collections.Generic;
using System.IO;
using PureCrack.Util;

namespace PureCrack.Provisioning;

/// <summary>
/// Recursively strips the NTFS <c>:Zone.Identifier</c> alternate data stream
/// from every file under a given root. Files extracted from a downloaded zip
/// pick up an MOTW (Mark-of-the-Web) ADS that .NET treats as a remote source;
/// without stripping, <c>Assembly.LoadFrom</c> on PureHelper.dll fails with
/// HRESULT 0x80131515 and the panel boots without plugins.
///
/// Two detection strategies:
///   1. <c>File.Exists(adsPath)</c> — fast, works on most NTFS volumes.
///   2. <c>File.Delete(adsPath)</c> — catch FileNotFoundException for false
///      negatives from strategy 1 (rare but real on some Windows builds).
///
/// Directories that can't be enumerated are collected and logged so the
/// operator knows which subtrees were skipped (rather than silently hiding
/// the gap).
/// </summary>
internal static class MotwStripper
{
    private const string ZoneIdentifierStream = ":Zone.Identifier";

    /// <summary>
    /// Walk <paramref name="root"/> recursively and remove any
    /// <c>:Zone.Identifier</c> ADS attached to files. Returns count stripped.
    /// Idempotent — files without the ADS are skipped silently.
    /// </summary>
    public static int Strip(string root)
    {
        return StripCore(root, logSkipped: false);
    }

    /// <summary>
    /// Targeted pass over the panel directory tree. Logs every file that
    /// couldn't be unblocked so the operator can act on it.
    /// </summary>
    public static int StripPanel(string panelDir)
    {
        return StripCore(panelDir, logSkipped: true);
    }

    private static int StripCore(string root, bool logSkipped)
    {
        if (!Directory.Exists(root)) return 0;

        var (files, inaccessible) = EnumerateFilesSafe(root);
        var stripped = 0;
        var skipped = 0;

        foreach (var path in files)
        {
            var adsPath = path + ZoneIdentifierStream;
            try
            {
                // Strategy 1: check existence. Fast path for the common case.
                if (File.Exists(adsPath))
                {
                    File.Delete(adsPath);
                    stripped++;
                    continue;
                }

                // Strategy 2: File.Exists can return false negatives for ADS
                // on some volume/snapshot configs. Try delete anyway — it
                // throws FileNotFoundException when the ADS genuinely isn't
                // there, which is the expected no-op path.
                try
                {
                    File.Delete(adsPath);
                    stripped++;
                }
                catch (FileNotFoundException) { /* clean — no-op */ }
                catch (DirectoryNotFoundException) { /* clean — no ADS support on this volume */ }
                catch (NotSupportedException) { /* path too long or illegal format — can't strip */ }
            }
            catch (Exception ex) when (
                ex is UnauthorizedAccessException
                || ex is IOException
                || ex is PathTooLongException
                || ex is NotSupportedException)
            {
                skipped++;
                if (logSkipped)
                    Log.Warn($"motw: can't unblock {Path.GetFileName(path)}: {ex.Message.TrimEnd('.')}");
            }
        }

        foreach (var dir in inaccessible)
            Log.Debug($"motw: can't enumerate directory: {dir}");

        if (stripped > 0)
        {
            var extra = skipped > 0 ? $" ({skipped} could not be unblocked)" : "";
            Log.Ok($"motw: stripped Zone.Identifier from {stripped} file(s){extra}");
        }
        else if (skipped > 0)
        {
            Log.Warn($"motw: {skipped} file(s) could not be unblocked " +
                     $"(check directory permissions)");
        }
        else
        {
            Log.Info("motw: no Zone.Identifier ADS found (already clean)");
        }

        return stripped;
    }

    /// <summary>
    /// Walk <paramref name="root"/> recursively collecting all files.
    /// Returns the file list and any directories that couldn't be read
    /// (permissions, reparse-point dead ends, etc.).
    /// </summary>
    private static (List<string> files, List<string> inaccessible) EnumerateFilesSafe(string root)
    {
        var files = new List<string>();
        var badDirs = new List<string>();
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            string[] dirFiles;
            string[] subDirs;
            try
            {
                dirFiles = Directory.GetFiles(dir);
                subDirs = Directory.GetDirectories(dir);
            }
            catch (Exception)
            {
                badDirs.Add(dir);
                continue;
            }
            files.AddRange(dirFiles);
            foreach (var s in subDirs) stack.Push(s);
        }

        return (files, badDirs);
    }
}
