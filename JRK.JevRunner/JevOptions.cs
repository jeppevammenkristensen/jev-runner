namespace JRK.JevRunner;

/// <summary>Configures authentication for requests to the TypeSafe System One API.</summary>
/// <param name="ApiKey">The API key sent as a bearer token with each request.</param>
public record JevOptions(string ApiKey);
