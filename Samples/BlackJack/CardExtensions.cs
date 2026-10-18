namespace BlackJack;

/// <summary>Provides hand scoring and card-rank classification helpers for Halvtolv (a Danish blackjack variant).</summary>
public static class CardExtensions
{

    /// <summary>Calculates the hand total with all aces worth 11, falling back to all aces worth 1 if the total exceeds 11.5.</summary>
    /// <returns>The high-ace total if it does not exceed 11.5; otherwise the low-ace total, which may still exceed 11.5.</returns>
    public static decimal CalculateBestSum(this IReadOnlyList<Card> cards)
    {
        var highestSum = CalculateHighestSum(cards);

        if (highestSum <= 11.5m)
        {
            return highestSum;
        }
        return CalculateLowestSum(cards);
    }
    
    /// <summary>Totals the cards with aces worth 11, face cards worth 0.5, and other cards worth their rank.</summary>
    public static decimal CalculateHighestSum(this IEnumerable<Card> cards)
    {
        return cards.Sum(x =>
        {
            return x.Rank switch
            {
                Rank.Ace => 11.0m,
                {IsRoyalty: true} => 0.5m,
                { } r => (decimal) r
            };
        });
    }
        
    /// <summary>Totals the cards with aces worth 1, face cards worth 0.5, and other cards worth their rank.</summary>
    public static decimal CalculateLowestSum(this IEnumerable<Card> cards)
    {
        return cards.Sum(x =>
        {
            return x.Rank switch
            {
                Rank.Ace => 1m,
                {IsRoyalty: true} => 0.5m,
                { } r => (decimal) r
            };
        });
    }
    
        
    extension(Rank rank)
    {
        /// <summary>Gets whether the rank is a jack, queen, or king, worth half a point in Halvtolv.</summary>
        private bool IsRoyalty => rank is Rank.Queen or Rank.Jack or Rank.King;
    }
}