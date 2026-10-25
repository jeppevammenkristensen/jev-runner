using System.Diagnostics.CodeAnalysis;
using System.Text;
using SkiaSharp;

namespace JRK.JevRunner.Image;

/// <summary>Describes a file's detected image properties and any local upload-readiness failures.</summary>
/// <remarks>A valid result does not guarantee API acceptance or validate request-wide limits.</remarks>
/// <param name="Format">The detected image format, or null when no supported format was identified.</param>
/// <param name="Width">The image width in pixels, or null when it could not be determined.</param>
/// <param name="Height">The image height in pixels, or null when it could not be determined.</param>
/// <param name="FileSizeBytes">The encoded file size in bytes before base64 conversion.</param>
/// <param name="Errors">The failures found by the local image validation checks.</param>
public sealed record ImageValidationResult(
    string? Format,
    int? Width,
    int? Height,
    long FileSizeBytes,
    IReadOnlyList<ImageValidationError> Errors)
{
    /// <summary>Whether all local image checks passed.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Gets a scaling suggestion based on the reported dimension violations.</summary>
    /// <remarks>Non-dimension errors are ignored. No dimension violations yields None;
    /// mixed undersized and oversized dimensions yield ScalingNotPossible.
    /// A suggestion does not guarantee that proportional resizing will satisfy both bounds.</remarks>
    public ImageSuggestedScaling SuggestedScaling
    {
        get
        {
            List<ImageDimensionError> errors = new();

            foreach (var error in Errors)
            {
                if (error is ImageDimensionError imageValidationError)
                {
                    errors.Add(imageValidationError);
                }
            }

            if (errors.Count == 0)
            {
                return ImageSuggestedScaling.None;
            }

            var distinctErrorValidations = errors.Select(x => x.Violation).Distinct().ToList();
            if (distinctErrorValidations.Count > 1)
            {
                return ImageSuggestedScaling.ScalingNotPossible;
            }

            return distinctErrorValidations[0] switch
            {
                ImageDimensionViolation.ToHigh => ImageSuggestedScaling.Downscale,
                ImageDimensionViolation.ToSmall => ImageSuggestedScaling.Upscale,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    /// <summary>Throws with the validation error messages when any local image check failed.</summary>
    /// <exception cref="InvalidOperationException">The result contains one or more validation errors.</exception>
    public void ThrowIfInvalid()
    {
        if (IsValid) return;
        var builder = new StringBuilder();
        builder.AppendLine("Image is not valid to use in a Jev Query");

        foreach (var imageValidationError in Errors)
        {
            builder.AppendLine(imageValidationError.GetErrorMessage());
        }

        throw new InvalidOperationException(builder.ToString());
    }
}

/// <summary>Represents either a structured dimension violation or a general image validation message.</summary>
public union ImageValidationError(ImageDimensionError, string)
{
    /// <summary>Returns the dimension violation description or the stored validation message.</summary>
    public string GetErrorMessage()
    {
        return this switch
        {
            ImageDimensionError e => e.ToString(),
            string s => s
        };
    }
}

/// <summary>Identifies the image axis checked against a dimension limit.</summary>
public enum ImageDimension
{
    /// <summary>The horizontal size of the image in pixels.</summary>
    Width,
    /// <summary>The vertical size of the image in pixels.</summary>
    Height,
}

/// <summary>Identifies whether an image dimension exceeds the maximum or falls below the minimum.</summary>
public enum ImageDimensionViolation
{
    /// <summary>The dimension exceeds the inclusive maximum.</summary>
    ToHigh,
    /// <summary>The dimension falls below the inclusive minimum.</summary>
    ToSmall
}

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
            {Dimension: ImageDimension.Height, Violation: ImageDimensionViolation.ToSmall} => "The Height is to high",
            {Dimension: ImageDimension.Width, Violation: ImageDimensionViolation.ToHigh} => "The Width is to high",
            {Dimension: ImageDimension.Width, Violation: ImageDimensionViolation.ToSmall} => "The Width is to high",
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
