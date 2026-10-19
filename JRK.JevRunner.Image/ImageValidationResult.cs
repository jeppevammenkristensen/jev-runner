using System.Diagnostics.CodeAnalysis;
using System.Text;
using SkiaSharp;

namespace JRK.JevRunner.Image;

/// <summary>Describes a file's detected image properties and any local upload-readiness failures.</summary>
/// <remarks>A valid result does not guarantee API acceptance or validate request-wide limits.</remarks>
public sealed record ImageValidationResult(
    string? Format,
    int? Width,
    int? Height,
    long FileSizeBytes,
    IReadOnlyList<ImageValidationError> Errors)
{
    /// <summary>Whether all local image checks passed.</summary>
    public bool IsValid => Errors.Count == 0;

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

public union ImageValidationError(ImageDimensionError, string)
{
    public string GetErrorMessage()
    {
        return this switch
        {
            ImageDimensionError e => e.ToString(),
            string s => s
        };
    }
}

public enum ImageDimension
{
    Width,
    Height,
}

public enum ImageDimensionViolation
{
    ToHigh,
    ToSmall
}

public record ImageDimensionError(
    ImageDimension Dimension,
    ImageDimensionViolation Violation,
    int Limit,
    int Actual
)
{
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
