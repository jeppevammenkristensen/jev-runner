using System.Text.Json.Nodes;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Represents a named asset that can be included in a request.
/// </summary>
public union Asset(ImageAsset)
{
    /// <summary>
    /// Adds the asset's JSON representation to the supplied object under its name.
    /// </summary>
    /// <param name="obj">The request's assets object; an existing entry with the same name is replaced.</param>
    public void AddTo(JsonObject obj)
    {
        if (this is ImageAsset imageAsset)
        {
            obj[imageAsset.Name] = imageAsset.Node;   
        }
    }

    /// <summary>
    /// Gets the name used to reference the asset from request state.
    /// </summary>
    public string Name => this switch
    {
        ImageAsset im => im.Name,
        _ => throw new NotImplementedException()
    };
}