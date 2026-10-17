using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Represents text, an object array, or a single object used as request state or question instructions.
/// </summary>
public union JevMessage(string, object[],object)
{
    private static readonly JsonSerializerOptions TupleJsonOptions = new()
    {
        IncludeFields = true
    };

    /// <summary>
    /// Serializes the stored content to a JSON value, array, or object for inclusion in a request.
    /// </summary>
    /// <remarks>Value tuples include their fields using runtime names such as Item1 and Item2.</remarks>
    /// <exception cref="NotImplementedException">The stored value does not match a supported case.</exception>
    /// <remarks>If the underlying type is a value type it will be serialized as Item1,Item2...ItemN</remarks>
    public JsonNode Node => Value switch
    {
        object[] array => JsonSerializer.SerializeToNode(array)!.AsArray(),
        string str => JsonSerializer.SerializeToNode(str)!,
        ITuple tuple when tuple.GetType().IsValueType => JsonSerializer.SerializeToNode((object)tuple, TupleJsonOptions)!,
        { } single => JsonSerializer.SerializeToNode(single)!,
        
        _ => throw new NotImplementedException()
    };

    /// <summary>
    /// Normalizes the stored content to an array so messages can be concatenated without losing individual items.
    /// </summary>
    private object[] Array => Value switch
    {
        string str => [str],
        object[] array => array,
        { } s => [s],
        _ => throw new NotImplementedException()
    };
    
    /// <summary>
    /// Creates a message containing this message's items followed by the supplied message's items.
    /// </summary>
    public JevMessage CombineWith(JevMessage message)
    {
        var items = Array.Concat(message.Array);
        return items.ToArray();
    }
}
