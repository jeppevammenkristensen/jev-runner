using System.Text.Json.Nodes;

namespace JRK.JevRunner;

/// <summary>Contains the input and output token counts reported by the API for a request.</summary>
/// <param name="InputTokens">The number of input tokens reported by the API.</param>
/// <param name="OutputTokens">The number of output tokens reported by the API.</param>
public record Usage ( long InputTokens, long OutputTokens )
{
    /// <summary>Parses token counts from the API response's usage object.</summary>
    public static Usage From(JsonObject asObject)
    {
        return new Usage(asObject["input_tokens"]!.GetValue<long>(), asObject["output_tokens"]!.GetValue<long>());
    }
}