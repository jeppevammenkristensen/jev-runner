using System.Collections.Immutable;

/// <summary>Collects the history, visible hands, and rules used to decide the dealer's next action.</summary>
/// <param name="RoundStats">The recorded outcomes and cards from previous rounds.</param>
/// <param name="CurrentPlayerHand">The information available about the player's current hand.</param>
/// <param name="CurrentDealerHand">The dealer's current cards and hand total.</param>
public record DealerState(
    ImmutableArray<PlayerRoundStats> RoundStats,
    CurrentPlayerHand CurrentPlayerHand,
    CurrentDealerHand CurrentDealerHand)
{
    /// <summary>Gets the Halvtolv rules and guidance supplied to the dealer model.</summary>
    public string GameRules => """
                               You are the dealer in Halvtolv, a Danish blackjack variant played with a standard 52-card deck.
                               Aim to beat the players by getting as close to 11.5 points as possible without exceeding it.
                               Cards 2–10 count at face value; jacks, queens, and kings count as 0.5; aces count as 1 or 11.
                               Use the highest hand total that does not exceed 11.5; if no such total exists, the hand is busted and loses.
                               Drawing adds a card and risks busting; standing keeps the current total and ends your turn. At 11.5, do not draw again.
                               Busted players cannot win. Compare your hand against each remaining player's total.
                               Some player cards may be hidden: use the supplied state without assuming unknown card identities.
                               Choose among the supplied moves using their descriptions; do not assume standard blackjack dealer thresholds or unstated tie rules. Assume that the player can bluff. If you get the exact value as the user you win"
                               """;
}
