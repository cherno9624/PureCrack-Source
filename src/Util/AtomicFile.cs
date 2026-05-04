using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace PureCrack.Util;

/// <summary>
/// Atomic file writes via the temp-then-rename pattern. Eliminates the
/// "crashed mid-WriteAllBytes → corrupt file at next launch" failure mode
/// that bites long-running kits eventually. Also durable across power loss
/// on any reasonable filesystem (NTFS journal records the rename atomically).
///
/// Usage mirrors <see cref="File.WriteAllBytes"/> /
/// <see cref="File.WriteAllText"/> — drop-in replacement.
/// </summary>
internal static class AtomicFile
{
    // MoveFileEx flags — MOVEFILE_REPLACE_EXISTING makes the rename atomic at
    // the NTFS journal level: the destination is never absent from the
    // filesystem, even for a single tick. AV and indexers can't inject
    // themselves into a window that doesn't exist.
    // MOVEFILE_WRITE_THROUGH flushes the operation before returning so power
    // loss after the call won't reorder the rename behind pending writes.
    private const uint MOVEFILE_REPLACE_EXISTING = 0x00000001;
    private const uint MOVEFILE_WRITE_THROUGH   = 0x00000008;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, uint dwFlags);

    /// <summary>
    /// Write <paramref name="bytes"/> to <paramref name="path"/> atomically.
    /// On return, either <paramref name="path"/> contains the full new content
    /// OR the original (if any) is untouched — never a partial write.
    /// </summary>
    public static void WriteAllBytes(string path, byte[] bytes)
    {
        var dir = Path.GetDirectoryName(path) ?? ".";
        Directory.CreateDirectory(dir);

        // Sibling tempfile in the same directory so rename is on the same
        // volume — required for File.Move / MoveFileEx to be an actual rename
        // rather than a copy+delete (which would defeat atomicity).
        var temp = Path.Combine(dir, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            // WriteThrough pushes the bytes through the OS write-back cache
            // before we rename. Without the flush, a power-loss between rename
            // and flush could replay-order the rename ahead of the data write.
            using (var fs = new FileStream(temp,
                       FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       bufferSize: 4096, FileOptions.WriteThrough))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(flushToDisk: true);
            }

            ReplaceWithRetry(temp, path);
        }
        catch
        {
            // Best-effort tempfile cleanup. Worst case the operator finds a
            // stray .pfx.<guid>.tmp file in data/ which is harmless.
            try { File.Delete(temp); } catch { }
            throw;
        }
    }

    /// <summary>
    /// Same contract as <see cref="WriteAllBytes"/> but takes a string.
    /// </summary>
    public static void WriteAllText(string path, string contents) =>
        WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes(contents));

    /// <summary>
    /// Atomically replace <paramref name="dest"/> with <paramref name="temp"/>
    /// via MoveFileEx(MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH).
    /// The destination is never absent from the directory — the NTFS journal
    /// guarantees the rename is a single atomic transaction. Retries briefly
    /// to ride out transient antivirus / indexer file locks.
    /// </summary>
    private static void ReplaceWithRetry(string temp, string dest)
    {
        const int attempts = 10;
        for (var i = 0; i < attempts; i++)
        {
            if (MoveFileEx(temp, dest, MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
                return;

            var err = Marshal.GetLastWin32Error();
            // ERROR_SHARING_VIOLATION (32) or ERROR_ACCESS_DENIED (5) — transient
            // lock from AV/indexer. Wait and retry. Anything else is terminal.
            if ((err == 32 || err == 5) && i < attempts - 1)
            {
                Thread.Sleep(50);
                continue;
            }

            throw new IOException(
                $"MoveFileEx failed for {Path.GetFileName(dest)}: win32 error {err}");
        }
    }
}
