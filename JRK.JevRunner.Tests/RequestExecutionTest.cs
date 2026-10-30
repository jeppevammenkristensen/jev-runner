using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies 0.7 request validation, model selection, logging, and request-scoped feature headers without network access.</summary>
public class RequestExecutionTest
{
    /// <summary>The shared matcher recognizes danger-zone prefixes without matching headers containing them elsewhere.</summary>
    [Theory]
    [InlineData(DangerZones.Images, true)]
    [InlineData(DangerZones.LongContext, true)]
    [InlineData("TypeSafe-Danger-Zone-Future", true)]
    [InlineData("X-TypeSafe-Danger-Zone-Images", false)]
    [InlineData("Authorization", false)]
    [InlineData("", false)]
    public void DangerZoneHeaderRegex_MatchesOnlyPrefixedHeaders(string header, bool expected)
    {
        Assert.Equal(expected, DangerZones.DangerZoneHeaderRegex().IsMatch(header));
    }

    /// <summary>Image and long-context headers are independent, appear once, and never leak to the next request.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Execute_FeatureFlags_SendIndependentRequestScopedHeaders(bool images, bool longContext)
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler(message =>
        {
            var first = calls++ == 0;
            AssertHeader(message, DangerZones.Images, first && images);
            AssertHeader(message, DangerZones.LongContext, first && longContext);
            return Success();
        }));
        var runner = CreateRunner(client, new RecordingLogger());
        var request = new Request("state") {LongContextEnabled = longContext};
        if (images)
            request.AddQuestion(new BoxQuestion("box", "Find the object"));

        await runner.Execute(request, TestContext.Current.CancellationToken);
        await runner.Execute(new Request("state"), TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
        Assert.False(client.DefaultRequestHeaders.Contains(DangerZones.Images));
        Assert.False(client.DefaultRequestHeaders.Contains(DangerZones.LongContext));
    }

    /// <summary>Strict validation rejects unsupported combinations before any HTTP request is sent.</summary>
    [Theory]
    [InlineData("jev-future", false, "Unknown version jev-future")]
    [InlineData("jev-future", true, "Unknown version jev-future")]
    [InlineData(JevVersions.Version1_13_0, true, "Box questions are only supported from version jev-1.14.0")]
    public async Task Execute_StrictInvalidRequest_ThrowsBeforeSending(string model, bool box, string expectedMessage)
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler(_ =>
        {
            calls++;
            return Success();
        }));
        var logger = new RecordingLogger();
        var request = CreateRequest(model, box);
        request.StrictValidation = true;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateRunner(client, logger).Execute(request, TestContext.Current.CancellationToken));

        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(0, calls);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    /// <summary>Default validation warns but still sends the requested model; HTTP failures retain validation context.</summary>
    [Theory]
    [InlineData("jev-future", false, RequestValidationError.UnsupportedVersion, false)]
    [InlineData("jev-future", true, RequestValidationError.UnsupportedVersion, true)]
    [InlineData(JevVersions.Version1_13_0, true, RequestValidationError.BoxNotSupported, false)]
    [InlineData(JevVersions.Version1_13_0, true, RequestValidationError.BoxNotSupported, true)]
    public async Task Execute_NonStrictInvalidRequest_WarnsAndStillSends(string model, bool box,
        RequestValidationError validationError, bool fail)
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler(async message =>
        {
            calls++;
            var body = JsonNode.Parse(await message.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
            Assert.Equal(model, body["model"]!.GetValue<string>());
            return fail ? new HttpResponseMessage(HttpStatusCode.BadRequest) : Success();
        }));
        var logger = new RecordingLogger();
        var runner = CreateRunner(client, logger);
        var request = CreateRequest(model, box);

        if (fail)
            await Assert.ThrowsAsync<JevKnownException>(() =>
                runner.Execute(request, TestContext.Current.CancellationToken));
        else
            Assert.Empty((await runner.Execute(request, TestContext.Current.CancellationToken)).Answers);

        Assert.Equal(1, calls);
        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Equal(validationError, warning.Properties["ValidationType"]);
        if (fail)
        {
            var error = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Error);
            Assert.Equal(validationError, error.Properties["Type"]);
            Assert.IsType<HttpRequestException>(error.Exception);
        }
        else
            Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    /// <summary>Every supported model accepts ordinary questions; only 1.13.0 rejects Box questions in strict mode.</summary>
    [Theory]
    [InlineData(JevVersions.Latest, false)]
    [InlineData(JevVersions.Latest, true)]
    [InlineData(JevVersions.Preview, false)]
    [InlineData(JevVersions.Preview, true)]
    [InlineData(JevVersions.Version1_13_0, false)]
    [InlineData(JevVersions.Version1_14_0, false)]
    [InlineData(JevVersions.Version1_14_0, true)]
    [InlineData(null, false)]
    [InlineData(null, true)]
    public async Task Execute_StrictValidRequest_PreservesExplicitModelOrUsesLatest(string? model, bool box)
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler(async message =>
        {
            calls++;
            var body = JsonNode.Parse(await message.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
            Assert.Equal(model ?? JevVersions.Latest, body["model"]!.GetValue<string>());
            return Success();
        }));
        var logger = new RecordingLogger();
        var request = CreateRequest(model, box);
        request.StrictValidation = true;

        await CreateRunner(client, logger).Execute(request, TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        Assert.Equal(model ?? JevVersions.Latest, request.Model.Model);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    /// <summary>HTTP failures without a validation problem use the generic failure log and retain the original exception.</summary>
    [Fact]
    public async Task Execute_ValidRequestHttpFailure_LogsGenericFailure()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var logger = new RecordingLogger();

        var error = await Assert.ThrowsAsync<JevKnownException>(() =>
            CreateRunner(client, logger).Execute(new Request("state"), TestContext.Current.CancellationToken));

        var log = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Error);
        Assert.Equal("Request failed", log.Message);
        Assert.Same(error.InnerException, log.Exception);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    private static JevRunner CreateRunner(HttpClient client, RecordingLogger logger) =>
        new(new JevOptions("test-api-key"), client, logger);

    private static Request CreateRequest(string? model, bool box)
    {
        var request = new Request("state", model is null ? null : new JevModel(model));
        if (box)
            request.AddQuestion(new BoxQuestion("box", "Find the object"));
        else
            request.AddQuestion(new NoulQuestion("visible", "Is it visible?", null));
        return request;
    }

    private static HttpResponseMessage Success() => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            """{"answers":{},"usage":{"input_tokens":1,"output_tokens":0}}""", Encoding.UTF8, "application/json")
    };

    /// <summary>Checks both header presence and the exact single opt-in value.</summary>
    private static void AssertHeader(HttpRequestMessage message, string header, bool enabled)
    {
        Assert.Equal(enabled, message.Headers.Contains(header));
        if (enabled)
            Assert.Equal("true", Assert.Single(message.Headers.GetValues(header)));
    }

    /// <summary>Supplies in-memory HTTP responses without calling the live API.</summary>
    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
            : this(message => Task.FromResult(send(message)))
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            send(request);
    }

    /// <summary>Captures structured logging so validation categories and exception identity can be asserted.</summary>
    private sealed class RecordingLogger : ILogger<JevRunner>
    {
        public List<LogEntry> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception,
                ((IEnumerable<KeyValuePair<string, object?>>) (object) state!).ToDictionary(pair => pair.Key,
                    pair => pair.Value)));
    }

    /// <summary>Stores the observable fields of a structured log entry.</summary>
    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        Exception? Exception,
        Dictionary<string, object?> Properties);
}