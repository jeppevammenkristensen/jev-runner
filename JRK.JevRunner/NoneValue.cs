namespace JRK.JevRunner;

/// <summary>Marks the absence of a value in an optional union.</summary>
internal class NoneValue
{
    /// <summary>Creates the empty-case marker used by optional results.</summary>
    private NoneValue()
    {
        
    }
    
    public static NoneValue New => new NoneValue();
}
