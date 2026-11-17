using System.Text.Json.Nodes;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Represents a named asset that can be included in a request.
/// </summary>
public union Asset(ImageAsset, IImageStreamAsset)
{
    /// <summary>
    /// Adds the asset's JSON representation to the supplied object under its name.
    /// </summary>
    /// <param name="obj">The request's assets object; an existing entry with the same name is replaced.</param>
    public void AddTo(JsonObject obj)
    {
        obj[AssetName] = Node();
    }

    /// <summary>
    /// Creates asset metadata using inline Base64 data or a multipart part reference.
    /// </summary>
    /// <returns>The asset's JSON representation without its asset name.</returns>
    internal JsonNode Node()
    {
        return this switch
        {
            ImageAsset ia => ia.Node,
            IImageStreamAsset imageStreamAsset => GenerateNodeFromImageStream(imageStreamAsset)
        };
    }

    /// <summary>
    /// Creates image metadata referring to a multipart field without opening the image stream.
    /// </summary>
    /// <param name="imageStreamAsset">The image supplying the format and multipart field name.</param>
    /// <returns>A JSON object describing the image and its multipart part reference.</returns>
    private JsonNode GenerateNodeFromImageStream(IImageStreamAsset imageStreamAsset)
    {
        var jsonObject = new JsonObject
        {
            ["type"] = "image",
            ["format"] = imageStreamAsset.Format,
            ["part"] = imageStreamAsset.Name
        };
        return jsonObject;
    }

    /// <summary>
    /// Gets the name used to reference the asset from request state.
    /// </summary>
    public string AssetName => this switch
    {
        ImageAsset im => im.Name,
        IImageStreamAsset s => s.AssetName,
    };

    /// <summary>
    /// Reads the encoded image bytes from inline Base64 data or an opened image stream.
    /// </summary>
    /// <returns>The decoded inline bytes or a new array containing the stream's remaining bytes.</returns>
    /// <remarks>Opened image streams are disposed even if reading fails. File and stream errors propagate to the caller.</remarks>
    public byte[] ReadAsBytes()
    {
        return this switch
        {
            IImageStreamAsset imageStreamAsset => GetByteArray(imageStreamAsset),
            ImageAsset imageAsset => imageAsset.FileData.Bytes,
        };
    }

    /// <summary>
    /// Reads the image stream into a new byte array and disposes the opened stream.
    /// </summary>
    /// <param name="imageStreamAsset">The asset providing the image stream to read.</param>
    /// <returns>The bytes read from the stream's current position to its end.</returns>
    private byte[] GetByteArray(IImageStreamAsset imageStreamAsset)
    {
        using var openRead = imageStreamAsset.OpenRead();
        using var buffer = new MemoryStream();
        openRead.CopyTo(buffer);
        return buffer.ToArray();
    }
}
