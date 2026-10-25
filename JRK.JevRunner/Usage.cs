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

    
    /// <summary>
    /// Returns an empty Usage
    /// </summary>
    public static Usage Zero => new Usage(0, 0);

    /// <summary>
    /// Calculate the cost for this usage
    /// </summary>
    /// <param name="price"></param>
    /// <returns></returns>
    public decimal CalculateCost(JevPrice price)
    {
        return (InputTokens * price.PricePerMillionInputTokens +  OutputTokens * price.PricePerMillionOutputTokens) / 1000000m;
    }
}

public static class UsageExtensions
{
    /// <summary>
    /// Creates the sum for all usages
    /// </summary>
    /// <param name="usages"></param>
    /// <returns></returns>
    public static Usage SumUsage(this IEnumerable<Usage> usages)
    {
        var result = new Usage(0,0);

        foreach (var usage in usages)
        {
            result = new Usage(usage.InputTokens + result.InputTokens, usage.OutputTokens + result.OutputTokens);
        }

        return result;
    }
}