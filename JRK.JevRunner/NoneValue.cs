namespace JRK.JevRunner;

/// <summary>Marks the absence of a value in an optional union.</summary>
internal class NoneValue
{
    /// <summary>Creates the empty-case marker used by optional results.</summary>
    private NoneValue()
    {
        
    }
    
    /// <summary>
    /// Gets a fresh marker representing the empty case of an optional result.
    /// </summary>
    public static NoneValue New => new NoneValue();
}
