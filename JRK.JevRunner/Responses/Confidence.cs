namespace JRK.JevRunner.Responses;

/// <summary>
/// Represents Choice or Score probability-distribution concentration on a zero-to-one scale.
/// </summary>
/// <remarks>Confidence summarizes the distribution, not the accuracy of the answer.</remarks>
public readonly record struct Confidence
{
    /// <summary>
    /// Gets the confidence value between zero and one, inclusive.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Creates a confidence value from a finite number between zero and one, inclusive.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is non-finite or outside [0, 1].</exception>
    /// <param name="value">The finite distribution confidence in the inclusive range [0, 1].</param>
    public Confidence(double value)
    {
        ErrorMessages.ThrowIfNotBetween0And1(value, nameof(Confidence));
        Value = value;
    }
    
    /// <summary>
    /// Converts a number to confidence using the constructor's validation.
    /// </summary>
    /// <param name="value">The finite distribution confidence in the inclusive range [0, 1].</param>
    public static implicit operator Confidence(double value) => new(value);
    /// <summary>
    /// Returns the underlying confidence value.
    /// </summary>
    /// <param name="confidence">The confidence whose numeric value is returned.</param>
    public static implicit operator double(Confidence confidence) => confidence.Value;
}
