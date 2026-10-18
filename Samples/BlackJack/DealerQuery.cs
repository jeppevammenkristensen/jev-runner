using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;

/// <summary>Asks the Jev model whether the dealer should stop or draw based on the supplied game state.</summary>
[JevQuery]
public partial class DealerQuery
{
    /// <summary>Creates a dealer-action query using the supplied game state.</summary>
    public DealerQuery(DealerState state)
    {
        State = state;
    }

    /// <summary>Gets the round history, current hands, and rules supplied to the model.</summary>
    /// <remarks>The State object holds information about the preceeding rounds. Who won. To make it fair the dealer will not know the original card if he or the player are busted</remarks>
    public DealerState State { get; private set; }
    /// <summary>Gets the Jev model used to choose the dealer's action.</summary>
    public JevModel JevModel { get; private set; } = JevModel.Latest;

    /// <summary>
    /// Gets the stop and continue choices and their decision criteria.
    /// </summary>
    /// <remarks>Much of the basis (hopefully) for Jev to make a best guess is based on the State</remarks>
    public GenericChoiceQuestionDefinition<string> DealerActions => new GenericChoiceQuestionDefinition<string>(
        "Use a combination of all the State information to determine the dealer action. The Player will never be busted when you are called",
        new ChoiceCriteria("Stop",
            "Evaluate if you should stop. If the CurrentPlayerKnownCards has HalvTolv dont stop. Stop if you reason that the chance of bust is to high or you are as confident as possible that you have the same or more than the Player. Also typically stop if you have 10 or an Aces as the single card and the user has no known cards"),
        new ChoiceCriteria("Continue",
            "Evaluate if you should continue. If the CurrentPlayerKnownCards has HalvTolv continue. Continue if you reason that you have a lower sum than the player and the player isn't bluffing"));
}
