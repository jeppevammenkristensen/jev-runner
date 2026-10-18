using System;
using System.Linq;
using JetBrains.Annotations;
using Xunit;

namespace BlackJack.Tests;

/// <summary>Verifies deck contents, round tracking, and the single-reshuffle limit.</summary>
[TestSubject(typeof(CardDeck))]
public class CardDeckTest
{
    /// <summary>Checks that a new deck contains one complete set of cards and no history.</summary>
    [Fact]
    public void Constructor_CreatesCompleteDeckWithEmptyHistory()
    {
        var deck = new CardDeck();

        Assert.Equal(52, deck.CurrentStack.Count);
        Assert.Equal(52, deck.CurrentStack.Distinct().Count());
        Assert.All(Card.GenerateCards(), card => Assert.Contains(card, deck.CurrentStack));
        Assert.Empty(deck.CurrentSession);
        Assert.Empty(deck.DrawnCards);
        Assert.False(deck.HasBeenReshuffled);
    }

    /// <summary>Checks that drawing removes and returns the top card.</summary>
    [Fact]
    public void DrawCard_RemovesTopCardAndTracksItInCurrentSession()
    {
        var deck = new CardDeck();
        var expected = deck.CurrentStack.Peek();

        var drawn = deck.DrawCard();

        Assert.Equal(expected, drawn);
        Assert.Equal(51, deck.CurrentStack.Count);
        Assert.DoesNotContain(drawn, deck.CurrentStack);
        Assert.Equal(drawn, Assert.Single(deck.CurrentSession));
        Assert.Empty(deck.DrawnCards);
    }

    /// <summary>Checks that the initial deck can be drawn completely without repeats or reshuffling.</summary>
    [Fact]
    public void DrawCard_First52Draws_ReturnEveryCardExactlyOnce()
    {
        var deck = new CardDeck();

        var drawn = Enumerable.Range(0, 52)
            .Select(_ => deck.DrawCard()).ToArray();

        Assert.Equal(52, drawn.Distinct().Count());
        Assert.All(Card.GenerateCards(), card => Assert.Contains(card, drawn));
        Assert.Equal(drawn, deck.CurrentSession.ToArray());
        Assert.Empty(deck.CurrentStack);
        Assert.False(deck.HasBeenReshuffled);
    }

    /// <summary>Checks that ending a round transfers its cards to the discard history.</summary>
    [Fact]
    public void EndNewRound_MovesSessionToDrawnCardsAndClearsSession()
    {
        var deck = new CardDeck();
        var first = deck.DrawCard();
        var second = deck.DrawCard();

        deck.EndNewRound();

        Assert.Equal(new[] { first, second }, deck.DrawnCards.ToArray());
        Assert.Empty(deck.CurrentSession);
        Assert.Equal(50, deck.CurrentStack.Count);
    }

    /// <summary>Checks that an empty round does not alter the deck.</summary>
    [Fact]
    public void EndNewRound_WithoutDraws_LeavesDeckUnchanged()
    {
        var deck = new CardDeck();
        var originalStack = deck.CurrentStack.ToArray();

        deck.EndNewRound();

        Assert.Equal(originalStack, deck.CurrentStack.ToArray());
        Assert.Empty(deck.CurrentSession);
        Assert.Empty(deck.DrawnCards);
        Assert.False(deck.HasBeenReshuffled);
    }

    /// <summary>Checks that ending a round twice cannot duplicate discarded cards.</summary>
    [Fact]
    public void EndNewRound_CalledTwice_DoesNotDuplicateDrawnCards()
    {
        var deck = new CardDeck();
        var drawn = deck.DrawCard();

        deck.EndNewRound();
        deck.EndNewRound();

        Assert.Equal(drawn, Assert.Single(deck.DrawnCards));
    }

    /// <summary>Checks that separate rounds append only their own cards to the discard history.</summary>
    [Fact]
    public void EndNewRound_AcrossRounds_PreservesDrawOrderWithoutDuplicates()
    {
        var deck = new CardDeck();
        var first = deck.DrawCard();
        deck.EndNewRound();
        var second = deck.DrawCard();

        deck.EndNewRound();

        Assert.Equal(new[] { first, second }, deck.DrawnCards.ToArray());
        Assert.Empty(deck.CurrentSession);
    }

    /// <summary>Checks that an exhausted deck reuses discarded cards without losing or duplicating any.</summary>
    [Fact]
    public void DrawCard_WhenDeckIsEmpty_ReshufflesDiscardedCardsOnce()
    {
        var deck = new CardDeck();
        for (var i = 0; i < 52; i++)
        {
            deck.DrawCard();
        }
        deck.EndNewRound();

        var drawn = deck.DrawCard();

        Assert.True(deck.HasBeenReshuffled);
        Assert.Empty(deck.DrawnCards);
        Assert.Equal(51, deck.CurrentStack.Count);
        var recycledCards = deck.CurrentStack.Append(drawn).ToArray();
        Assert.Equal(52, recycledCards.Distinct().Count());
        Assert.All(Card.GenerateCards(), card => Assert.Contains(card, recycledCards));
    }

    /// <summary>Checks that drawing after exhausting the reshuffled deck rejects a second reshuffle.</summary>
    [Fact]
    public void DrawCard_AfterExhaustingReshuffledDeck_ThrowsInvalidOperationException()
    {
        var deck = new CardDeck();
        for (var round = 0; round < 2; round++)
        {
            for (var i = 0; i < 52; i++)
            {
                deck.DrawCard();
            }
            deck.EndNewRound();
        }

        Assert.Throws<InvalidOperationException>(() => deck.DrawCard());
    }
}
