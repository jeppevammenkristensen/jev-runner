namespace JRK.JevRunner.Requests;

/// <summary>
/// Represents file contents supplied as raw bytes or already base64-encoded text.
/// </summary>
public union FileData(byte[], Base64String)
{
    /// <summary>
    /// Returns the data as Base64 encoded
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The stored value is not a supported file data case.</exception>
    public string Base64 => this switch
    {
        byte[] bytes => Convert.ToBase64String(bytes),
        Base64String s => s.Base64Encoded,
        _ => throw new ArgumentOutOfRangeException()
    };

    /// <summary>Gets the raw file bytes, decoding base64 text when necessary.</summary>
    public byte[] Bytes => this switch
    {
        byte[] bytes => bytes,
        Base64String s => Convert.FromBase64String(s),
        _ => throw new ArgumentOutOfRangeException()
    };
}
