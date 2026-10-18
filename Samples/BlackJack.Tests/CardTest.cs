using System;
using System.Linq;
using JetBrains.Annotations;
using Xunit;

namespace BlackJack.Tests;

/// <summary>Verifies that card generation produces a complete standard deck.</summary>
[TestSubject(typeof(Card))]
public class CardTest
{
    /// <summary>Checks the standard deck size.</summary>
    [Fact]
    public void GenerateCards_Returns52Cards()
    {
        var cards = Card.GenerateCards().ToArray();

        Assert.Equal(52, cards.Length);
    }

    /// <summary>Checks that every generated card has a defined rank and suit.</summary>
    [Fact]
    public void GenerateCards_ReturnsOnlyValidCards()
    {
        var cards = Card.GenerateCards().ToArray();

        Assert.All(cards, card =>
        {
            Assert.True(Enum.IsDefined(card.Rank));
            Assert.True(Enum.IsDefined(card.Suit));
        });
    }

    /// <summary>Checks that no rank-and-suit combination is duplicated.</summary>
    [Fact]
    public void GenerateCards_ReturnsNoDuplicates()
    {
        var cards = Card.GenerateCards().ToArray();

        Assert.Equal(cards.Length, cards.Distinct().Count());
    }

    /// <summary>Checks that every rank, including ace and king, occurs once in each suit.</summary>
    [Fact]
    public void GenerateCards_IncludesEveryRankInEverySuit()
    {
        var cards = Card.GenerateCards().ToArray();

        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                Assert.Single(cards, card => card.Rank == rank && card.Suit == suit);
            }
        }
    }
}
