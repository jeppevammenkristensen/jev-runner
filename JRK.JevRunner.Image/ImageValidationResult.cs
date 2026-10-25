using System.Diagnostics.CodeAnalysis;
using System.Text;

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