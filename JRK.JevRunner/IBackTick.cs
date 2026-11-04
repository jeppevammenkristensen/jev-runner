namespace JRK.JevRunner;

/// <summary>Provides state names and paths that are quoted when interpolated into instructions.</summary>
internal interface IBackTick
{
    public string Value { get; }

    /// <summary>
    /// The BackTick Quoted Value
    /// </summary>
    public string BackTickQuoted => $"`{Value}`";
}
