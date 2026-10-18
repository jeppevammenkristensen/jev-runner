using System.Collections.Immutable;
using BlackJack;

/// <summary>Records a completed round's cards and outcome for use in later dealer decisions.</summary>
/// <param name="KnownPlayerCards">The player cards recorded as known to the dealer.</param>
/// <param name="CardsInTotal">The total number of cards held by the player.</param>
/// <param name="PlayerBusted">Whether the player's hand exceeded 11.5 points.</param>
/// <param name="DealerBusted">Whether the dealer's hand exceeded 11.5 points.</param>
/// <param name="PlayerWon">Whether the player won the round.</param>
/// <param name="GotHalvTolv">Whether the player's hand totaled exactly 11.5 points.</param>
/// <param name="DealerCards">The dealer cards recorded for the round.</param>
public record PlayerRoundStats(
    ImmutableArray<Card> KnownPlayerCards,
    int CardsInTotal,
    bool PlayerBusted,
    bool DealerBusted,
    bool PlayerWon,
    bool GotHalvTolv,
    ImmutableArray<Card> DealerCards)
{
    /// <summary>Gets the known player card total using high ace values when they do not cause a bust, otherwise low ace values.</summary>
    public decimal KnownSum => KnownPlayerCards.CalculateBestSum();
    /// <summary>Gets whether some of the player's cards are absent from the known cards.</summary>
    public bool DidNotDisplayAllCards => CardsInTotal > KnownPlayerCards.Length;
}
