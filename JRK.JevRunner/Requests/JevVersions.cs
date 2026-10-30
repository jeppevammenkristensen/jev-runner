// ReSharper disable InconsistentNaming
namespace JRK.JevRunner.Requests;

/// <summary>
/// Provides model identifiers for supported Jev versions.
/// </summary>
public static class JevVersions
{
    /// <summary>Gets the latest model identifier.</summary>
    public const string Latest = "jev-latest";

    /// <summary>Gets the preview model identifier.</summary>
    public const string Preview = "jev-preview";

    /// <summary>Gets the model identifier for version 1.13.0.</summary>
    public const string Version1_13_0 = "jev-1.13.0";

    /// <summary>Gets the model identifier for version 1.14.0.</summary>
    public const string Version1_14_0 = "jev-1.14.0";
    
    public static HashSet<string> Versions =>
    [
        Latest,
        Preview,
        Version1_13_0,
        Version1_14_0
    ];
}
