using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Responses;
using Microsoft.Extensions.Logging;

namespace JRK.JevRunner;


/// <summary>
/// DangerZones are attributes you can include in the request for
/// toggling areas like Images or LongContext
/// </summary>
public partial class DangerZones
{
    /// <summary>
    /// Header to use when you want to include Images (currently Dreamerready)
    /// </summary>
    public const string Images = "TypeSafe-Danger-Zone-Images";
    /// <summary>
    /// Header to use to "disable" the max context size
    /// </summary>
    /// <remarks>TypeSafe states for longer context above the default 64 kb that "It is slower and less accurate than staying within the default limits, so turn it on only for requests that need it."</remarks>
    public const string LongContext = "TypeSafe-Danger-Zone-Long-Context";

    [GeneratedRegex("^TypeSafe-Danger-Zone")]
    public static partial Regex DangerZoneHeaderRegex();
}


/// <summary>
/// Submits requests to the TypeSafe System One API and parses their named answers.
/// </summary>
public partial class JevRunner : IJevRunner
{
    private readonly JevOptions _options;
    private readonly HttpClient _client;
    private readonly ILogger<JevRunner> _logger;

    /// <summary>
    /// Submits requests to the TypeSafe System One API and parses their named answers.
    /// </summary>
    public JevRunner(JevOptions options, HttpClient client, ILogger<JevRunner> logger)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("API key cannot be null or empty");
        }
        
        _options = options;
        _client = client;
        _logger = logger;
    }
    
    /// <summary>
    /// Serializes and submits the request, then parses the API's named answers into a response.
    /// </summary>
    /// <param name="request">The state, model, and questions to send to the API.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A response containing the supported answers returned by the API.</returns>
    /// <exception cref="InvalidOperationException">The API returns an unsuccessful HTTP status code.</exception>
    /// <exception cref="JsonException">The response cannot be read as a JSON object or contains a null JSON response.</exception>
    public async Task<Response> Execute(Request request, CancellationToken cancellationToken = default)
    {
        // This logic is here. Because between the creation of the request and adding questions
        // this can affect the best choice for JevModel so it's reevaluated if it hasn't been initalized
        // with a not null value
        if (request.PassedModelWasNull)
        {
            request.Model = request.AutoDetectJevModel();
        }

        var validateRequest = ValidateRequest(request);

        var json = JsonTranslator.CreateRequestJson(request);
        LogRequestJsonRequestjson(json);
        
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.typesafe.ai/v1/systemone");
        message.Content = JsonContent.Create(json);

        message.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", _options.ApiKey);

        // I have a theory that in later versions of TypeSafe. This check will not be required.
        if (request.HasImages)
        {
            _  = message.AddImageDangerZone();
        }

        if (request.LongContextEnabled)
        {
            _ = message.AddLongContext();
        }
        
        var response = await _client.SendAsync(message, cancellationToken: cancellationToken);

        try
        {
            _ = response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            validateRequest.If(x => LogKnownValidationErrorType(e, x), () => LogRequestFailed(e));
            throw (await ExceptionTranslator.TranslateRequestException(e, response));
        }

        try
        {
            var responseJson = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken) ??
                               throw new JsonException("The API returned a null JSON response");

            
            
            var answersObject = responseJson["answers"]!.AsObject();
            return new Response([.. Answer.From(answersObject)], Usage.From(responseJson["usage"]!.AsObject()));
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("An unexpected error occurred when deserializing the response content", e);
        }
    }

    private Maybe<RequestValidationError> ValidateRequest(Request request)
    {
        var simpleValidation = RequestValidator.Validate(request);

        switch (simpleValidation)
        {
            case ValueTuple<string, RequestValidationError> validationError:
                if (request.StrictValidation)
                {
                    throw new InvalidOperationException(validationError.Item1);
                }
                LogValidationFailedErrorValidationtype(validationError.Item1, validationError.Item2);
                return validationError.Item2;

            case bool b:
                return NoneValue.New;
            default:
                throw new ArgumentOutOfRangeException(nameof(simpleValidation));
        }
    }

    [LoggerMessage(LogLevel.Debug, "Request json {RequestJson}")]
    partial void LogRequestJsonRequestjson(JsonObject requestJson);

    [LoggerMessage(LogLevel.Warning, "Validation failed. {Error} {ValidationType}")]
    partial void LogValidationFailedErrorValidationtype(string error, RequestValidationError validationType);

    [LoggerMessage(LogLevel.Error, "Request failed")]
    partial void LogRequestFailed(Exception exception);

    [LoggerMessage(LogLevel.Error, "Known validation error {Type}")]
    partial void LogKnownValidationErrorType(Exception ex, RequestValidationError type);
}

internal static class HttpRequestMessageExtensions
{
    extension(HttpRequestMessage message)
    {
        /// <summary>
        /// Toggles Images DangerZone (by adding <see cref="DangerZones.Images"/>)
        /// </summary>
        /// <returns></returns>
        internal HttpRequestMessage AddImageDangerZone()
        {
            message.Headers.Add(DangerZones.Images, "true");
            return message;
        }

        /// <summary>
        /// Toggles LongContext DangerZone (by adding <see cref="DangerZones.LongContext"/>)
        /// </summary>
        /// <returns></returns>
        internal HttpRequestMessage AddLongContext()
        {
            message.Headers.Add(DangerZones.LongContext, "true");
            return message;
        }

        internal bool HasDangerRequestMessage()
        {
            return message.Headers.Any(x => DangerZones.DangerZoneHeaderRegex().IsMatch(x.Key ?? string.Empty));
        }
    }
    
}

internal union Maybe<T>(T,NoneValue)
{
    internal Maybe<T> Some (T value)
    {
        return new Maybe<T>(value);
    }

    internal Maybe<T> None => NoneValue.New;

    public Maybe<T> If(Action<T> some, Action? none = null)
    {
        switch (Value)
        {
            case NoneValue noneValue:
                none?.Invoke();
                break;
            case T unknown:
                some.Invoke(unknown);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return this;
    }
}

internal class NoneValue
{
    private NoneValue()
    {
        
    }
    
    public static NoneValue New => new NoneValue();
}
