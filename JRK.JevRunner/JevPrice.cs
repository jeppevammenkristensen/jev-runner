namespace JRK.JevRunner;

/// <summary>
/// Represents the price for Jev requests
/// </summary>
/// <param name="PricePerMillionInputTokens">The price per million input tokens</param>
/// <param name="PricePerMillionOutputTokens">The price per million output tokens (defaults to 0)</param>
public readonly record struct JevPrice(decimal PricePerMillionInputTokens, decimal PricePerMillionOutputTokens = 0.0m)
{
    
}