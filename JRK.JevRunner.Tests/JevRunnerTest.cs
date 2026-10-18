using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using JRK.JevRunner.Responses;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
// ReSharper disable AccessToDisposedClosure

namespace JRK.JevRunner.Tests;

/// <summary>
/// Verifies request submission, representative returned answers, and HTTP failure handling without network access.
/// </summary>
[TestSubject(typeof(JevRunner))]
public class JevRunnerTest
{
    /// <summary>Box questions trigger experimental opt-in and their documented answers survive the HTTP pipeline.</summary>
    /// <remarks>The asset-free case verifies header selection only; the live API still requires an image.</remarks>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Execute_BoxQuestion_SendsOptInAndParsesBoxAnswer(bool includeAsset)
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "model": "jev-1.14.0",
                  "answers": {
                    "mug": {
                      "type": "box",
                      "box": {"top": 0.25, "bottom": 0.82, "left": 0.44, "right": 0.84},
                      "confidence": 0.92
                    },
                    "mug_visible": {"type": "noul", "noul": 0.95}
                  },
                  "usage": {"input_tokens": 290, "output_tokens": 21}
                }
                """, Encoding.UTF8, "application/json")
        };
        using var client = new HttpClient(new FakeHttpMessageHandler(async (message, cancellationToken) =>
        {
            Assert.Equal("true", Assert.Single(message.Headers.GetValues("TypeSafe-Danger-Zone-Images")));
            var body = JsonNode.Parse(await message.Content!.ReadAsStringAsync(cancellationToken))!;
            Assert.Equal("jev-1.14.0", body["model"]!.GetValue<string>());
            Assert.Equal("box", body["questions"]!["mug"]!["type"]!.GetValue<string>());
            Assert.Equal(includeAsset ? 1 : 0, body["assets"]!.AsObject().Count);
            return httpResponse;
        }));
        var request = new Request(new {photo = "image-ref"}, new JevModel("jev-1.14.0"))
            .AddQuestion(new BoxQuestion("mug", "the coffee mug in `photo`"))
            .AddQuestion(new NoulQuestion("mug_visible", "Is there a mug in `photo`?", null));
        if (includeAsset)
            request.Assets.Add(new ImageAsset("image-ref", "jpg", "AQID"));

        var response = await CreateRunner(client).Execute(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, response.Answers.Count);
        var box = Assert.IsType<BoxAnswer>(response.Answers[0].Answer.Value);
        Assert.Equal("mug", response.Answers[0].Name);
        Assert.Equal(0.25, box.Top);
        Assert.Equal(0.82, box.Bottom);
        Assert.Equal(0.44, box.Left);
        Assert.Equal(0.84, box.Right);
        Assert.Equal(0.92, box.Confidence.Value);
        Assert.Equal(0.95, response.GetRequiredNoulAnswer("mug_visible").Noul.Value);
        Assert.Equal(new Usage(290, 21), response.Usage);
    }

    /// <summary>The experimental header is request-scoped and only image requests upload asset data.</summary>
    [Fact]
    public async Task Execute_ImageThenTextRequest_SendsImageHeaderOnlyForImageRequest()
    {
        var calls = 0;
        using var client = new HttpClient(new FakeHttpMessageHandler(async (message, cancellationToken) =>
        {
            var body = JsonNode.Parse(await message.Content!.ReadAsStringAsync(cancellationToken))!;
            if (calls++ == 0)
            {
                Assert.Equal("true", Assert.Single(message.Headers.GetValues("TypeSafe-Danger-Zone-Images")));
                Assert.Equal("image-ref", body["state"]!["photo"]!.GetValue<string>());
                Assert.Equal("AQID", body["assets"]!["image-ref"]!["data_base64"]!.GetValue<string>());
            }
            else
            {
                Assert.False(message.Headers.Contains("TypeSafe-Danger-Zone-Images"));
                Assert.Empty(body["assets"]!.AsObject());
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"answers":{},"usage":{"input_tokens":1,"output_tokens":0}}""",
                    Encoding.UTF8, "application/json")
            };
        }));
        var runner = CreateRunner(client);
        var state = new AssetState().AddAsset("photo", new ImageAsset("image-ref", "jpg", "AQID"));

        await runner.Execute(new Request(state, null), TestContext.Current.CancellationToken);
        await runner.Execute(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
    }

    /// <summary>The HTTP pipeline preserves both structured and plain-text error bodies.</summary>
    [Theory]
    [InlineData("""{"error":"Invalid image"}""", true)]
    [InlineData("Invalid image", false)]
    public async Task Execute_ErrorBody_ThrowsKnownExceptionWithContent(string body, bool isJson)
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body)
        };
        using var client = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(httpResponse)));

        var error = await Assert.ThrowsAsync<JevKnownException>(() =>
            CreateRunner(client).Execute(CreateRequest(), TestContext.Current.CancellationToken));

        Assert.NotNull(error.Content);
        if (isJson)
        {
            var node = Assert.IsAssignableFrom<JsonNode>(error.Content.Value.Value);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(body), node));
        }
        else
        {
            Assert.Equal(body, Assert.IsType<string>(error.Content.Value.Value));
        }
        Assert.Equal(HttpStatusCode.BadRequest, Assert.IsType<HttpRequestException>(error.InnerException).StatusCode);
    }

    /// <summary>
    /// The injected client receives an authenticated JSON request and its successful response supplies named answers and usage.
    /// </summary>
    [Fact]
    public async Task Execute_Success_SubmitsRequestAndReturnsNamedAnswersAndUsage()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent("""
                                                 {
                                                   "answers": {
                                                     "approved": { "type": "noul", "noul": 0.75 },
                                                     "priority": {
                                                       "type": "choice",
                                                       "choice": "urgent",
                                                       "probabilities": { "urgent": 0.8, "normal": 0.2 },
                                                       "confidence": 0.9
                                                     }
                                                   },
                                                   "usage": { "input_tokens": 120, "output_tokens": 30 }
                                                 }
                                                 """, Encoding.UTF8, "application/json");
        var callCount = 0;
        using var client = new HttpClient(new FakeHttpMessageHandler(async (message, cancellationToken) =>
        {
            callCount++;
            Assert.Equal(HttpMethod.Post, message.Method);
            Assert.Equal(new Uri("https://api.typesafe.ai/v1/systemone"), message.RequestUri);
            Assert.Equal("Bearer", message.Headers.Authorization!.Scheme);
            Assert.Equal("test-api-key", message.Headers.Authorization.Parameter);
            Assert.NotNull(message.Content);
            Assert.Equal("application/json", message.Content.Headers.ContentType!.MediaType);
            var body = JsonNode.Parse(await message.Content.ReadAsStringAsync(cancellationToken))!;
            Assert.Equal("request state", body["state"]!.GetValue<string>());
            Assert.Equal("jev-latest", body["model"]!.GetValue<string>());
            Assert.Equal("noul", body["questions"]!["approved"]!["type"]!.GetValue<string>());
            Assert.Equal("choice", body["questions"]!["priority"]!["type"]!.GetValue<string>());
            return httpResponse;
        }));
        var runner = CreateRunner(client);
        var request = new Request("request state", JevModel.Latest)
        {
            Questions =
            [
                new NoulQuestion("approved", "Is this approved?", null),
                new ChoiceQuestion("priority", "Choose a priority",
                    OneOrMore<ChoiceCriteria>.FromEnumerable(
                        [new ChoiceCriteria("urgent", "Urgent"), new ChoiceCriteria("normal", "Normal")]))
            ]
        };

        var response = await runner.Execute(request, TestContext.Current.CancellationToken);

        Assert.Equal(1, callCount);
        Assert.Equal(2, response.Answers.Count);
        Assert.Equal(0.75, response.GetRequiredNoulAnswer("approved").Noul.Value);
        var priority = response.GetRequiredChoiceAnswer("priority");
        Assert.Equal("urgent", priority.Choice);
        Assert.Equal(0.9, priority.Confidence.Value);
        Assert.Equal(2, priority.Probabilities.Length);
        Assert.Equal(new Usage(120, 30), response.Usage);
    }

    /// <summary>
    /// Usage is parsed from snake-case fields as 64-bit counts, even when no answers are returned.
    /// </summary>
    [Theory]
    [InlineData(0L, 0L)]
    [InlineData(120L, 30L)]
    [InlineData(2147483648L, 4294967296L)]
    public async Task Execute_EmptyAnswers_DeserializesUsage(long inputTokens, long outputTokens)
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent($$"""
                                                 {
                                                   "answers": {},
                                                   "usage": {
                                                     "input_tokens": {{inputTokens}},
                                                     "output_tokens": {{outputTokens}}
                                                   }
                                                 }
                                                 """, Encoding.UTF8, "application/json");
        using var client = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(httpResponse)));
        var runner = CreateRunner(client);

        var response = await runner.Execute(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Empty(response.Answers);
        Assert.Equal(inputTokens, response.Usage.InputTokens);
        Assert.Equal(outputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Known HTTP error responses receive actionable messages and retain the HTTP failure as their cause.
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
    public async Task Execute_KnownErrorStatus_ThrowsKnownException(HttpStatusCode statusCode, string expectedMessage)
    {
        using var httpResponse = new HttpResponseMessage(statusCode);
        using var client = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(httpResponse)));
        var runner = CreateRunner(client);

        var exception = await Assert.ThrowsAsync<JevKnownException>(() => runner.Execute(CreateRequest(), TestContext.Current.CancellationToken));

        Assert.Equal(expectedMessage, exception.Message);
        var httpException = Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.Equal(statusCode, httpException.StatusCode);
    }

    /// <summary>
    /// Unmapped HTTP error responses remain HTTP exceptions with their original status code.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData((HttpStatusCode) 599)]
    public async Task Execute_UnknownErrorStatus_ThrowsHttpRequestException(HttpStatusCode statusCode)
    {
        using var httpResponse = new HttpResponseMessage(statusCode);
        using var client = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(httpResponse)));
        var runner = CreateRunner(client);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => runner.Execute(CreateRequest(), TestContext.Current.CancellationToken));

        Assert.Equal(statusCode, exception.StatusCode);
    }

    /// <summary>
    /// A transport failure before receiving a response passes through as the original exception.
    /// </summary>
    [Fact]
    public async Task Execute_TransportFailure_PreservesOriginalException()
    {
        var original = new HttpRequestException("Connection failed");
        using var client = new HttpClient(new FakeHttpMessageHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(original)));
        var runner = CreateRunner(client);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => runner.Execute(CreateRequest(),TestContext.Current.CancellationToken));

        Assert.Same(original, exception);
        Assert.Null(exception.StatusCode);
    }

    private static JevRunner CreateRunner(HttpClient client) =>
        new(new JevOptions("test-api-key"), client, NullLogger<JevRunner>.Instance);

    private static Request CreateRequest() => new("request state", JevModel.Latest);

    /// <summary>
    /// Supplies a per-test HTTP response or failure through the injected client's normal send pipeline.
    /// </summary>
    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        /// <summary>
        /// Delegates the outgoing request to the test's response callback.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => sendAsync(request, cancellationToken);
    }
}
