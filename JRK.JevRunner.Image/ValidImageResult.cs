namespace JRK.JevRunner.Image;

/// <summary>Contains detected image properties and encoded bytes after local validation succeeds.</summary>
/// <remarks>Local validation does not guarantee API acceptance or check request-wide limits.</remarks>
/// <param name="Format">The detected upload format: png or jpg.</param>
/// <param name="Width">The image width in pixels.</param>
/// <param name="Height">The image height in pixels.</param>
/// <param name="FileSizeBytes">The encoded file size before base64 conversion.</param>
/// <param name="Bytes">The original encoded bytes read during validation.</param>
public sealed record ValidImageResult(string Format, int Width, int Height, long FileSizeBytes, Byte[] Bytes);