using System.IO;
using PureCrack.Util;
using PureCrack.Verify;

namespace PureCrack.Cli;

/// <summary>
/// Run the frozen-baseline asset verification only — no launch. Useful as
/// a quick "does the panel match the kit's pinned version?" check.
/// Returns 0 if everything matches, 1 on any drift.
/// </summary>
public static class VerifyCommand
{
    public static int Run()
    {
        Log.Section($"verify: SHA-256 baseline {ExpectedHashes.PanelVersion}");
        var panelDir = Path.Combine(Workspace.Root, "panel");
        if (!Directory.Exists(panelDir))
        {
            Log.Err($"panel/ not found at {panelDir}");
            return 1;
        }

        var av = AssetVerifier.Verify(panelDir);
        AssetVerifier.Report(av);
        return av.Ok ? 0 : 1;
    }
}
