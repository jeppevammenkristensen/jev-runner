using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace BlackJack;

/// <summary>
/// Represents a playing card identified by its rank and suit.
/// </summary>
/// <param name="Rank">The card's rank, which determines its Halvtolv point value.</param>
/// <param name="Suit">The card's suit.</param>
public sealed record Card(
    [property: JsonConverter(typeof(JsonStringEnumConverter<Rank>))] Rank Rank,
    [property: JsonConverter(typeof(JsonStringEnumConverter<Suit>))] Suit Suit)
{
    /// <summary>Generates every rank and suit combination in a standard 52-card deck.</summary>
    /// <returns>The cards in suit order, then rank order, without shuffling.</returns>
    public static IEnumerable<Card> GenerateCards()
    {
        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                yield return new Card(rank, suit);
            }
        }
    }
}

/// <summary>
/// Manages a shuffled deck, tracks drawn cards, and permits one reshuffle of recorded cards.
/// </summary>
public class CardDeck
{
    /// <summary>Gets whether the deck has used its single permitted reshuffle.</summary>
    /// <remarks>A reshuffle will occur if all cards have been used. The cards not allready dealt are then shuffled</remarks>
    public bool HasBeenReshuffled { get; private set; } = false;
    
    private static readonly Random Random = new();

    /// <summary>Gets the cards recorded at round endings and available for reshuffling.</summary>
    public ImmutableArray<Card> DrawnCards { get; private set; } = ImmutableArray<Card>.Empty;

    /// <summary>Gets the remaining cards, with the next card to draw at the top of the stack.</summary>
    public Stack<Card> CurrentStack { get; private set; }
    
    /// <summary>Gets all cards drawn during this deck's session.</summary>
    public ImmutableArray<Card> CurrentSession { get; private set; } = ImmutableArray<Card>.Empty;
    
    /// <summary>Creates a complete 52-card deck and shuffles it for drawing.</summary>
    public CardDeck()
    {
        var cards = Card.GenerateCards().ToArray().AsSpan();
        Random.Shuffle(cards);
        CurrentStack =  new Stack<Card>([.. cards]);
    }

    /// <summary>Appends the session's drawn cards to the cards available for reshuffling.</summary>
    /// <remarks>The session's drawn-card collection is not cleared by this operation.</remarks>
    public void EndNewRound()
    {
        if (CurrentSession.Length > 0)
        {
            DrawnCards = DrawnCards.AddRange(CurrentSession);
        }
    }

    /// <summary>Draws and records the next card, reshuffling recorded cards if the stack is empty.</summary>
    /// <returns>The card removed from the top of the stack.</returns>
    /// <exception cref="InvalidOperationException">The empty stack has already been reshuffled, or no recorded cards are available to draw after reshuffling.</exception>
    public Card DrawCard()
    {
        if (CurrentStack.Count == 0)
        {
            Reshuffle();
        }

        var currentCard = CurrentStack.Pop();
        CurrentSession = CurrentSession.Add(currentCard);
        return currentCard;
    }

    /// <summary>Shuffles the recorded cards into a new stack and marks the single permitted reshuffle as used.</summary>
    /// <exception cref="InvalidOperationException">The deck has already been reshuffled.</exception>
    private void Reshuffle()
    {
        if (this.HasBeenReshuffled) throw new InvalidOperationException("Can only reshuffle once");

        var newSet = DrawnCards.ToArray();
        Random.Shuffle(newSet);
        DrawnCards = [];
        CurrentStack = new Stack<Card>(newSet);
        HasBeenReshuffled = true;

    }
}
