using PureCrack.Provisioning;

namespace PureCrack.Cli;

/// <summary>
/// Run the host-environment provisioning steps without launching the relay
/// or panel. Equivalent to running the old Launch.ps1's setup section.
///
/// Useful for: scripted machine setup, troubleshooting "is the regkey set?",
/// preparing a box where the panel will be launched later by some other
/// orchestration. Idempotent.
/// </summary>
public static class ProvisionCommand
{
    public static int Run()
    {
        Provision.Run();
        return 0;
    }
}
