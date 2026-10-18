using System.Collections.Immutable;
using BlackJack;

/// <summary>Represents the information about the player's current hand available to the dealer.</summary>
/// <param name="KnownCards">The player's cards visible to the dealer.</param>
/// <param name="IsHalvTolv">Whether the player's full hand totals exactly 11.5 points.</param>
public record CurrentPlayerHand(ImmutableArray<Card> KnownCards, bool IsHalvTolv)
{
    /// <summary>Gets the visible card total using high ace values when they do not cause a bust, otherwise low ace values.</summary>
    public decimal SumOfKnownCards => KnownCards.CalculateBestSum();
}
