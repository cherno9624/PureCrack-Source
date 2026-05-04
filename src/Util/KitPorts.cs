namespace PureCrack.Util;

/// <summary>
/// Network ports the kit binds or connects to. Centralised so the relay,
/// preflight, panel launcher, and any future cleanup logic all agree on
/// the same numbers — and the operator has one place to read when they
/// need to know what to keep clear.
/// </summary>
internal static class KitPorts
{
    /// <summary>The port the relay binds (loopback by default). 443 because
    /// the panel hardcodes its licence-API URLs to https with no port.</summary>
    public const int Relay = 443;

    /// <summary>The port the panel binds for incoming bot connections.
    /// Set in panel/data/Settings.json's Ports[]; first entry by convention.</summary>
    public const int Panel = 56001;
}
