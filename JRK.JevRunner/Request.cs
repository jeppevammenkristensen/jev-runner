using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;

namespace JRK.JevRunner;

/// <summary>
/// Holds the state, model, questions, and assets to send to the API.
/// </summary>
public class Request
{
    /// <summary>
    /// This will be true if Request was initialized with a null Jev Model
    /// </summary>
    /// <remarks>It's used reevalute the model when a query is run. </remarks>
    internal bool PassedModelWasNull { get; private set; }
    
    /// <summary>
    /// If set to true a validation error will throw an exception before
    /// the request is performed
    /// </summary>
    public bool StrictValidation { get; set; }

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

        PassedModelWasNull = true;

        return AutoDetectJevModel();
    }

    
    

    /// <summary>
    /// Gets the best JevModel fit for the request. 
    /// </summary>
    /// <returns></returns>
    internal JevModel AutoDetectJevModel()
    {
        // In the current time of writing Latest and Preview are aligned
        // So JevModel.Latest is used
        return JevModel.Latest;
    }

    /// <summary>
    /// Gets the state used as context for the request's questions.
    /// </summary>
    public JevMessage State { get; }

    /// <summary>
    /// Gets the model to use when executing the request.
    /// </summary>
    public JevModel Model { get; internal set; }

    /// <summary>
    /// Gets or sets the questions to include in the request.
    /// </summary>
    public List<Question> Questions { get; set; } = [];

    /// <summary>
    /// Gets or sets the assets to include in the request's assets map, keyed by each asset's name.
    /// </summary>
    public List<Asset> Assets { get; set; } = [];
    
    /// <summary>
    /// Enables long context for a request. (The default is 64kb and the suggested max by TypeSafe)
    /// </summary>
    /// <remarks>Toggling this means that there is no limit to the context size. However TypeSafe instructs that this makes the request slower and less precise</remarks>
    public bool LongContextEnabled { get; set; } = false;
}

internal static class RequestValidator
{
    public static SimpleValidation Validate(Request request)
    {
        if (request.Model is {IsKnownVersion: false})
        {
            return ($"Unknown version {request.Model.Model}", RequestValidationError.UnsupportedVersion);
        }

        if (request.Questions.Any(x => x is BoxQuestion) && request.Model.IsVersion(JevVersions.Version1_13_0))
        {
            return ($"Box questions are only supported from version {JevVersions.Version1_14_0}", RequestValidationError.BoxNotSupported);
        }

        return true;
    }
}

public enum RequestValidationError
{
    UnsupportedVersion,
    BoxNotSupported
}

internal union SimpleValidation(bool, (string, RequestValidationError))
{
    
}