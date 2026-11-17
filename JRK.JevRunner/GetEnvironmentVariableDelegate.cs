namespace JRK.JevRunner;

/// <summary>
/// Looks up an environment variable when initializing a runner.
/// </summary>
/// <param name="environmentVariableName">The name of the variable to retrieve.</param>
/// <returns>The variable's value, or null when it is not defined.</returns>
internal delegate string? GetEnvironmentVariableDelegate(string environmentVariableName);