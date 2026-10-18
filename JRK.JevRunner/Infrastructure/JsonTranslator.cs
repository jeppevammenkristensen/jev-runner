using System.Text.Json.Nodes;

namespace JRK.JevRunner.Infrastructure;

/// <summary>
/// Translates JevRunner requests into the JSON payload expected by the Jev service.
/// </summary>
internal class JsonTranslator
{
    /// <summary>
    /// Creates the JSON payload for a request, including its state, model, questions, and assets.
    /// </summary>
    internal static JsonObject CreateRequestJson(Request request)
    {
        var json = new JsonObject
        {
            {"state", request.State.Node},
            {"model", request.Model.Model}
        };

        var question = new JsonObject();

        foreach (var requestQuestion in request.Questions)
        {
            requestQuestion.AddTo(question);
        }
        
        json.Add("questions", question);
        
        var assets = new JsonObject();

        foreach (var asset in request.Assets)
        {
            asset.AddTo(assets);
        }
        
        json.Add("assets", assets);

        return json;
    }
}