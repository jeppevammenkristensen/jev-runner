namespace JRK.JevRunner.Infrastructure;

/// <summary>Describes a recognized API failure and retains its HTTP exception and response content.</summary>
/// <param name="errorMessage">An actionable description of the HTTP failure.</param>
/// <param name="httpRequestException">The original HTTP exception, including its status code.</param>
/// <param name="content">The response body, or null for an empty or whitespace-only body.</param>
public class JevKnownException(string errorMessage, HttpRequestException httpRequestException, ErrorContent? content)
    : Exception(errorMessage, httpRequestException)
{
    /// <summary>Gets the parsed JSON or raw text error body, if one was returned.</summary>
    public ErrorContent? Content => content;
}