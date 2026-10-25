using SkiaSharp;

namespace JRK.JevRunner.Image;

/// <summary>Describes an image dimension that falls outside an inclusive pixel limit.</summary>
/// <param name="Dimension">The image axis that violates the limit.</param>
/// <param name="Violation">Whether the dimension exceeds the maximum or falls below the minimum.</param>
/// <param name="Limit">The violated minimum or maximum limit in pixels.</param>
/// <param name="Actual">The measured dimension in pixels.</param>
public record ImageDimensionError(
    ImageDimension Dimension,
    ImageDimensionViolation Violation,
    int Limit,
    int Actual
)
{
    /// <summary>Formats the dimension violation with its expected limit and actual pixel value.</summary>
    public override string ToString()
    {
        string temp = this switch
        {
            {Dimension: ImageDimension.Height, Violation: ImageDimensionViolation.ToHigh} => "The Height is to high",
            {Dimension: ImageDimension.Height, Violation: ImageDimensionViolation.ToSmall} => "The Height is to small",
            {Dimension: ImageDimension.Width, Violation: ImageDimensionViolation.ToHigh} => "The Width is to high",
            {Dimension: ImageDimension.Width, Violation: ImageDimensionViolation.ToSmall} => "The Width is to small",
            _ => throw new InvalidOperationException()
        };

        return temp + $" expected {Limit} but value was {Actual}";
    }
    
    /// <summary>Returns each width and height violation of the inclusive limits, width first.</summary>
    public static IEnumerable<ImageValidationError> GetErrors(SKImageInfo info, int minimumDimension,
        int maximumDimension)
    {
        if (info.Width < minimumDimension)
        {
            yield return new ImageDimensionError(ImageDimension.Width, ImageDimensionViolation.ToSmall,
                minimumDimension, info.Width);
        }

        if (info.Width > maximumDimension)
        {
            yield return new ImageDimensionError(ImageDimension.Width, ImageDimensionViolation.ToHigh, maximumDimension,
                info.Width);
        }

        if (info.Height < minimumDimension)
        {
            yield return new ImageDimensionError(ImageDimension.Height, ImageDimensionViolation.ToSmall,
                minimumDimension, info.Height);
        }

        if (info.Height > maximumDimension)
        {
            yield return new ImageDimensionError(ImageDimension.Height, ImageDimensionViolation.ToHigh,
                maximumDimension, info.Height);
        }
    }
}