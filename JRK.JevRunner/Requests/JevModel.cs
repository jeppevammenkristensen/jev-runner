namespace JRK.JevRunner.Requests;

/// <summary>
/// Identifies the model to use when executing a request.
/// </summary>
/// <param name="Model">The model identifier sent to the API.</param>
/// <remarks>You can use the <see cref="JevVersions"/></remarks>
public record JevModel(string Model)
{
    /// <summary>
    /// Returns true if the version is latest <see cref="JevVersions.Latest"/>
    /// </summary>
    public bool IsLatest => Model.Equals(JevVersions.Latest);
    
    /// <summary>
    /// returns true if the version is the preview <see cref="JevVersions.Preview"/>
    /// </summary>
    public bool IsPreview => Model.Equals(JevVersions.Preview);

    /// <summary>
    /// Returns true if this is a known version
    /// </summary>
    /// <remarks>This checks the JevVersions.Versions so in the case a new
    /// version has been added. This method can falsely report an error</remarks>
    public bool IsKnownVersion => JevVersions.Versions.Contains(Model);

    public bool IsVersion(string version) => Model.Equals(version);
    
    /// <summary>
    /// Gets the model identified by <c>jev-latest</c>.
    /// </summary>
    public static JevModel Latest => new(JevVersions.Latest);
    
    /// <summary>
    /// Specific model for 1.13.0
    /// </summary>
    public static JevModel Version1_13_0 => new(JevVersions.Version1_13_0);
    
    /// <summary>
    /// Specific model for 1.14.0. This model is required for Box questions
    /// </summary>
    /// <remarks>This adds support for the Box Question </remarks>
    public static JevModel Version1_14_0 => new(JevVersions.Version1_14_0);

    /// <summary>
    /// Gets the preview model
    /// </summary>
    public static JevModel Preview => new(JevVersions.Preview);
    
    
}
