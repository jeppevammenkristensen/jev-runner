using System.Collections;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;

namespace JRK.JevRunner;

/// <summary>
/// Holds the state, model, and questions to send to the API.
/// </summary>
public class Request
{
    /// <summary>
    /// Creates a request with the supplied state and model, defaulting to the latest model when none is supplied.
    /// </summary>
    public Request(JevMessage state, JevModel? model)
    {
        State = state;
        Model = model ?? JevModel.Latest;

        if (State is AssetState assetState)
        {
            foreach (var asset in assetState.Assets)
            {
                Assets.Add(asset.Value);
            }
        }
    }

    /// <summary>
    /// Gets the state used as context for the request's questions.
    /// </summary>
    public JevMessage State { get; }

    /// <summary>
    /// Gets the model to use when executing the request.
    /// </summary>
    public JevModel Model { get; }

    /// <summary>
    /// Gets or sets the questions to include in the request.
    /// </summary>
    public List<Question> Questions { get; set; } = [];

    public List<Asset> Assets { get; set; } = [];
}
