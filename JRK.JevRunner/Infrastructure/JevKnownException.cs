using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JRK.JevRunner.Infrastructure;

/// <summary>Describes a recognized API failure and retains its HTTP exception and response content.</summary>
/// <param name="errorMessage">An actionable description of the HTTP failure.</param>
/// <param name="httpRequestException">The original HTTP exception, including its status code.</param>
/// <param name="content">The response body, or null for an empty or whitespace-only body.</param>
public class JevKnownException(string errorMessage, HttpRequestException httpRequestException, ErrorContent? content)
    : Exception(BuildMessage(errorMessage, content), httpRequestException)
{
    private static string? BuildMessage(string message, ErrorContent? errorContent)
    {
        var builder = new StringBuilder().AppendLine();
        if (errorContent is string stringContent)
        {
            builder.AppendLine($"   {stringContent}");
        }
        else if (errorContent is JsonNode node)
        {
            builder.AppendLine($"   {node.ToJsonString(new JsonSerializerOptions() { })}");
        }
        
        return builder.ToString();
    }

    /// <summary>Gets the parsed JSON or raw text error body, if one was returned.</summary>
    public ErrorContent? Content => content;
}