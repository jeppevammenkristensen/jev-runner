using System.Collections.Immutable;
using BlackJack;

/// <summary>Represents the dealer's cards in the current round.</summary>
/// <param name="Cards">The cards currently held by the dealer.</param>
public record CurrentDealerHand(ImmutableArray<Card> Cards)
{
    /// <summary>Gets the hand total using high ace values when they do not cause a bust, otherwise low ace values.</summary>
    public decimal SumOfCards => Cards.CalculateBestSum();
}
