using System.Collections.Immutable;
using BlackJack;
using JRK.JevRunner;
using Spectre.Console;

/// <summary>Coordinates interactive rounds of Halvtolv (a Danish blackjack variant) between a human player and a Jev-controlled dealer.</summary>
public class Game
{
    private readonly JevRunner _runner;
    List<PlayerRoundStats> roundStats = new List<PlayerRoundStats>();

    /// <summary>Creates a game that uses the supplied Jev runner for dealer decisions.</summary>
    public Game(JevRunner runner)
    {
        _runner = runner;
    }

    /// <summary>Plays interactive rounds with a new shuffled deck until its single reshuffle has been used.</summary>
    public async Task Play()
    {
        var cardDeck = new CardDeck();
        PlayerDetails[] players = [new("Person 1"), new("Dealer")];

        while (!cardDeck.HasBeenReshuffled)
        {
            await PlayRound(players, cardDeck);
            cardDeck.EndNewRound();
        }
    }

    /// <summary>Calculates the hand total using high ace values unless that total exceeds 11.5, then uses low ace values.</summary>
    private Decimal Calculate(IReadOnlyList<Card> cards)
    {
        var highest = cards.CalculateHighestSum();
        var lowest = cards.CalculateLowestSum();

        return highest <= 11.5m ? highest : lowest;
    }


    /// <summary>Plays the human and dealer turns, displays the outcome, and records the round's statistics.</summary>
    /// <param name="players">The game participants, with the human player at index zero.</param>
    /// <param name="cardDeck">The shared deck from which both hands draw cards.</param>
    private async Task PlayRound(PlayerDetails[] players, CardDeck cardDeck)
    {
        AnsiConsole.Write(new Rule($"Round {roundStats.Count + 1}"));
        
        var player = players[0];
        
        // Holds all the cards. Note that in some scenarios the dealer will not know what the first card
        // was. 
        var allPlayerCards = new List<(PlayerDetails, Card)>();

        var firstCard = cardDeck.DrawCard();
        allPlayerCards.Add((player, firstCard));
        AnsiConsole.MarkupLineInterpolated($"Player initial card: [green]{firstCard.Rank} {firstCard.Suit}[/]");
        bool busted = false;
        bool stopped = false;
        bool madeHalvTolv = false;
        Decimal playerSum = allPlayerCards.Select(x => x.Item2).ToArray().CalculateBestSum();

        // Continue until the player stops, is busted or reached HalvTolv (11.5 points) simular to BlackJack (21)
        while (!(busted || stopped || madeHalvTolv))
        {
            // Prompt the user if a new card should be drawn
            
            var drawAnotherCard = await AnsiConsole.PromptAsync(new ConfirmationPrompt("[green]Draw another card?[/]"));
            if (!drawAnotherCard)
            {
                stopped = true;
            }
            else
            {
                var card = cardDeck.DrawCard();
                AnsiConsole.WriteLine($"Player drew: [green]{card.Rank} {card.Suit}[/]");
                allPlayerCards.Add((player, card));
                playerSum = Calculate([.. allPlayerCards.Select(x => x.Item2)]);

                // If the player is over 11.5 he is busted exiting the loop
                if (playerSum > 11.5m)
                {
                    busted = true;
                }
                
                // if the player got 11.5 directly he has HalvTolv. exiting the loop
                if (playerSum == 11.5m)
                {
                    madeHalvTolv = true;
                }
            }
        }

        // Get the cards that the dealer will know of (We skip the first)
        var knownCards = allPlayerCards.Skip(1).Select(x => x.Item2).ToImmutableArray();
        
        // Create the CurrentPlayerHand that will be passed to the State for the jev query and the CurrentDealerHand
        // also used by the State.
        var currentPlayerHand = new CurrentPlayerHand(knownCards, madeHalvTolv);

        if (busted)
        { 
            // If the player display this. Update the PlayerRoundStats and continue. We will not query Jev here
            AnsiConsole.MarkupLineInterpolated($"[red]Player busted[/]");
            roundStats.Add(new PlayerRoundStats(KnownPlayerCards: knownCards, CardsInTotal: allPlayerCards.Count, PlayerBusted: true, PlayerWon: false, GotHalvTolv: false, DealerBusted: false, DealerCards: []));
            return;
        }
        
        AnsiConsole.MarkupLineInterpolated($"Player stopped {playerSum}");
        
        var initialDealerHand = cardDeck.DrawCard();
        AnsiConsole.MarkupLineInterpolated($"[green]Initial dealer hand {initialDealerHand.Rank} {initialDealerHand.Suit}[/]");
        
        CurrentDealerHand currentDealerHand = new CurrentDealerHand([initialDealerHand]);
        
        

        var dealerStopped = false;

        // Create the initial dealer state to pass to the JevQuery
        DealerState state =
            new DealerState(roundStats.ToImmutableArray(), currentPlayerHand, currentDealerHand);

        while (!dealerStopped)
        {
            // Important note. If we wanted to limit unnecessary calls to jev. It could be done if the player has 11.5. The way I played it you had to say
            // I got HalvTolv. In those cases the Dealer would try to reach 11.5 or bust, so we could just do that without "asking" jev.
            // With that said. This is at least attempted to be included in the instructions in the Query. Note the query is not that expensive. A general game of HalvTolv cost me $0.0026 
            
            // Create the jevquery to ask if the dealer should stop or draw a new card
            var query = new DealerQuery(state);

            DealerQueryResponseWrapper response = await query.Execute(_runner);

            // Jev suggest Stop
            if (response.DealerActions.Choice == "Stop")
            {
                AnsiConsole.MarkupLineInterpolated($"[green]Dealer stopped[/]");
                dealerStopped = true;
            }
            
            // Jev suggest to continue
            else
            {
                var card = cardDeck.DrawCard();
                AnsiConsole.MarkupLineInterpolated($"[green]Dealer drew another card {card.Rank} {card.Suit}[/]");
                var dealerCards = currentDealerHand.Cards.Add(card);

                // Update the currentDealerHand and the DealerState to prepare it for the next jev request
                
                currentDealerHand = currentDealerHand with
                {
                    Cards = dealerCards
                };

                state = state with
                {
                    CurrentDealerHand = currentDealerHand
                };

                // Check if the dealer reached 11.5 (HalvTolv) or was busted
                dealerStopped = currentDealerHand.SumOfCards > 11.5m;
            }
            
        }
        
        
        var dealerSum = currentDealerHand.SumOfCards;
        var dealerBusted = dealerSum > 11.5m;

        if (dealerBusted)
        {
            AnsiConsole.MarkupLineInterpolated($"[green]Dealer busted[/]");
        }
        
        var dealerWon = !dealerBusted && dealerSum >= playerSum;
        if (dealerWon)
        {
            AnsiConsole.MarkupLineInterpolated($"[red bold]Dealer won Dealer: {dealerSum} Player: {playerSum}[/]");    
        }
        else
        {
            AnsiConsole.MarkupLineInterpolated($"[green bold]Player won Dealer: {dealerSum} Player: {playerSum}[/]");    
        }
        
        // Add the stats for this "round"
        var playerRoundStats = new PlayerRoundStats(
            KnownPlayerCards: allPlayerCards.Select(x => x.Item2)
                .ToImmutableArray(), allPlayerCards.Count, false, dealerBusted, !dealerWon, madeHalvTolv,
            currentDealerHand.Cards);

        roundStats.Add(playerRoundStats);
    }
}
