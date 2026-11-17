namespace JRK.JevRunner.Image;

/// <summary>
/// An error signaling that the maximum file size has been execeeded
/// </summary>
/// <param name="MaximumFileSizeBytes"></param>
public record ImageSizeExceededError(long MaximumFileSizeBytes)
{
    /// <summary>
    /// Gets an error message reporting the maximum file size in whole mebibytes.
    /// </summary>
    public string Message => $"Message exceed the limit of {MaximumFileSizeBytes / 1024 / 1024} mb";
}
