namespace JRK.JevRunner;

/// <summary>Identifies a named state field for backtick-quoted interpolation into instructions.</summary>
/// <remarks>Validates the name, but does not check whether the field exists in request state.</remarks>
public class StateReference : IBackTick
{
    /// <summary>Creates a state reference without whitespace or backticks.</summary>
    /// <param name="value">The nonempty state field name.</param>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name is empty or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">The name contains whitespace or backticks.</exception>
    public StateReference(string value)
    {
        StateRegex.ThrowIfInvalid(value);
        Value = value;
    }
    
    /// <summary>Creates a validated state reference from a field name.</summary>
    /// <param name="value">The state field name to validate and wrap.</param>
    /// <returns>A reference containing the supplied name.</returns>
    public static implicit operator StateReference(string value) => new(value);

    public string Value { get; }
    public string BackTickQuoted => $"`{Value}`";
}
