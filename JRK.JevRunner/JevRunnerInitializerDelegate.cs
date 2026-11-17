namespace JRK.JevRunner;

/// <summary>
/// Creates a runner using an API key obtained from the environment.
/// </summary>
/// <param name="apiKey">The API key to use for authentication.</param>
/// <returns>The initialized runner.</returns>
internal delegate JevRunner JevRunnerInitializerDelegate(string apiKey);