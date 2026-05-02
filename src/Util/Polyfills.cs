// Polyfills required to use C# 9+ language features on net472.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Sentinel type the C# compiler looks up when emitting <c>init</c>
    /// accessors. .NET 5+ ships it in the BCL; net472 doesn't, so we declare
    /// it ourselves. Internal so it doesn't leak into the public API.
    /// </summary>
    internal static class IsExternalInit { }
}
