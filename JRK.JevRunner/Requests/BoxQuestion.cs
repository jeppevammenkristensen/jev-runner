using System.Text.Json.Nodes;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Defines a named question that locates a target in an image using a bounding box.
/// </summary>
/// <param name="Name">The question identifier used to associate the returned answer.</param>
/// <param name="Instructions">Instructions identifying the target and the image to search.</param>
/// <remarks>Requires an image in the request, a Box-capable model such as <c>jev-1.14.0</c>,
/// and the experimental images header. Box questions take no criteria.</remarks>
public record BoxQuestion(string Name, JevMessage Instructions) : IQuestion
{
    /// <summary>
    /// Gets the question payload containing the box type and serialized instructions.
    /// </summary>
    public JsonNode? Node
    {
        get
        {
            var jsonObject = new JsonObject();
            jsonObject["type"] = "box";
            jsonObject["instructions"] = Instructions.Node;
            return jsonObject;
        }
    }
}
