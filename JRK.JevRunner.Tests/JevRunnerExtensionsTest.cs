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
    /// The optional logger can be supplied without changing the factory result.
    /// </summary>
    [Fact]
    public void Init_WithLoggerFactory_ReturnsRunner()
    {
        var runner = JevRunner.Init("test-api-key", NullLoggerFactory.Instance);

        Assert.IsType<JevRunner>(runner);
    }

    /// <summary>Typed loggers and null loggers are both accepted.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Init_WithTypedLogger_ReturnsRunner(bool useNullLogger)
    {
        ILogger<JevRunner>? logger = useNullLogger ? null : NullLogger<JevRunner>.Instance;

        var runner = JevRunner.Init("test-api-key", logger);

        Assert.IsType<JevRunner>(runner);
    }

    /// <summary>A null logger factory falls back to no-op logging.</summary>
    [Fact]
    public void Init_WithNullLoggerFactory_ReturnsRunner()
    {
        var runner = JevRunner.Init("test-api-key", (ILoggerFactory?) null);

        Assert.IsType<JevRunner>(runner);
    }

    /// <summary>The supplied factory creates a logger with the runner's category.</summary>
    [Fact]
    public void Init_WithLoggerFactory_CreatesRunnerLogger()
    {
        using var factory = new RecordingLoggerFactory();

        var runner = JevRunner.Init("test-api-key", factory);

        Assert.IsType<JevRunner>(runner);
        Assert.Equal(typeof(JevRunner).FullName, factory.CategoryName);
    }

    /// <summary>Both logger overloads preserve API-key validation.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Init_WithLoggerAndInvalidApiKey_ThrowsArgumentException(string? apiKey)
    {
        var factoryException =
            Assert.Throws<ArgumentException>(() => JevRunner.Init(apiKey!, NullLoggerFactory.Instance));
        var loggerException =
            Assert.Throws<ArgumentException>(() => JevRunner.Init(apiKey!, NullLogger<JevRunner>.Instance));

        Assert.Equal("API key cannot be null or empty", factoryException.Message);
        Assert.Equal("API key cannot be null or empty", loggerException.Message);
    }

    /// <summary>Both public environment overloads read a custom variable and accept optional logging.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitFromEnvironmentVariable_WithCustomVariable_ReturnsRunner(bool useNullLogger)
    {
        var variableName = $"JEV_RUNNER_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(variableName, "test-api-key");
        try
        {
            ILoggerFactory? factory = useNullLogger ? null : NullLoggerFactory.Instance;
            ILogger<JevRunner>? logger = useNullLogger ? null : NullLogger<JevRunner>.Instance;

            Assert.IsType<JevRunner>(JevRunner.InitFromEnvironmentVariable(variableName, factory));
            Assert.IsType<JevRunner>(JevRunner.InitFromEnvironmentVariable(variableName, logger));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    /// <summary>Both public overloads report the name of a missing environment variable.</summary>
    [Fact]
    public void InitFromEnvironmentVariable_WithMissingVariable_ThrowsInvalidOperationException()
    {
        var variableName = $"JEV_RUNNER_TEST_{Guid.NewGuid():N}";

        var factoryException = Assert.Throws<InvalidOperationException>(() =>
            JevRunner.InitFromEnvironmentVariable(variableName, (ILoggerFactory?) null));
        var loggerException = Assert.Throws<InvalidOperationException>(() =>
            JevRunner.InitFromEnvironmentVariable(variableName, (ILogger<JevRunner>?) null));

        var expectedMessage = $"An environment variable with the key {variableName} was not found";
        Assert.Equal(expectedMessage, factoryException.Message);
        Assert.Equal(expectedMessage, loggerException.Message);
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

    /// <summary>Records the category requested during runner initialization without creating logging resources.</summary>
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