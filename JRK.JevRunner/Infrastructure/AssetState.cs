using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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

    /// <summary>Combines derived state properties and dictionary entries into the request's state object.</summary>
    /// <remarks>
    /// Includes public readable instance properties declared below AssetState in the inheritance hierarchy.
    /// Dictionary infrastructure, indexers, and properties marked with JsonIgnore are excluded.
    /// JsonPropertyName controls property names. Property values use the default JSON serializer options.
    /// </remarks>
    /// <returns>A new JSON object containing the state data and named asset references.</returns>
    /// <exception cref="InvalidOperationException">Two state members have the same JSON name.</exception>
    public JsonObject ToStateJson()
    {
        var json = new JsonObject();

        foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.DeclaringType is null ||
                !property.DeclaringType.IsSubclassOf(typeof(AssetState)) ||
                property.GetMethod is not { IsPublic: true } ||
                property.GetIndexParameters().Length != 0)
                continue;

            var ignore = property.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ignore?.Condition == JsonIgnoreCondition.Always)
                continue;

            var value = property.GetValue(this);
            if (value is FormattableString formattableString)
            {
                value = formattableString.Escaped;
            }
            
            if (ignore?.Condition == JsonIgnoreCondition.WhenWritingNull && value is null)
                continue;
            if (ignore?.Condition == JsonIgnoreCondition.WhenWritingDefault &&
                (value is null || (property.PropertyType.IsValueType &&
                    value.Equals(Activator.CreateInstance(property.PropertyType)))))
                continue;

            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
            AddStateMember(json, name, JsonSerializer.SerializeToNode(value));
        }

        foreach (var entry in this)
            AddStateMember(json, entry.Key, JsonValue.Create(entry.Value));

        return json;
    }

    /// <summary>Adds a state member without silently overwriting a property or asset reference.</summary>
    /// <param name="json">The state object receiving the member.</param>
    /// <param name="name">The JSON member name, which must not already exist in the state object.</param>
    /// <param name="value">The member's JSON value, or null for a JSON null value.</param>
    private static void AddStateMember(JsonObject json, string name, JsonNode? value)
    {
        if (json.ContainsKey(name))
            throw new InvalidOperationException($"State contains duplicate JSON member '{name}'.");

        json.Add(name, value);
    }

    /// <summary>
    /// Registers an asset and adds its reference to the state under the supplied field name.
    /// </summary>
    /// <param name="name">The state field that will contain the asset reference. </param>
    /// <param name="asset">The asset to include with the request; the <paramref name="name"/> field stores this asset's name.</param>
    /// <returns>The same state instance for further configuration.</returns>
    /// <exception cref="ArgumentException">The state already contains the supplied field name.</exception>
    public AssetState AddAsset(StateReference name, Asset asset)
    {
        Add(name.Value, asset.Name);
        Assets[name.Value] = asset;
        return this;
    }

    /// <summary>
    /// Gets the registered asset associated with a required state field.
    /// </summary>
    /// <param name="name">The state field name under which the asset was registered.</param>
    /// <returns>The asset registered for the state field.</returns>
    /// <exception cref="InvalidOperationException">No asset is registered for the supplied state field name.</exception>
    public Asset GetRequiredAsset(string name)
    {
        if (Assets.TryGetValue(name, out var asset))
        {
            return asset;
        }

        throw new InvalidOperationException($"ImageAsset was not found for key {name}");
    }
    
    /// <summary>
    /// Gets the registered image asset associated with a required state field.
    /// </summary>
    /// <param name="name">The state field reference under which the image asset was registered.</param>
    /// <returns>The image asset registered for the state field.</returns>
    /// <exception cref="InvalidOperationException">The state field has no registered asset, or its asset is not an image.</exception>
    public ImageAsset GetRequiredImageAsset(StateReference name)
    {
        return GetRequiredAsset(name.Value) switch
        {
            ImageAsset a => a,
            _ => throw new InvalidOperationException($"ImageAsset was not found for key {name}")
        };
    }
}
