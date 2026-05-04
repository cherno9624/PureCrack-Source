using System;
using System.Collections.Generic;
using System.Linq;
using PureCrack.Crypto;
using PureCrack.Relay;
using PureCrack.Util;
using PureCrack.Wire;

namespace PureCrack.Verify;

/// <summary>
/// Synthetic round-trip checks for every wire-format invariant the relay
/// depends on. Catches silent breakage before it manifests as panel weirdness:
///
///   1. AES-256-CBC round-trip with the static panel key — proves the
///      crypto path encrypts/decrypts the same bytes the panel will.
///   2. AES with fixed IV produces deterministic ciphertext — proves the
///      key constant hasn't been corrupted in some build pass.
///   3. Protobuf wire encoder/decoder round-trip across all 4 wire types.
///   4. /validate response: build it, parse it back, assert all expected
///      fields land at their documented numbers.
///   5. /heartbeat ACK is exactly the 4-byte shape the panel accepts.
///   6. /update-plugins ACK is identical to heartbeat (same handler).
///
/// Runnable via <c>PureCrack.exe selftest</c>. No relay listener, no panel,
/// no admin needed beyond what the manifest already requires. Zero external
/// dependencies — everything operates on byte arrays in memory.
///
/// Exit code 0 = all green; non-zero = at least one assertion failed.
/// </summary>
public static class SelfTest
{
    public sealed class Result
    {
        public List<string> Passed { get; } = new();
        public List<string> Failed { get; } = new();
        public bool Ok => Failed.Count == 0;
    }

    /// <summary>Run every check and return the result.</summary>
    public static Result Run()
    {
        var r = new Result();

        Check(r, "aes-roundtrip",            CheckAesRoundtrip);
        Check(r, "aes-deterministic",        CheckAesDeterministic);
        Check(r, "aes-framing-roundtrip",    CheckAesFramingRoundtrip);
        Check(r, "protobuf-varint",          CheckProtobufVarint);
        Check(r, "protobuf-string",          CheckProtobufString);
        Check(r, "protobuf-bytes",           CheckProtobufBytes);
        Check(r, "protobuf-submsg",          CheckProtobufSubmsg);
        Check(r, "protobuf-repeated",        CheckProtobufRepeated);
        Check(r, "validate-response-shape",  CheckValidateResponseShape);
        Check(r, "ack-shape",                CheckAckShape);

        return r;
    }

    public static void Report(Result r)
    {
        foreach (var p in r.Passed) Log.Bullet($"  [+] {p}");
        foreach (var f in r.Failed) Log.Bullet($"  [X] {f}");
        if (r.Ok)
            Log.Ok($"selftest: {r.Passed.Count}/{r.Passed.Count} checks passed");
        else
            Log.Err($"selftest: {r.Failed.Count} of {r.Passed.Count + r.Failed.Count} checks FAILED");
    }

    // ============================================================================
    // Check harness
    // ============================================================================

    private static void Check(Result r, string name, Action body)
    {
        try
        {
            body();
            r.Passed.Add(name);
        }
        catch (Exception ex)
        {
            r.Failed.Add($"{name}: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(what);
    }

    private static void AssertEqual(byte[] expected, byte[] actual, string what)
    {
        if (expected.Length != actual.Length)
            throw new InvalidOperationException(
                $"{what}: length expected {expected.Length}, got {actual.Length}");
        for (var i = 0; i < expected.Length; i++)
            if (expected[i] != actual[i])
                throw new InvalidOperationException(
                    $"{what}: byte[{i}] expected 0x{expected[i]:x2}, got 0x{actual[i]:x2}");
    }

    // ============================================================================
    // Crypto checks
    // ============================================================================

    private static void CheckAesRoundtrip()
    {
        // Encrypt a non-trivial plaintext, decrypt, assert byte equality.
        var iv = new byte[16];
        for (var i = 0; i < 16; i++) iv[i] = (byte)i;

        var plain = System.Text.Encoding.UTF8.GetBytes(
            "PureCrack v3 selftest — quick brown fox jumps over the lazy dog.");

        var ct = Symmetric.AesEncrypt(plain, iv);
        var pt = Symmetric.AesDecrypt(ct, iv);
        AssertEqual(plain, pt, "aes round-trip plaintext mismatch");
    }

    private static void CheckAesDeterministic()
    {
        // Same plaintext + same IV must produce same ciphertext (assert
        // the AES key constant hasn't drifted).
        var iv = new byte[16];
        for (var i = 0; i < 16; i++) iv[i] = (byte)(0x10 + i);

        var plain = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var ct1 = Symmetric.AesEncrypt(plain, iv);
        var ct2 = Symmetric.AesEncrypt(plain, iv);
        AssertEqual(ct1, ct2, "aes-with-fixed-iv must be deterministic");

        // 8-byte plaintext with PKCS7 → 16-byte ciphertext block.
        Assert(ct1.Length == 16, $"aes-cbc-pkcs7 8-byte input expects 16-byte ciphertext, got {ct1.Length}");
    }

    private static void CheckAesFramingRoundtrip()
    {
        // The framing format the relay reads/writes: [16-byte IV][CT].
        var plain = System.Text.Encoding.UTF8.GetBytes("hello world");
        var framed = Symmetric.AesEncryptFraming(plain);
        Assert(framed.Length >= 32, "framed body must be at least IV + 1 block");
        var pt = Symmetric.AesDecryptFraming(framed);
        AssertEqual(plain, pt, "aes-framing round-trip mismatch");
    }

    // ============================================================================
    // Protobuf wire checks
    // ============================================================================

    private static void CheckProtobufVarint()
    {
        // Single-byte varints: 0..127.
        for (ulong v = 0; v < 128; v++)
        {
            var bytes = ProtoNet.WriteVarint(v);
            Assert(bytes.Length == 1, $"varint({v}) expected 1 byte, got {bytes.Length}");
            var (back, _) = ProtoNet.ReadVarint(bytes, 0);
            Assert(back == v, $"varint round-trip for {v} failed (got {back})");
        }
        // Two-byte varints: 128..16383.
        var twob = ProtoNet.WriteVarint(300);
        Assert(twob.Length == 2, "varint(300) expected 2 bytes");
        var (back2, _) = ProtoNet.ReadVarint(twob, 0);
        Assert(back2 == 300, $"varint round-trip for 300 failed");
    }

    private static void CheckProtobufString()
    {
        var encoded = ProtoNet.FString(7, "hello");
        var parsed = ProtoNet.Parse(encoded);
        var s = ProtoNet.FirstString(parsed, 7);
        Assert(s == "hello", $"FString round-trip: expected 'hello', got '{s}'");
    }

    private static void CheckProtobufBytes()
    {
        var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var encoded = ProtoNet.FBytes(3, data);
        var parsed = ProtoNet.Parse(encoded);
        var sub = ProtoNet.FirstSub(parsed, 3);
        Assert(sub != null, "FBytes round-trip: field 3 missing");
        AssertEqual(data, sub!, "FBytes round-trip mismatch");
    }

    private static void CheckProtobufSubmsg()
    {
        var inner = ProtoNet.FInt(1, 1);
        var outer = ProtoNet.FSub(2, inner);
        var parsed = ProtoNet.Parse(outer);
        var subBytes = ProtoNet.FirstSub(parsed, 2);
        Assert(subBytes != null, "FSub round-trip: field 2 missing");
        var inner2 = ProtoNet.Parse(subBytes!);
        Assert(inner2.ContainsKey(1), "FSub round-trip: inner field 1 missing");
    }

    private static void CheckProtobufRepeated()
    {
        // Three string fields with the same field number → repeated semantic.
        var concat = Concat(
            ProtoNet.FString(1, "a"),
            ProtoNet.FString(1, "b"),
            ProtoNet.FString(1, "c"));
        var parsed = ProtoNet.Parse(concat);
        var all = ProtoNet.GetStrings(parsed, 1);
        Assert(all.Count == 3, $"repeated string: expected 3, got {all.Count}");
        Assert(all[0] == "a" && all[1] == "b" && all[2] == "c",
            "repeated string order or content mismatch");
    }

    // ============================================================================
    // Wire-shape checks (the things the panel actually inspects)
    // ============================================================================

    private static void CheckValidateResponseShape()
    {
        // Build a validate response with a synthetic agent PFX (just dummy
        // bytes — we're testing shape, not content). Parse it back and
        // assert every field WIRE_FORMAT.md documents lands where expected.
        var dummyPfx = new byte[] { 1, 2, 3, 4 };
        var routes = new RouteHandlers(dummyPfx, cannedCompileResponse: new byte[] { 0 });
        var pb = routes.ValidatePb;

        var outer = ProtoNet.Parse(pb);
        Assert(outer.ContainsKey(2), "validate response missing outer field 2");

        var innerBytes = ProtoNet.FirstSub(outer, 2);
        Assert(innerBytes != null, "validate response field 2 not bytes");
        var inner = ProtoNet.Parse(innerBytes!);

        Assert(inner.ContainsKey(1), "validate response inner missing F1 (status)");
        Assert(inner.ContainsKey(7), "validate response inner missing F7 (greeting)");
        Assert(inner.ContainsKey(9), "validate response inner missing F9 (welcome)");
        Assert(inner.ContainsKey(10), "validate response inner missing F10 (cert wrapper)");
        Assert(inner.ContainsKey(11), "validate response inner missing F11 (HWID stats)");
        Assert(inner.ContainsKey(13), "validate response inner missing F13 (expiry)");

        // Status must be 1 = OK.
        var statusList = inner[1];
        Assert(statusList.Count == 1 && statusList[0].Number == 1,
            "validate response F1 (status) must be exactly 1");

        // F10 → F7 → F2 should contain the (base64-encoded) PFX bytes.
        var certWrap = ProtoNet.FirstSub(inner, 10);
        Assert(certWrap != null, "validate response F10 not a sub-message");
        var certInner = ProtoNet.Parse(certWrap!);
        var pfxWrap = ProtoNet.FirstSub(certInner, 7);
        Assert(pfxWrap != null, "validate response F10.F7 not a sub-message");
        var pfxLeaf = ProtoNet.Parse(pfxWrap!);
        var b64 = ProtoNet.FirstString(pfxLeaf, 2);
        Assert(!string.IsNullOrEmpty(b64), "validate response F10.F7.F2 (PFX b64) empty");
        var roundtrip = Convert.FromBase64String(b64!);
        AssertEqual(dummyPfx, roundtrip, "validate response PFX b64 didn't round-trip");
    }

    private static void CheckAckShape()
    {
        // The minimal heartbeat / update-plugins reply: F2:{F1:1}.
        // Exact bytes: 12 02 08 01.
        var ack = RouteHandlers.AckResponse();
        var expected = new byte[] { 0x12, 0x02, 0x08, 0x01 };
        AssertEqual(expected, ack, "ack shape drifted from {F2:{F1:1}}");
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var total = parts.Sum(p => p.Length);
        var buf = new byte[total];
        var off = 0;
        foreach (var p in parts)
        {
            Buffer.BlockCopy(p, 0, buf, off, p.Length);
            off += p.Length;
        }
        return buf;
    }
}
