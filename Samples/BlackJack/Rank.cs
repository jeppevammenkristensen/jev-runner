using System.Collections.Immutable;

namespace BlackJack;

/// <summary>Identifies a card rank, not its blackjack point value.</summary>
public enum Rank
{
    Ace = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13
}

// The player Session holds some properties not effectively used. Like Wins and Identifier. Kept if the game is modified later