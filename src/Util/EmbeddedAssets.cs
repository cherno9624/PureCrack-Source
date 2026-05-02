using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PureCrack.Util;

/// <summary>
/// Reads embedded resources baked into the EXE at build time. The csproj's
/// <c>&lt;EmbeddedResource Include="assets\..." /&gt;</c> items become manifest
/// resources whose names are derived from the file path (separators → dots).
///
/// Examples (assuming RootNamespace="PureCrack"):
///   assets/inner/Class9.cs           → PureCrack.assets.inner.Class9.cs
///   assets/inner/Loader.cs.tmpl      → PureCrack.assets.inner.Loader.cs.tmpl
///   assets/inner/protobuf-net.dll    → PureCrack.assets.inner.protobuf-net.dll
///   assets/compile_response.bin      → PureCrack.assets.compile_response.bin
/// </summary>
internal static class EmbeddedAssets
{
    private static readonly Assembly Asm = typeof(EmbeddedAssets).Assembly;
    private const string Prefix = "PureCrack.assets.";

    /// <summary>All inner C# source files, keyed by their original filename.</summary>
    public static IReadOnlyDictionary<string, string> InnerSources
    {
        get
        {
            if (_innerSources != null) return _innerSources;
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in Asm.GetManifestResourceNames())
            {
                if (!name.StartsWith(Prefix + "inner.", StringComparison.Ordinal)) continue;
                if (!name.EndsWith(".cs", StringComparison.Ordinal)) continue;
                var fname = name.Substring(Prefix.Length + "inner.".Length); // e.g. "Class9.cs"
                dict[fname] = ReadString(name);
            }
            return _innerSources = dict;
        }
    }
    private static Dictionary<string, string>? _innerSources;

    /// <summary>The Loader template with __KEY_B64__ / __IV_B64__ placeholders.</summary>
    public static string LoaderTemplate =>
        _loader ??= ReadString(Prefix + "inner.Loader.tmpl");
    private static string? _loader;

    /// <summary>protobuf-net.dll bytes, referenced by inner DLL at compile time.</summary>
    public static byte[] ProtobufNetDll =>
        _pbNet ??= ReadBytes(Prefix + "inner.protobuf-net.dll");
    private static byte[]? _pbNet;

    /// <summary>Canned /compile response — captured from real PureServer.</summary>
    public static byte[] CannedCompileResponse =>
        _canned ??= ReadBytes(Prefix + "compile_response.bin");
    private static byte[]? _canned;

    private static string ReadString(string resName)
    {
        using var s = Asm.GetManifestResourceStream(resName)
                      ?? throw new FileNotFoundException($"missing embedded resource: {resName}");
        using var r = new StreamReader(s, Encoding.UTF8);
        return r.ReadToEnd();
    }

    private static byte[] ReadBytes(string resName)
    {
        using var s = Asm.GetManifestResourceStream(resName)
                      ?? throw new FileNotFoundException($"missing embedded resource: {resName}");
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>For diagnostics — list every resource found.</summary>
    public static IEnumerable<string> AllResources() =>
        Asm.GetManifestResourceNames().OrderBy(n => n);
}
