using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JRK.JevRunner;

/// <summary>Provides convenience initialization for runners using a shared HTTP client.</summary>
public static class JevRunnerExtensions 
{
    /// <summary>
    /// Reuses HTTP connections across runners created by the convenience initialization methods.
    /// </summary>
    private static readonly HttpClient SharedClient = new();
    
    extension(JevRunner)
    {
        /// <summary>
        /// Convenience method to create a runner
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static JevRunner Init(string apiKey)
        {
            return JevRunner.Init(apiKey, NullLoggerFactory.Instance);
        }

        /// <summary>
        /// Convenience method to create a runner
        /// </summary>
        /// <param name="apiKey">The API key sent as a bearer token with each request.</param>
        /// <param name="logFactory">An optional logFactory.</param>
        /// <returns>A new <see cref="JevRunner"/> instance</returns>
        /// <remarks>This is quick usage. If you want to use dependency injection consider using the public constructor</remarks>
        public static JevRunner Init(string apiKey, ILoggerFactory? logFactory)
        {
            var logger = (logFactory ?? new NullLoggerFactory()).CreateLogger<JevRunner>();

            return new JevRunner(new JevOptions(apiKey), SharedClient, logger);
        }

        /// <summary>
        /// Creates a runner using the supplied apikey and an optional logger
        /// </summary>
        /// <param name="apiKey">The API key sent as a bearer token with each request.</param>
        /// <param name="logger">The logger receiving request diagnostics, validation warnings, and failures.</param>
        /// <returns>A new <see cref="JevRunner"/> instance.</returns>
        public static JevRunner Init(string apiKey, ILogger<JevRunner>? logger)
        {
            return new JevRunner(new JevOptions(apiKey), SharedClient, logger ??  NullLogger<JevRunner>.Instance);
        }

        /// <summary>
        /// Initializes a JevRunner based on an environment variable. Default to look for an environment variable with the key JEV_TOKEN
        /// </summary>
        /// <param name="environmentVariableName"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public static JevRunner InitFromEnvironmentVariable(string environmentVariableName = "JEV_TOKEN", ILoggerFactory? loggerFactory = null)
        {
            return InitFromEnvironmentVariable(environmentVariableName, key => JevRunner.Init(key, loggerFactory));
        }

        /// <summary>
        /// Initializes a JevRunner based on an environment variable. Default to look for an environment variable with the key JEV_TOKEN
        /// </summary>
        /// <param name="environmentVariableName"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static JevRunner InitFromEnvironmentVariable(string environmentVariableName = "JEV_TOKEN", ILogger<JevRunner>? logger = null)
        {
            return InitFromEnvironmentVariable(environmentVariableName, key => JevRunner.Init(key, logger));
        }
        

        /// <summary>
        /// Reads an API key from the environment and passes it to the supplied runner initializer.
        /// </summary>
        /// <param name="environmentVariableName">The environment variable containing the API key.</param>
        /// <param name="initializer">The callback that creates a runner from the retrieved key.</param>
        /// <param name="getEnvironmentVariableDelegate">An optional environment lookup; defaults to the process environment.</param>
        /// <returns>The runner created by the initializer.</returns>
        /// <exception cref="InvalidOperationException">The environment lookup returns null.</exception>
        internal static JevRunner InitFromEnvironmentVariable(string environmentVariableName, JevRunnerInitializerDelegate initializer,
            GetEnvironmentVariableDelegate? getEnvironmentVariableDelegate = null)
        {
            getEnvironmentVariableDelegate ??= DefaultDelegate;
            var apiKey = getEnvironmentVariableDelegate(environmentVariableName) ??
                throw new InvalidOperationException(
                    $"An environment variable with the key {environmentVariableName} was not found");
            return initializer(apiKey);
        }
        
        

        
    }
    
    /// <summary>
    /// Gets the default lookup that reads environment variables from the current process.
    /// </summary>
    internal static GetEnvironmentVariableDelegate DefaultDelegate => Environment.GetEnvironmentVariable;
    
    
}