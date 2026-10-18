using System.Text;
using System.Text.Json.Nodes;

namespace JRK.JevRunner.Infrastructure;

/// <summary>Describes a recognized API failure and retains its HTTP exception and response content.</summary>
/// <param name="errorMessage">An actionable description of the HTTP failure.</param>
/// <param name="httpRequestException">The original HTTP exception, including its status code.</param>
/// <param name="content">The response body, or null for an empty or whitespace-only body.</param>
public class JevKnownException(string errorMessage, HttpRequestException httpRequestException, ErrorContent? content)
    : Exception(BuildMessage(errorMessage, content), httpRequestException)
{
    /// <summary>Retains the HTTP failure description and appends any returned error content.</summary>
    private static string BuildMessage(string message, ErrorContent? errorContent)
    {
        var builder = new StringBuilder(message);
        if (errorContent is string stringContent)
        {
            builder.AppendLine().Append("   ").Append(stringContent);
        }
        else if (errorContent is JsonNode node)
        {
            builder.AppendLine().Append("   ").Append(node.ToJsonString());
        }
        
        return builder.ToString();
    }

    /// <summary>Gets the parsed JSON or raw text error body, if one was returned.</summary>
    public ErrorContent? Content => content;
}
