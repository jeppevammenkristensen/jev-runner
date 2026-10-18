using System.Text.Json.Nodes;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Represents a noul, choice, or score question that can be added to a request.
/// </summary>
public union Question(NoulQuestion, ChoiceQuestion, ScoreQuestion)
{
    /// <summary>
    /// Adds the question's JSON representation to the supplied object under its name.
    /// </summary>
    /// <param name="obj">The object containing the request's named questions; an existing entry with the same name is replaced.</param>
    public void AddTo(JsonObject obj)
    {
        if (this is NoulQuestion noul)
        {
            obj[noul.Name] = noul.Node;
        }
        else if (this is ChoiceQuestion choice)
        {
            obj[choice.Name] = choice.Node;
        }
        else if (this is ScoreQuestion score)
        {
            obj[score.Name] = score.Node;
        }
    }
}

public union Asset(ImageAsset)
{
    public void AddTo(JsonObject obj)
    {
        if (this is ImageAsset imageAsset)
        {
            obj[imageAsset.Name] = imageAsset.Node;   
        }
    }

    public string Name => this switch
    {
        ImageAsset im => im.Name,
        _ => throw new NotImplementedException()
    };
}

/// <summary>
/// An image assert
/// </summary>
/// <param name="Name"></param>
/// <param name="Format"></param>
/// <param name="Base64"></param>
public record ImageAsset(string Name, string Format, string Base64)
{
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
}
