namespace JRK.JevRunner.Image;

/// <summary>
/// An error signaling that a given file is not png or jpg
/// </summary>
public record NotASupportedImageError
{
    public string Message => "The given file is not an Image or not the support image types png,jpg";
}