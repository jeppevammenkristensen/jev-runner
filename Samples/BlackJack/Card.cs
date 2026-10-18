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