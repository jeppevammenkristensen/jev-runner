namespace JRK.JevRunner.Image;

/// <summary>Describes whether proportional scaling can address the latest image validation failures.</summary>
public enum ImageSuggestedScaling
{
    /// <summary>
    /// If you can solve validation by upscaling
    /// </summary>
    Upscale,

    /// <summary>
    /// If you can solve validation by downscaling
    /// </summary>
    Downscale,

    /// <summary>
    /// If scaling can't fix the issues. 
    /// </summary>
    /// <remarks>For instance in scenarios where one dimension is to low and the other is to high</remarks>
    ScalingNotPossible,

    /// <summary>
    /// Scaling is not required
    /// </summary>
    None
}