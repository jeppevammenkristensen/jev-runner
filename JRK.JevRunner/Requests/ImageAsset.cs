using System.Text.Json.Nodes;

namespace JRK.JevRunner.Requests;

public readonly record struct Base64String(string Base64Encoded)
{
    public static implicit operator Base64String(string base64Encoded) => new(base64Encoded);
    public static implicit operator String(Base64String base64Encoded) => base64Encoded.Base64Encoded;
}

/// <summary>
///
/// </summary>
public union FileData(byte[], Base64String)
{
    /// <summary>
    /// Returns the data as Base64 encoded
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public string Base64 => this switch
    {
        byte[] bytes => Convert.ToBase64String(bytes),
        Base64String s => s.Base64Encoded,
        _ => throw new ArgumentOutOfRangeException()
    };

    public byte[] Bytes => this switch
    {
        byte[] bytes => bytes,
        Base64String s => Convert.FromBase64String(s),
        _ => throw new ArgumentOutOfRangeException()
    };
}


/// <summary>
/// Represents a named image with its format and base64-encoded contents.
/// </summary>
/// <param name="Name">The name identifying the image in the request's assets map.</param>
/// <param name="Format">The image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.</param>
/// <param name="FileData">The image bytes encoded as base64.</param>
public record ImageAsset(string Name, string Format, FileData FileData)
{
    /// <summary>
    /// Creates the image payload containing its type, format, and base64 data, without its asset name.
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
    /// <param name="name"></param>
    /// <param name="format"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    /// <remarks>This has been marked Obsolete as since 0.6.0 the class takes the <see cref="FileData"/> union type that supports both string and bytes</remarks>
    [Obsolete("Use new ImageAsset(name, format, bytes) instead.")]
    public static ImageAsset FromByteArray(string name, string format, Byte[] bytes)
    {
        return new ImageAsset(name, format, bytes);
    }
}
