namespace JRK.JevRunner.Requests;

/// <summary>
/// Identifies the model to use when executing a request.
/// </summary>
/// <param name="Model">The model identifier sent to the API.</param>
public record JevModel(string Model)
{
    /// <summary>
    /// Gets the model identified by <c>jev-latest</c>.
    /// </summary>
    public static JevModel Latest => new("jev-latest");
    
    /// <summary>
    /// Specific model for 1.13.0
    /// </summary>
    public static JevModel Version1_13_0 => new("jev-1.13.0");
    
    /// <summary>
    /// Specific model for 1.14.0. This model is required for Box questions
    /// </summary>
    public static JevModel Version1_14_0 => new("jev-1.14.0");

    /// <summary>
    /// Gets the preview model
    /// </summary>
    public static JevModel Preview => new("jev-preview");
}
