using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JRK.JevRunner;

/// <summary>Provides convenience initialization for runners using a shared HTTP client.</summary>
public static class JevRunnerExtensions 
{
    private static readonly HttpClient SharedClient = new();
    
    extension(JevRunner)
    {
        /// <summary>
        /// Convenience method to create a runner
        /// </summary>
        /// <param name="apiKey">The apiKey</param>
        /// <param name="logger"></param>
        /// <returns>A new <see cref="JevRunner"/> instance</returns>
        /// <remarks>This is quick usage. If you want to use dependency injection consider using the public constructor</remarks>
        public static JevRunner Init(string apiKey, ILogger<JevRunner>? logger = null)
        {
            return new JevRunner(new JevOptions(apiKey), SharedClient, logger ?? NullLogger<JevRunner>.Instance);
        }
    }
}