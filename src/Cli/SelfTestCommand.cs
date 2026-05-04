using PureCrack.Util;
using PureCrack.Verify;

namespace PureCrack.Cli;

/// <summary>
/// Run the wire-format / crypto round-trip self-tests. No relay, no panel,
/// no admin (beyond manifest). Catches silent breakage from BCL changes,
/// embedded asset drift, or refactor bugs in the relay's encode/decode
/// path.
///
/// Returns 0 when every check passes, 1 otherwise.
/// </summary>
public static class SelfTestCommand
{
    public static int Run()
    {
        Log.Section("selftest: wire-format and crypto round-trip checks");
        var r = SelfTest.Run();
        SelfTest.Report(r);
        return r.Ok ? 0 : 1;
    }
}
