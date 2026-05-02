using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PureCrack.Wire;

/// <summary>
/// Standard protobuf wire types — the low 3 bits of every tag varint.
/// PureRAT's panel speaks vanilla protobuf-net wire; protobuf-net's quirks
/// (SubItem ProtoInclude tags, etc.) are implemented at a higher layer
/// using these primitives.
/// </summary>
public enum ProtoWire
{
    /// <summary>int32/int64/uint32/uint64/sint32/sint64/bool/enum.</summary>
    Varint  = 0,
    /// <summary>fixed64/sfixed64/double.</summary>
    Fixed64 = 1,
    /// <summary>string/bytes/embedded message/packed repeated.</summary>
    Bytes   = 2,
    /// <summary>fixed32/sfixed32/float.</summary>
    Fixed32 = 5,
}

/// <summary>
/// One decoded value off the wire. <see cref="Wire"/> selects which of
/// <see cref="Bytes"/> or <see cref="Number"/> is meaningful.
/// </summary>
public sealed class ProtoValue
{
    public ProtoWire Wire { get; }
    /// <summary>Length-delimited payload. Empty for non-Bytes wire types.</summary>
    public byte[] Bytes { get; }
    /// <summary>Varint / Fixed32 / Fixed64 raw value (zero-extended).</summary>
    public ulong Number { get; }

    private ProtoValue(ProtoWire wire, byte[] bytes, ulong number)
    {
        Wire = wire;
        Bytes = bytes;
        Number = number;
    }

    public static ProtoValue OfVarint(ulong v)  => new(ProtoWire.Varint,  Array.Empty<byte>(), v);
    public static ProtoValue OfBytes(byte[] b)  => new(ProtoWire.Bytes,   b,                    0);
    public static ProtoValue OfFixed64(ulong v) => new(ProtoWire.Fixed64, Array.Empty<byte>(), v);
    public static ProtoValue OfFixed32(uint v)  => new(ProtoWire.Fixed32, Array.Empty<byte>(), v);
}

/// <summary>
/// Hand-rolled protobuf wire encoder/decoder. Mirror of <c>_pb.py</c> in the
/// Python reference kit. We don't depend on Google.Protobuf or protobuf-net
/// at the relay layer because:
///   1. We need to encode/decode arbitrary unknown fields without a .proto;
///   2. The protobuf-net DLL is embedded for the *inner* DLL compile only;
///   3. Hand-rolled keeps the wire-shape decisions visible — the panel rejects
///      a 3-field /compile reply, accepts a 5-field one, and we want that
///      shape constructed in code we can read.
///
/// All write helpers return fresh byte arrays — no shared buffers, no
/// streaming. Simpler to reason about; perf is fine for the tens of
/// requests per session this kit handles.
/// </summary>
public static class ProtoNet
{
    // ============================================================================
    // ENCODE
    // ============================================================================

    /// <summary>LEB128 varint — 7 bits per byte, MSB = "more bytes follow".</summary>
    public static byte[] WriteVarint(ulong n)
    {
        // Max varint length is 10 bytes (uint64).
        Span<byte> buf = stackalloc byte[10];
        var len = 0;
        while (n > 0x7F)
        {
            buf[len++] = (byte)((n & 0x7F) | 0x80);
            n >>= 7;
        }
        buf[len++] = (byte)n;
        return buf.Slice(0, len).ToArray();
    }

    /// <summary>Tag byte(s) = (field_num &lt;&lt; 3) | wire_type, varint-encoded.</summary>
    public static byte[] WriteTag(int field, ProtoWire wire) =>
        WriteVarint(((ulong)field << 3) | (ulong)wire);

    /// <summary>Field N = UTF-8 string (length-delimited).</summary>
    public static byte[] FString(int field, string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        return Concat(WriteTag(field, ProtoWire.Bytes), WriteVarint((ulong)bytes.Length), bytes);
    }

    /// <summary>Field N = raw bytes (length-delimited).</summary>
    public static byte[] FBytes(int field, byte[] b) =>
        Concat(WriteTag(field, ProtoWire.Bytes), WriteVarint((ulong)b.Length), b);

    /// <summary>Field N = signed 64-bit varint.</summary>
    public static byte[] FInt(int field, long v) =>
        Concat(WriteTag(field, ProtoWire.Varint), WriteVarint(unchecked((ulong)v)));

    /// <summary>Field N = bool (varint 0/1).</summary>
    public static byte[] FBool(int field, bool v) =>
        Concat(WriteTag(field, ProtoWire.Varint), WriteVarint(v ? 1UL : 0UL));

    /// <summary>Field N = embedded message (length-delimited).</summary>
    public static byte[] FSub(int field, byte[] body) =>
        Concat(WriteTag(field, ProtoWire.Bytes), WriteVarint((ulong)body.Length), body);

    private static byte[] Concat(params byte[][] chunks)
    {
        var total = 0;
        foreach (var c in chunks) total += c.Length;
        var output = new byte[total];
        var off = 0;
        foreach (var c in chunks)
        {
            Buffer.BlockCopy(c, 0, output, off, c.Length);
            off += c.Length;
        }
        return output;
    }

    // ============================================================================
    // DECODE
    // ============================================================================

    /// <summary>Read one varint starting at <paramref name="off"/>.</summary>
    /// <returns>(value, new offset just past the varint).</returns>
    public static (ulong val, int newOff) ReadVarint(byte[] buf, int off)
    {
        ulong v = 0;
        var shift = 0;
        while (off < buf.Length)
        {
            var b = buf[off++];
            v |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) return (v, off);
            shift += 7;
            if (shift >= 64)
                throw new FormatException("varint exceeds 10 bytes");
        }
        throw new FormatException("truncated varint");
    }

    /// <summary>
    /// Parse a complete protobuf message into a <c>field_num → [values]</c> map.
    /// Repeated fields collect into the same key in source order.
    /// Throws on truncation or unknown wire types — we never want to silently
    /// accept malformed input from the panel.
    /// </summary>
    public static Dictionary<int, List<ProtoValue>> Parse(byte[] data)
    {
        var dict = new Dictionary<int, List<ProtoValue>>();
        var off = 0;
        while (off < data.Length)
        {
            ulong tag;
            (tag, off) = ReadVarint(data, off);
            var field = (int)(tag >> 3);
            var wire = (int)(tag & 0x7);

            ProtoValue value;
            switch (wire)
            {
                case 0: // Varint
                {
                    ulong num;
                    (num, off) = ReadVarint(data, off);
                    value = ProtoValue.OfVarint(num);
                    break;
                }
                case 1: // Fixed64
                    if (off + 8 > data.Length)
                        throw new FormatException("truncated fixed64");
                    value = ProtoValue.OfFixed64(BitConverter.ToUInt64(data, off));
                    off += 8;
                    break;
                case 2: // Length-delimited
                {
                    ulong len;
                    (len, off) = ReadVarint(data, off);
                    if (off + (int)len > data.Length)
                        throw new FormatException("truncated length-delimited");
                    var bytes = new byte[(int)len];
                    Buffer.BlockCopy(data, off, bytes, 0, (int)len);
                    off += (int)len;
                    value = ProtoValue.OfBytes(bytes);
                    break;
                }
                case 5: // Fixed32
                    if (off + 4 > data.Length)
                        throw new FormatException("truncated fixed32");
                    value = ProtoValue.OfFixed32(BitConverter.ToUInt32(data, off));
                    off += 4;
                    break;
                default:
                    throw new FormatException($"unknown wire type {wire} at offset {off}");
            }

            if (!dict.TryGetValue(field, out var list))
            {
                list = new List<ProtoValue>();
                dict[field] = list;
            }
            list.Add(value);
        }
        return dict;
    }

    /// <summary>All values of <paramref name="field"/> as UTF-8 strings.</summary>
    public static List<string> GetStrings(Dictionary<int, List<ProtoValue>> parsed, int field)
    {
        if (!parsed.TryGetValue(field, out var list)) return new List<string>();
        var output = new List<string>(list.Count);
        foreach (var v in list)
        {
            if (v.Wire == ProtoWire.Bytes)
                output.Add(Encoding.UTF8.GetString(v.Bytes));
        }
        return output;
    }

    /// <summary>All values of <paramref name="field"/> as int64.</summary>
    public static List<long> GetInts(Dictionary<int, List<ProtoValue>> parsed, int field)
    {
        if (!parsed.TryGetValue(field, out var list)) return new List<long>();
        var output = new List<long>(list.Count);
        foreach (var v in list)
        {
            if (v.Wire == ProtoWire.Varint)
                output.Add(unchecked((long)v.Number));
        }
        return output;
    }

    /// <summary>First string for <paramref name="field"/>, or <paramref name="fallback"/>.</summary>
    public static string? FirstString(Dictionary<int, List<ProtoValue>> parsed, int field,
                                      string? fallback = null)
    {
        var ss = GetStrings(parsed, field);
        return ss.Count > 0 ? ss[0] : fallback;
    }

    /// <summary>First nested message bytes for <paramref name="field"/>, or null.</summary>
    public static byte[]? FirstSub(Dictionary<int, List<ProtoValue>> parsed, int field)
    {
        if (!parsed.TryGetValue(field, out var list)) return null;
        foreach (var v in list)
            if (v.Wire == ProtoWire.Bytes) return v.Bytes;
        return null;
    }

    // ============================================================================
    // PRETTY-PRINT — used by the relay's request capture dump
    // ============================================================================

    private const int MaxDumpDepth = 8;

    public static string Dump(byte[] data)
    {
        var sb = new StringBuilder();
        DumpInto(data, 0, sb);
        return sb.ToString();
    }

    private static void DumpInto(byte[] data, int depth, StringBuilder sb)
    {
        if (depth > MaxDumpDepth)
        {
            sb.Append(Indent(depth)).Append("<max depth>\n");
            return;
        }

        Dictionary<int, List<ProtoValue>> parsed;
        try { parsed = Parse(data); }
        catch (Exception ex)
        {
            sb.Append(Indent(depth)).Append("<parse err: ").Append(ex.Message).Append(">\n");
            return;
        }

        foreach (var kv in parsed.OrderBy(p => p.Key))
        {
            foreach (var v in kv.Value)
            {
                var pad = Indent(depth);
                switch (v.Wire)
                {
                    case ProtoWire.Varint:
                        sb.Append(pad).Append('F').Append(kv.Key).Append(" varint = ")
                          .Append(v.Number).Append('\n');
                        break;
                    case ProtoWire.Fixed64:
                        sb.Append(pad).Append('F').Append(kv.Key).Append(" fixed64 = 0x")
                          .Append(v.Number.ToString("x16")).Append('\n');
                        break;
                    case ProtoWire.Fixed32:
                        sb.Append(pad).Append('F').Append(kv.Key).Append(" fixed32 = 0x")
                          .Append(((uint)v.Number).ToString("x8")).Append('\n');
                        break;
                    case ProtoWire.Bytes:
                        if (v.Bytes.Length == 0)
                        {
                            sb.Append(pad).Append('F').Append(kv.Key).Append(" empty\n");
                        }
                        else if (LooksLikeProto(v.Bytes))
                        {
                            sb.Append(pad).Append('F').Append(kv.Key).Append(" sub(")
                              .Append(v.Bytes.Length).Append("):\n");
                            DumpInto(v.Bytes, depth + 1, sb);
                        }
                        else if (TryUtf8(v.Bytes, out var asString))
                        {
                            sb.Append(pad).Append('F').Append(kv.Key).Append(" str(")
                              .Append(v.Bytes.Length).Append(") = ").Append(EscapeString(asString))
                              .Append('\n');
                        }
                        else
                        {
                            sb.Append(pad).Append('F').Append(kv.Key).Append(" bytes(")
                              .Append(v.Bytes.Length).Append(") = ").Append(HexHead(v.Bytes, 24))
                              .Append('\n');
                        }
                        break;
                }
            }
        }
    }

    private static string Indent(int depth) => new(' ', depth * 2);

    private static bool TryUtf8(byte[] b, out string s)
    {
        try
        {
            s = Encoding.UTF8.GetString(b);
            foreach (var c in s)
            {
                if (c < 0x20 && c != '\t' && c != '\n' && c != '\r') { s = ""; return false; }
            }
            return true;
        }
        catch { s = ""; return false; }
    }

    private static bool LooksLikeProto(byte[] b)
    {
        if (b.Length < 2) return false;
        try
        {
            var parsed = Parse(b);
            return parsed.Count > 0;
        }
        catch { return false; }
    }

    private static string HexHead(byte[] b, int n)
    {
        var sb = new StringBuilder(n * 3 + 4);
        for (var i = 0; i < Math.Min(n, b.Length); i++)
            sb.Append(b[i].ToString("x2")).Append(' ');
        if (b.Length > n) sb.Append("...");
        return sb.ToString().TrimEnd();
    }

    private static string EscapeString(string s)
    {
        if (s.Length > 80) s = s.Substring(0, 80) + "...";
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
