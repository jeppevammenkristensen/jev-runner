using System.Text.Json.Nodes;

namespace JRK.JevRunner;

/// <summary>Contains the input and output token counts reported by the API for a request.</summary>
/// <param name="InputTokens">The number of input tokens reported by the API.</param>
/// <param name="OutputTokens">The number of output tokens reported by the API.</param>
public readonly record struct Usage ( long InputTokens, long OutputTokens )
{
    /// <summary>Parses token counts from the API response's usage object.</summary>
    public static Usage From(JsonObject asObject)
    {
        return new Usage(asObject["input_tokens"]!.GetValue<long>(), asObject["output_tokens"]!.GetValue<long>());
    }

    
    /// <summary>
    /// Returns an empty Usage
    /// </summary>
    public static Usage Zero => new(0, 0);

    /// <summary>
    /// Adds the usage to the existing
    /// </summary>
    /// <param name="usage">The usage to add</param>
    /// <returns>Generates the result of the two added Usages</returns>
    public Usage Add(Usage usage)
    {
        return new Usage(InputTokens + usage.InputTokens, OutputTokens + usage.OutputTokens);
    }

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
            result = result.Add(usage);
        }

        return result;
    }
}