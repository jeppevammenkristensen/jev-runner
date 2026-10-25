namespace JRK.JevRunner.Image;

/// <summary>Identifies whether an image dimension exceeds the maximum or falls below the minimum.</summary>
public enum ImageDimensionViolation
{
    /// <summary>The dimension exceeds the inclusive maximum.</summary>
    ToHigh,
    /// <summary>The dimension falls below the inclusive minimum.</summary>
    ToSmall
}