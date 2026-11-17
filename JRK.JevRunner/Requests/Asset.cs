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
}
