using JRK.JevRunner.Requests;

namespace JRK.JevRunner.Infrastructure;

/// <summary>
/// Stores named asset references as request state and tracks their associated assets.
/// </summary>
/// <remarks>A request constructed from this state copies its registered assets during construction;
/// assets registered afterward are not automatically added to that request.</remarks>
public class AssetState : Dictionary<string,string>
{
    /// <summary>
    /// Gets or sets the assets indexed by their state field names.
    /// </summary>
    internal Dictionary<string, Asset> Assets { get; set; } = [];

    /// <summary>
    /// Registers an asset and adds its reference to the state under the supplied field name.
    /// </summary>
    /// <param name="name">The state field that will contain the asset reference. </param>
    /// <param name="asset">The asset to include with the request; the <paramref name="name"/> field stores this asset's name.</param>
    /// <returns>The same state instance for further configuration.</returns>
    /// <exception cref="ArgumentException">The state already contains the supplied field name.</exception>
    public AssetState AddAsset(string name, Asset asset)
    {
        Add(name, asset.Name);
        Assets[name] = asset;
        return this;
    }
}