using JetBrains.Annotations;

namespace JRK.JevRunner.Responses;

/// <summary>
/// Wraps a score value, rejecting values below zero.
/// </summary>
[UsedImplicitly]
public readonly record struct Score
{
    /// <summary>
    /// Gets the stored score value.
    /// </summary>
    public double Value { get; }
    
    /// <summary>
    /// Creates a score, rejecting only values below zero.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below zero.</exception>
    public Score(double value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
        Value = value;
    }
}
