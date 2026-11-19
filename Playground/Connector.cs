using AutoSpectre;
using JRK.JevRunner;

namespace Playground;

[AutoSpectreForm]
public partial class Connector
{
    public Connector()
    {
        DefaultApiKey = Environment.GetEnvironmentVariable("JEV_TOKEN");
    }
    
    public string? DefaultApiKey { get; set; }
    
    [TextPrompt(DefaultValueSource = nameof(DefaultApiKey))] public partial string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Creates a runner that authenticates with the configured API key and logs to the console.
    /// </summary>
    public IJevRunner GetRunner() => JevRunner.Init(ApiKey).WithLogger(new ConsoleLogger<JevRunner>());
}
