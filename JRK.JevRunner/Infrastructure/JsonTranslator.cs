using System.Text.Json.Nodes;
using JRK.JevRunner.Requests;

namespace JRK.JevRunner.Infrastructure;

/// <summary>
/// Translates JevRunner requests into the JSON payload expected by the Jev service.
/// </summary>
internal class JsonTranslator
{
    /// <summary>
    /// Creates the JSON payload for a request, including its state, model, and questions.
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

public class AssetState : Dictionary<string,string>
{
    private readonly Asset _asset;
    internal Dictionary<string, Asset> Assets { get; set; } = [];

    public AssetState AddAsset(string name, Asset asset)
    {
        Assets[name] = asset;
        Add(name, asset.Name);
        return this;
    }
    
    
}