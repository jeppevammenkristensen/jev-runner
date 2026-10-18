using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JRK.JevRunner.Infrastructure;
using Xunit;

namespace JRK.JevRunner.Tests.Infrastructure;

[TestSubject(typeof(ExceptionTranslator))]
public class ExceptionTranslatorTest
{
    /// <summary>JSON objects, arrays, and scalar values remain available without losing their structure.</summary>
    [Theory]
    [InlineData("""{"error":{"message":"Invalid image"}}""")]
    [InlineData("""{"detail":[{"loc":["body","state"],"msg":"Field required"}]}""")]
    [InlineData("""["error",42]""")]
    [InlineData("\"Invalid image\"")]
    [InlineData("42")]
    public async Task TranslateRequestException_JsonBody_PreservesJsonContent(string body)
    {
        var original = new HttpRequestException("HTTP failure", null, HttpStatusCode.BadRequest);
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent(body)};

        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        var known = Assert.IsType<JevKnownException>(translated);
        Assert.Same(original, known.InnerException);
        Assert.NotNull(known.Content);
        var node = Assert.IsAssignableFrom<JsonNode>(known.Content.Value.Value);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(body), node));
        Assert.Equal($"Bad request{Environment.NewLine}   {node.ToJsonString()}", known.Message);
    }

    /// <summary>Plain text, invalid JSON, HTML, and JSON null preserve the raw error body.</summary>
    [Theory]
    [InlineData("Service unavailable")]
    [InlineData("{invalid json")]
    [InlineData("<html>Bad gateway</html>")]
    [InlineData("null")]
    public async Task TranslateRequestException_TextBody_PreservesRawContent(string body)
    {
        var original = new HttpRequestException("HTTP failure", null, HttpStatusCode.BadRequest);
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent(body)};

        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        var known = Assert.IsType<JevKnownException>(translated);
        Assert.Same(original, known.InnerException);
        Assert.NotNull(known.Content);
        Assert.Equal(body, Assert.IsType<string>(known.Content.Value.Value));
        Assert.Equal($"Bad request{Environment.NewLine}   {body}", known.Message);
    }

    /// <summary>Empty and whitespace-only error bodies are represented by absent content.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(" \r\n\t")]
    public async Task TranslateRequestException_EmptyBody_HasNullContent(string body)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent(body)};

        var translated = await ExceptionTranslator.TranslateRequestException(
            new HttpRequestException("HTTP failure", null, response.StatusCode), response);

        var known = Assert.IsType<JevKnownException>(translated);
        Assert.Null(known.Content);
        Assert.Equal("Bad request", known.Message);
    }

    /// <summary>An unmapped status with a plain-text body still preserves the original HTTP failure.</summary>
    [Fact]
    public async Task TranslateRequestException_UnknownStatusWithText_ReturnsOriginalException()
    {
        var original = new HttpRequestException("HTTP failure", null, HttpStatusCode.BadGateway);
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("Bad gateway")
        };

        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        Assert.Same(original, translated);
    }

    /// <summary>
    /// Known API failures receive an actionable message and retain the original exception as their cause.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad request")]
    [InlineData(HttpStatusCode.Unauthorized,
        "The request was forbidden. Ensure that you called with a valid api token")]
    [InlineData(HttpStatusCode.RequestEntityTooLarge,
        "The request exceeds an API size limit. Reduce the request body or image sizes.")]
    [InlineData(HttpStatusCode.UnsupportedMediaType,
        "The request uses an unsupported Content-Encoding or more than one compression layer.")]
    [InlineData(HttpStatusCode.UnprocessableEntity,
        "The request body failed validation — for example a missing required field or a malformed question. The body details the offending field.")]
    [InlineData(HttpStatusCode.TooManyRequests, "You may have exceeded your rate limit")]
    [InlineData((HttpStatusCode) 509, "TypeSafe is temporarily overloaded. Retry after a short delay.")]
    [InlineData((HttpStatusCode) 529, "TypeSafe is temporarily overloaded. Retry after a short delay.")]
    public async Task TranslateRequestException_KnownStatusCode_WrapsOriginalException(
        HttpStatusCode statusCode, string expectedMessage)
    {
        var original = new HttpRequestException("Original HTTP failure", new InvalidOperationException("Root cause"),
            statusCode);

        using var response = new HttpResponseMessage(statusCode);
        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        var knownException = Assert.IsType<JevKnownException>(translated);
        Assert.Equal(expectedMessage, knownException.Message);
        Assert.Same(original, knownException.InnerException);
        Assert.Null(knownException.Content);
    }

    /// <summary>
    /// Unmapped status codes pass through unchanged, including forbidden responses and other server failures.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData((HttpStatusCode) 599)]
    public async Task TranslateRequestException_UnknownStatusCode_ReturnsOriginalException(HttpStatusCode statusCode)
    {
        var original = new HttpRequestException("Original HTTP failure", null, statusCode);

        using var response = new HttpResponseMessage(statusCode);
        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        Assert.Same(original, translated);
    }

    /// <summary>
    /// Non-HTTP failures pass through unchanged.
    /// </summary>
    [Fact]
    public async Task TranslateRequestException_NonHttpException_ReturnsOriginalException()
    {
        var original = new InvalidOperationException("Operation failed");

        using var response = new HttpResponseMessage();
        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        Assert.Same(original, translated);
    }

    /// <summary>
    /// Cancellation is preserved rather than converted into a known API failure.
    /// </summary>
    [Fact]
    public async Task TranslateRequestException_Cancellation_ReturnsOriginalException()
    {
        var original = new OperationCanceledException("Request cancelled");

        using var response = new HttpResponseMessage();
        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        Assert.Same(original, translated);
    }

    /// <summary>
    /// Only the supplied exception is matched; known HTTP failures nested inside other exceptions are not translated.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData((HttpStatusCode) 509)]
    public async Task TranslateRequestException_WrappedHttpException_ReturnsOriginalException(HttpStatusCode statusCode)
    {
        var httpException = new HttpRequestException("Original HTTP failure", null, statusCode);
        var original = new Exception("Outer failure", httpException);

        using var response = new HttpResponseMessage(statusCode);
        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        Assert.Same(original, translated);
        Assert.Same(httpException, translated.InnerException);
    }

    /// <summary>
    /// Transport failures without an HTTP status code pass through unchanged.
    /// </summary>
    [Fact]
    public async Task TranslateRequestException_MissingStatusCode_ReturnsOriginalException()
    {
        var original = new HttpRequestException("Connection failed", new IOException("Connection reset"));

        using var response = new HttpResponseMessage();
        var translated = await ExceptionTranslator.TranslateRequestException(original, response);

        Assert.Same(original, translated);
    }
}
