using System.Diagnostics.CodeAnalysis;
using System.Text;
using TruePath;

namespace JRK.JevRunner.Image;

/// <summary>Contains detected image properties and encoded bytes after local validation succeeds.</summary>
/// <remarks>Local validation does not guarantee API acceptance or check request-wide limits.</remarks>
/// <param name="Format">The detected upload format: png or jpg.</param>
/// <param name="Width">The image width in pixels.</param>
/// <param name="Height">The image height in pixels.</param>
/// <param name="FileSizeBytes">The encoded file size before base64 conversion.</param>
/// <param name="Bytes">The original encoded bytes read during validation.</param>
public sealed record ValidImageResult(string Format, int Width, int Height, long FileSizeBytes, Byte[] Bytes);

/// <summary>Contains local image validation failures and a dimension-based scaling suggestion.</summary>
/// <param name="Errors">The failures found by the local validation checks.</param>
public sealed record InvalidImageResult(ImageValidationError[] Errors)
{
    
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
}

/// <summary>Represents either a locally valid image with its bytes or validation failures.</summary>
/// <remarks>Pattern-match the result to access image metadata or errors and scaling suggestions.</remarks>
public union ImageValidationResult(ValidImageResult, InvalidImageResult)
{
    /// <summary>Whether all local image checks passed.</summary>
    public bool IsValid => this is ValidImageResult;
    
    /// <summary>Throws with the validation error messages when any local image check failed.</summary>
    /// <exception cref="InvalidOperationException">The result contains one or more validation errors.</exception>
    public void ThrowIfInvalid()
    {
        switch (this)
        {
            case InvalidImageResult invalidImageResult:
                ThrowHelper.Throw(invalidImageResult);
                return;
            case ValidImageResult:
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

/// <summary>Formats image validation failures as an exception.</summary>
internal static class ThrowHelper
{
    /// <summary>Throws an exception containing all reported image validation messages.</summary>
    [DoesNotReturn]
    public static void Throw(InvalidImageResult imageResult)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Image is not valid to use in a Jev Query");

        foreach (var imageValidationError in imageResult.Errors)
        {
            builder.AppendLine(imageValidationError.GetErrorMessage());
        }

        throw new InvalidOperationException(builder.ToString());
    }
}