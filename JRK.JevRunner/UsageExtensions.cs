namespace JRK.JevRunner;

/// <summary>Aggregates token usage across multiple responses.</summary>
public static class UsageExtensions
{
    /// <summary>Creates the sum of all supplied token counts.</summary>
    /// <param name="usages">The sequence of token counts to total.</param>
    /// <returns>The combined token counts, or zero usage for an empty sequence.</returns>
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
