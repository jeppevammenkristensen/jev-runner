using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace JRK.JevRunner.Infrastructure;

internal class ThrowHelper
{
    public static void ThrowIfEmpty<T>(IReadOnlyCollection<T> argument,
        [CallerArgumentExpression(nameof(argument))] string? argumentName = null)
    {
        if (argument.Count == 0)
        {
            throw new ArgumentException("The collection cannot be empty", argumentName);
        }
    }
    
}

internal class ExceptionTranslator
{
    internal static async Task<Exception> TranslateRequestException(Exception ex, HttpResponseMessage response)
    {
        var result = await response.Content.ReadAsStringAsync();

        ErrorContent? content = null;
        
        if (!string.IsNullOrWhiteSpace(result))
        {
            var jsonNode = JsonNode.Parse(result);
            if (jsonNode is { })
            {
                content = jsonNode;
            }
            else
            {
                content = result;
            }
        }

        return ex switch
        {
            HttpRequestException {StatusCode: HttpStatusCode.BadRequest} bad => new JevKnownException("Bad request", bad, content),
            HttpRequestException {StatusCode: HttpStatusCode.Unauthorized} unauthorized => new JevKnownException(
                "The request was forbidden. Ensure that you called with a valid api token", unauthorized, content),
            HttpRequestException {StatusCode: HttpStatusCode.RequestEntityTooLarge} tooLarge => new JevKnownException("The request exceeds an API size limit. Reduce the request body or image sizes.", tooLarge, content),
            HttpRequestException {StatusCode: HttpStatusCode.UnsupportedMediaType} unsupportedMediaType => new JevKnownException("The request uses an unsupported Content-Encoding or more than one compression layer.", unsupportedMediaType, content),
            HttpRequestException {StatusCode: HttpStatusCode.UnprocessableEntity} unprocessable => new JevKnownException("The request body failed validation — for example a missing required field or a malformed question. The body details the offending field.",unprocessable, content),
            HttpRequestException {StatusCode: HttpStatusCode.TooManyRequests} toManyRequests => new JevKnownException("You may have exceeded your rate limit",toManyRequests, content),
            HttpRequestException {StatusCode: (HttpStatusCode)509} overloaded => new JevKnownException("TypeSafe is temporarily overloaded. Retry after a short delay.", overloaded, content),
            HttpRequestException {StatusCode: (HttpStatusCode)529} overloaded => new JevKnownException("TypeSafe is temporarily overloaded. Retry after a short delay.", overloaded, content),
            _ => ex
        };
    }
}

public union ErrorContent(JsonNode,string);

public class JevKnownException(string errorMessage, HttpRequestException httpRequestException, ErrorContent? content)
    : Exception(errorMessage, httpRequestException)
{
    public ErrorContent? Content => content;
}
