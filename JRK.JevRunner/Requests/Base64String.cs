namespace JRK.JevRunner.Requests;

/// <summary>Identifies a string as file contents that are already base64-encoded.</summary>
/// <param name="Base64Encoded">The base64-encoded file contents; construction does not validate the encoding.</param>
public readonly record struct Base64String(string Base64Encoded)
{
    /// <summary>Wraps an already encoded string without validating or transforming it.</summary>
    /// <param name="base64Encoded">The base64-encoded file contents.</param>
    public static implicit operator Base64String(string base64Encoded) => new(base64Encoded);
    /// <summary>Returns the stored base64-encoded text.</summary>
    /// <param name="base64Encoded">The wrapper containing the encoded text.</param>
    public static implicit operator String(Base64String base64Encoded) => base64Encoded.Base64Encoded;
}
