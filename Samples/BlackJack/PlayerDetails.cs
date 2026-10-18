namespace BlackJack;

/// <summary>Holds a participant's identity, win count, and known cards during a game session.</summary>
public class PlayerDetails
{
    /// <summary>Gets the participant's win count.</summary>
    public int Wins { get; }
    
    /// <summary>Gets the name identifying this participant.</summary>
    public string Identifier { get; }

    /// <summary>Creates a participant session with the supplied identifier and no known cards.</summary>
    public PlayerDetails(string identifier)
    {
        Identifier = identifier;
    }
}