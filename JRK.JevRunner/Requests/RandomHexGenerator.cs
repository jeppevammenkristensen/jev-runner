using System.Security.Cryptography;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Creates random hexadecimal prefixes for multipart image field names.
/// </summary>
internal static class RandomHexGenerator
{
    /// <summary>
    /// Gets a fresh eight-character lowercase hexadecimal string from four random bytes.
    /// </summary>
    public static string RandomHex => Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
}