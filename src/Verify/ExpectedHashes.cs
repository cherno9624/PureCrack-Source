using System.Collections.Generic;

namespace PureCrack.Verify;

/// <summary>
/// Frozen-baseline SHA-256 hashes for the bundled v4.0.9596 panel artefacts.
/// These are the bytes the v3 relay was developed against. Any drift —
/// PureCoder pushing a panel update that the operator's box auto-installs,
/// AV silently quarantining-and-restoring with metadata changes, accidental
/// file replacement — flips the verifier and the launch aborts with a clear
/// "your panel is not the verified build" error.
///
/// To update the baseline: rebuild the panel bundle, recompute these hashes
/// from <c>panel/...</c>, paste them in, bump the kit version. The const-string
/// shape means a swapped panel file alone can't pass verification — an
/// attacker would need to also patch the binary.
/// </summary>
public static class ExpectedHashes
{
    /// <summary>Kit-baseline label, surfaced in error messages.</summary>
    public const string PanelVersion = "v4.0.9596.35655";

    /// <summary>Path under <c>panel/</c> → expected SHA-256 (lowercase hex).</summary>
    public static readonly IReadOnlyDictionary<string, string> Panel = new Dictionary<string, string>
    {
        ["PureRAT.exe"]                  = "f7792cde754de2ec0023d2c4fad3592d394cdaa8ff011f3188989642d9adbaa6",
        ["data.pak"]                     = "efee5150ae55013540e5dccf8c95faf7e82d33e0c8c66d246c76b49876c78c6e",
        ["Plugins/PureHelper.dll"]       = "4d13c13d45e24baecc8359a9535f5a2ccdcf1249a56daaef8e42feb1491fa8ce",
        ["Plugins/PureHelper.Client.dll"] = "ed64609ee64110d3b7dd7d719b2b9dac2885b3e1c4e8154ff250fb4325b2648b",
    };
}
