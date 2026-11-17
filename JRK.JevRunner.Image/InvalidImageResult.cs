namespace JRK.JevRunner.Image;

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