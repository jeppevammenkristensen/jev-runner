namespace JRK.JevRunner.SourceGenerators;

/// <summary>
/// Identifies the criteria or choice configuration required before a question is added.
/// </summary>
internal enum ConfigurationKind
{
    /// <summary>
    /// Configures a yes-or-no question using its criteria.
    /// </summary>
    Noul,
    /// <summary>
    /// Configures a choice question using its choices and selection criteria.
    /// </summary>
    Choice,
    /// <summary>
    /// Configures a score question using its scoring criteria.
    /// </summary>
    Score,
    /// <summary>
    /// Configures a bounding-box question using its object-location criteria.
    /// </summary>
    Box
}
