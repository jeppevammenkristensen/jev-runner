using System.Text.Json.Nodes;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Represents a named image with its format and base64-encoded contents.
/// </summary>
/// <param name="Name">The name identifying the image in the request's assets map.</param>
/// <param name="Format">The image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.</param>
/// <param name="Base64">The image bytes encoded as base64.</param>
public record ImageAsset(string Name, string Format, string Base64)
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
            jsonObject["data_base64"] = Base64;
            return jsonObject;
        }
    }

    public static ImageAsset FromByteArray(string name, string format, Byte[] bytes)
    {
        return new ImageAsset(name, format, Convert.ToBase64String(bytes));
    }
}