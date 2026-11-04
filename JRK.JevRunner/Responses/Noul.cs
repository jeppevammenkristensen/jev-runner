namespace JRK.JevRunner.Responses;

/// <summary>
/// Represents the probability that a yes/no answer is yes.
/// </summary>
public readonly record struct Noul
{
    /// <summary>
    /// Creates a probability from a finite number between zero and one, inclusive.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is non-finite or outside [0, 1].</exception>
    /// <param name="value">The finite probability of yes in the inclusive range [0, 1].</param>
    public Noul(double value)
    {
        ErrorMessages.ThrowIfNotBetween0And1(value);
        Value = value;
    }
    
    /// <summary>
    /// Gets the probability between zero and one, inclusive.
    /// </summary>
    public double Value { get; }
    
    /// <summary>
    /// Gets whether the probability is at least 0.5, including an equal yes/no probability.
    /// </summary>
    public bool Success => Value >= 0.5;
    
    /// <summary>
    /// Converts the probability to a Boolean using the inclusive 0.5 threshold.
    /// </summary>
    /// <param name="noul">The probability to compare with the inclusive 0.5 threshold.</param>
    public static implicit operator Boolean(Noul noul) => noul.Success;
    /// <summary>
    /// Converts a number to a probability using the constructor's validation.
    /// </summary>
    /// <param name="value">The finite probability of yes in the inclusive range [0, 1].</param>
    public static implicit operator Noul(double value) => new(value);
}
