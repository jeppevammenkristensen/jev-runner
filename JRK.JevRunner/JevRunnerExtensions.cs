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
        /// Creates a runner with a shared HTTP client and no-op logging.
        /// </summary>
        /// <param name="apiKey">The API key sent as a bearer token with each request.</param>
        /// <returns>A new runner.</returns>
        /// <exception cref="ArgumentException">The API key is null, empty, or whitespace.</exception>
        public static JevRunner Init(string apiKey)
        {
            return new JevRunner(new JevOptions(apiKey), SharedClient, NullLogger<JevRunner>.Instance);
        }

        /// <summary>
        /// Creates a runner with no-op logging using an API key from the process environment.
        /// </summary>
        /// <param name="environmentVariableName">The variable containing the API key. Defaults to <c>JEV_TOKEN</c>.</param>
        /// <returns>A new runner.</returns>
        /// <exception cref="InvalidOperationException">The environment variable is not set.</exception>
        /// <exception cref="ArgumentException">The retrieved API key is empty or whitespace.</exception>
        public static JevRunner InitFromEnvironmentVariable(string environmentVariableName = "JEV_TOKEN")
        {
            return InitFromEnvironmentVariable(environmentVariableName, static key => JevRunner.Init(key));
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