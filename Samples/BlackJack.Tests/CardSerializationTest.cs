using System.Text.Json;
using Xunit;

namespace BlackJack.Tests;

/// <summary>Verifies that card ranks and suits round-trip as JSON strings.</summary>
public class CardSerializationTest
{
    /// <summary>Checks string enum serialization and deserialization for every card.</summary>
    [Fact]
    public void Card_RoundTripsWithStringRankAndSuit()
    {
        foreach (var card in Card.GenerateCards())
        {
            var json = JsonSerializer.Serialize(card);
            using var document = JsonDocument.Parse(json);

            Assert.Equal(card.Rank.ToString(), document.RootElement.GetProperty("Rank").GetString());
            Assert.Equal(card.Suit.ToString(), document.RootElement.GetProperty("Suit").GetString());
            Assert.Equal(card, JsonSerializer.Deserialize<Card>(json));
        }
    }
}
