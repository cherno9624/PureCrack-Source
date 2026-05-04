using System;
using System.Security.Cryptography.X509Certificates;

namespace PureCrack.Cli;

/// <summary>
/// The exact subject DNs we have ever generated for the relay cert. Listed
/// here so cleanup / doctor target only certs we know we own — never a
/// substring match on "PureCrack" that could clobber an unrelated user
/// cert with that string in its O= or OU= field.
///
/// When CertManager generates new cert shapes, add the new DN here. The
/// list is append-only (legacy DNs stay so old installs can still be
/// cleaned up correctly).
/// </summary>
internal static class CertSubjects
{
    private static readonly string[] Known =
    {
        "CN=api.purecoder.io, O=PureCrack, OU=Relay", // current (post-hardening leaf)
        "CN=api.purecoder.io",                         // legacy single-CN form
    };

    public static bool IsOurCert(X509Certificate2 c)
    {
        var subject = c.SubjectName.Name;
        if (subject == null) return false;
        foreach (var known in Known)
            if (string.Equals(subject, known, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
