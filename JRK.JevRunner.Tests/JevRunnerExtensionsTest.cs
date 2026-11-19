using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JRK.JevRunner.Tests;

[TestSubject(typeof(JevRunnerExtensions))]
public class JevRunnerExtensionsTest
{
    /// <summary>
    /// A non-empty API key creates a runner through the convenience factory.
    /// </summary>
    [Fact]
    public void Init_WithApiKey_ReturnsRunner()
    {
        var runner = JevRunner.Init("test-api-key");

        Assert.NotNull(runner);
        Assert.IsType<JevRunner>(runner);
    }

    /// <summary>
    /// Configuring a typed logger returns the same runner for chaining.
    /// </summary>
    [Fact]
    public void WithLogger_ReturnsSameRunner()
    {
        var runner = JevRunner.Init("test-api-key");

        Assert.Same(runner, runner.WithLogger(NullLogger<JevRunner>.Instance));
    }

    /// <summary>The supplied factory creates a logger with the runner's category.</summary>
    [Fact]
    public void WithLoggerFactory_CreatesRunnerLoggerAndReturnsSameRunner()
    {
        using var factory = new RecordingLoggerFactory();
        var runner = JevRunner.Init("test-api-key");

        Assert.Same(runner, runner.WithLoggerFactory(factory));
        Assert.Equal(typeof(JevRunner).FullName, factory.CategoryName);
    }

    /// <summary>Environment initialization reads the API key from a custom variable.</summary>
    [Fact]
    public void InitFromEnvironmentVariable_WithCustomVariable_ReturnsRunner()
    {
        var variableName = $"JEV_RUNNER_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(variableName, "test-api-key");
        try
        {
            Assert.IsType<JevRunner>(JevRunner.InitFromEnvironmentVariable(variableName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    /// <summary>Environment initialization reports the name of a missing environment variable.</summary>
    [Fact]
    public void InitFromEnvironmentVariable_WithMissingVariable_ThrowsInvalidOperationException()
    {
        var variableName = $"JEV_RUNNER_TEST_{Guid.NewGuid():N}";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JevRunner.InitFromEnvironmentVariable(variableName));

        Assert.Equal($"An environment variable with the key {variableName} was not found", exception.Message);
    }

    /// <summary>The environment helper forwards the requested variable's value and returns the initializer result.</summary>
    [Fact]
    public void InitFromEnvironmentVariable_ForwardsApiKeyToInitializer()
    {
        var expectedRunner = JevRunner.Init("test-api-key");
        string? requestedVariable = null;
        string? receivedApiKey = null;

        var runner = JevRunner.InitFromEnvironmentVariable(
            "CUSTOM_JEV_TOKEN",
            key =>
            {
                receivedApiKey = key;
                return expectedRunner;
            },
            name =>
            {
                requestedVariable = name;
                return "environment-api-key";
            });

        Assert.Equal("CUSTOM_JEV_TOKEN", requestedVariable);
        Assert.Equal("environment-api-key", receivedApiKey);
        Assert.Same(expectedRunner, runner);
    }

    /// <summary>A missing environment value must not invoke the initializer.</summary>
    [Fact]
    public void InitFromEnvironmentVariable_WithMissingValue_DoesNotInvokeInitializer()
    {
        var initializerCalled = false;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JevRunner.InitFromEnvironmentVariable(
                "CUSTOM_JEV_TOKEN",
                _ =>
                {
                    initializerCalled = true;
                    return JevRunner.Init("test-api-key");
                },
                _ => null));

        Assert.False(initializerCalled);
        Assert.Equal("An environment variable with the key CUSTOM_JEV_TOKEN was not found", exception.Message);
    }

    /// <summary>
    /// Missing or whitespace-only API keys are rejected by the created runner.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Init_WithInvalidApiKey_ThrowsArgumentException(string? apiKey)
    {
        var exception = Assert.Throws<ArgumentException>(() => JevRunner.Init(apiKey!));

        Assert.Equal("API key cannot be null or empty", exception.Message);
    }

    /// <summary>Records the category requested when configuring runner logging.</summary>
    private sealed class RecordingLoggerFactory : ILoggerFactory
    {
        public string? CategoryName { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            CategoryName = categoryName;
            return NullLogger.Instance;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}