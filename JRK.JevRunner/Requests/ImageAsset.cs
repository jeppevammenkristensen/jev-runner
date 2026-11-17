using System.Text.Json.Nodes;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Represents a named image backed by bytes, Base64 text
/// </summary>
/// <param name="Name">The name identifying the image in the request's assets map.</param>
/// <param name="Format">The image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.</param>
/// <param name="FileData">The image contents as raw bytes or base64-encoded text.</param>
/// <remarks>Using this will Base64 encode the image</remarks>
public record ImageAsset(string Name, string Format, FileData FileData)
{
    /// <summary>
    /// Creates image metadata with either Base64 data or a multipart part reference, without its asset name.
    /// </summary>
    public JsonNode Node
    {
        get
        {
            var jsonObject = new JsonObject();
            jsonObject["type"] = "image";
            jsonObject["format"] = Format;
            jsonObject["data_base64"] = FileData.Base64;
            return jsonObject;
        }
    }

    /// <summary>
    /// Create an ImageAsset from a byte array
    /// </summary>
    /// <param name="name">The name identifying the image in the request's assets map.</param>
    /// <param name="format">The image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.</param>
    /// <param name="bytes">The raw image file contents.</param>
    /// <returns>An image asset backed by the supplied bytes.</returns>
    /// <remarks>This has been marked Obsolete as since 0.6.0 the class takes the <see cref="FileData"/> union type that supports both string and bytes</remarks>
    [Obsolete("Use new ImageAsset(name, format, bytes) instead.")]
    public static ImageAsset FromByteArray(string name, string format, Byte[] bytes)
    {
        return new ImageAsset(name, format, bytes);
    }
}
