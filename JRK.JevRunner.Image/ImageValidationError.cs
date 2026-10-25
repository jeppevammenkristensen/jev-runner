namespace JRK.JevRunner.Image;

/// <summary>Represents either a structured dimension violation or a general image validation message.</summary>
public union ImageValidationError(ImageDimensionError, NotASupportedImageError,ImageSizeExceededError,string)
{
    /// <summary>Returns the dimension violation description or the stored validation message.</summary>
    public string GetErrorMessage()
    {
        return this switch
        {
            ImageDimensionError e => e.ToString(),
            string s => s,
            NotASupportedImageError e => e.Message,
            ImageSizeExceededError e => e.Message
        };
    }
}