using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using JRK.JevRunner.Responses;
using Microsoft.Extensions.Logging;

namespace JRK.JevRunner;


/// <summary>
/// Submits requests to the TypeSafe System One API and parses their named answers.
/// </summary>
public partial class JevRunner : IJevRunner
{
    /// <summary>
    /// Keeps backtick references readable in standalone JSON payloads, which are not embedded in HTML.
    /// </summary>
    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Holds the API credentials used to authenticate outgoing requests.
    /// </summary>
    private readonly JevOptions _options;
    /// <summary>
    /// Sends API requests using the caller-owned HTTP client.
    /// </summary>
    private readonly HttpClient _client;
    /// <summary>
    /// Receives request diagnostics, validation warnings, and failures.
    /// </summary>
    private readonly ILogger<JevRunner> _logger;

    /// <summary>
    /// Submits requests to the TypeSafe System One API and parses their named answers.
    /// </summary>
    /// <param name="options">The API authentication settings.</param>
    /// <param name="client">The HTTP client used to send requests; ownership remains with the caller.</param>
    /// <param name="logger">The logger receiving request diagnostics, validation warnings, and failures.</param>
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
    /// <param name="cancellationToken">Cancels HTTP submission and response reading.</param>
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

        var json = JsonTranslator.CreateRequestJson(request).ToJsonString(RequestJsonOptions);
        LogRequestJsonRequestjson(json);
        
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.typesafe.ai/v1/systemone");
        message.Content = CreateRequestContent(request, json);

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
        
        if (_logger.IsEnabled(LogLevel.Debug))
            LogRequestHeaders(CreateSafeHeaderSnapshot(message));

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

    /// <summary>
    /// Uses multipart for stream-backed images and sends the same serialized JSON for either upload mode.
    /// </summary>
    /// <param name="request">The request whose stream-backed assets are uploaded alongside the JSON.</param>
    /// <param name="json">The serialized UTF-8 JSON payload, also used for request logging.</param>
    /// <remarks>JSON is the first part. Disposing the content also disposes the opened streams.</remarks>
    /// <returns>JSON content or multipart content owning the opened image streams.</returns>
    private HttpContent CreateRequestContent(Request request, string json)
    {
        // Check the assets to see if there are any IImageStreamAssets
        var images = request.Assets
            .Select(asset => asset.Value)
            .OfType<IImageStreamAsset>()
            .ToArray();
        
        if (images.Length == 0)
            return new StringContent(json, Encoding.UTF8, "application/json");

        var multipart = new MultipartFormDataContent();
        try
        {
            multipart.Add(new StringContent(json, Encoding.UTF8, "application/json"), "request");
            foreach (var image in images)
            {
                // Starter implementation: existing byte/Base64 assets remain inline in JSON.
                var content = new StreamContent(image.OpenRead());
                try
                {
                    LogAddingOctetStreamPartnameFilename(image.Name, image.FileName, image.Format);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    multipart.Add(content: content, name: image.Name, fileName: image.FileName);
                }
                catch
                {
                    content.Dispose();
                    throw;
                }
            }
            return multipart;
        }
        catch
        {
            multipart.Dispose();
            throw;
        }
    }

    /// <summary>Snapshots outgoing headers, applying request overrides and redacting credentials before logging.</summary>
    /// <remarks>Includes client defaults and content headers, but not headers added later by HTTP handlers or the transport.</remarks>
    private JsonObject CreateSafeHeaderSnapshot(HttpRequestMessage message)
    {
        var headers = new JsonObject(new JsonNodeOptions { PropertyNameCaseInsensitive = true });
        AddHeaders(_client.DefaultRequestHeaders);
        AddHeaders(message.Headers);
        if (message.Content != null)
            AddHeaders(message.Content.Headers);
        return headers;

        void AddHeaders(HttpHeaders source)
        {
            foreach (var header in source)
            {
                var sensitive = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                                header.Key.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
                                header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
                                header.Key.Equals("JevToken", StringComparison.OrdinalIgnoreCase) ||
                                header.Key.Equals("X-Api-Key", StringComparison.OrdinalIgnoreCase);
                headers[header.Key] = sensitive
                    ? "[REDACTED]"
                    : string.Join(", ", header.Value).Replace(_options.ApiKey, "[REDACTED]", StringComparison.Ordinal);
            }
        }
    }

    /// <summary>Checks model compatibility, throwing in strict mode or logging a warning otherwise.</summary>
    /// <param name="request">The request and validation policy to inspect.</param>
    /// <returns>The detected validation error, or an empty result when validation succeeds.</returns>
    /// <exception cref="InvalidOperationException">Strict validation is enabled and a compatibility check fails.</exception>
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

            case bool:
                return NoneValue.New;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Logs the serialized outgoing request at debug level with literal backtick references.
    /// </summary>
    /// <param name="requestJson">The JSON payload about to be sent.</param>
    [LoggerMessage(LogLevel.Debug, "Request json {RequestJson}")]
    partial void LogRequestJsonRequestjson(string requestJson);

    /// <summary>Logs only the sanitized snapshot of outgoing request headers at debug level.</summary>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Request headers {Headers}")]
    partial void LogRequestHeaders(JsonObject headers);

    /// <summary>Logs a compatibility warning when the request is allowed to proceed.</summary>
    /// <param name="error">The validation failure description.</param>
    /// <param name="validationType">The compatibility check that failed.</param>
    [LoggerMessage(LogLevel.Warning, "Validation failed. {Error} {ValidationType}")]
    partial void LogValidationFailedErrorValidationtype(string error, RequestValidationError validationType);

    /// <summary>Logs an unsuccessful HTTP response without a known local validation error.</summary>
    /// <param name="exception">The exception raised by HTTP status validation.</param>
    [LoggerMessage(LogLevel.Error, "Request failed")]
    partial void LogRequestFailed(Exception exception);

    /// <summary>Logs an unsuccessful HTTP response alongside a previously detected compatibility error.</summary>
    /// <param name="ex">The exception raised by HTTP status validation.</param>
    /// <param name="type">The compatibility error detected before submission.</param>
    [LoggerMessage(LogLevel.Error, "Known validation error {Type}")]
    partial void LogKnownValidationErrorType(Exception ex, RequestValidationError type);

    /// <summary>
    /// Logs the metadata of an image being added as a multipart octet-stream part at debug level.
    /// </summary>
    /// <param name="name">The multipart field name identifying the uploaded image.</param>
    /// <param name="fileName">The file name reported in the multipart part's headers.</param>
    /// <param name="format">The image format reported in the request metadata.</param>
    [LoggerMessage(LogLevel.Debug, "Adding octet-stream {Name} {FileName} {Format}")]
    partial void LogAddingOctetStreamPartnameFilename(string name, string fileName, string format);
}
