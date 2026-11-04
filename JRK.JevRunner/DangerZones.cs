using System.Text.RegularExpressions;

namespace JRK.JevRunner;

/// <summary>
/// DangerZones are attributes you can include in the request for
/// toggling areas like Images or LongContext
/// </summary>
public static partial class DangerZones
{
    /// <summary>
    /// Header to use when you want to include Images (currently Dreamerready)
    /// </summary>
    public const string Images = "TypeSafe-Danger-Zone-Images";
    /// <summary>
    /// Header to use to "disable" the max context size
    /// </summary>
    /// <remarks>TypeSafe states for longer context above the default 64 kb that "It is slower and less accurate than staying within the default limits, so turn it on only for requests that need it."</remarks>
    public const string LongContext = "TypeSafe-Danger-Zone-Long-Context";

    /// <summary>Gets the regular expression matching experimental feature header prefixes.</summary>
    /// <returns>A cached regular expression for the danger-zone header prefix.</returns>
    [GeneratedRegex("^TypeSafe-Danger-Zone")]
    public static partial Regex DangerZoneHeaderRegex();
}
