using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;

namespace JRK.JevRunner;

/// <summary>
/// Holds the state, model, questions, and assets to send to the API.
/// </summary>
public class Request
{
    /// <summary>
    /// Creates a request with the supplied state and model, defaulting to the latest model when none is supplied.
    /// </summary>
    /// <remarks>Assets from asset-backed state are copied into the request's asset list during construction.</remarks>
    public Request(JevMessage state, JevModel? model = null)
    {
        State = state;
        Model = EnsureCorrectJevModel(model);

        if (State is AssetState assetState)
        {
            foreach (var asset in assetState.Assets)
            {
                Assets.Add(asset.Value);
            }
        }
    }

    private JevModel EnsureCorrectJevModel(JevModel? model)
    {
        if (model != null)
        {
            return model;
        }

        if (Questions.Any(x => x is BoxQuestion))
            return JevModel.Preview;

        return JevModel.Latest;
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

    /// <summary>
    /// Gets or sets the assets to include in the request's assets map, keyed by each asset's name.
    /// </summary>
    public List<Asset> Assets { get; set; } = [];
}
