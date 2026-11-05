namespace JRK.JevRunner;

/// <summary>
/// Utility helper for escaping a <see cref="FormattableString"/> ensuring
/// that <see cref="StateReference"/> or <see cref="StatePath"/> values are correctly wrapped
/// in backticks
/// </summary>
public static class Escape
{
    /// <summary>
    /// Use this apply escaping to an interpolated string. 
    /// </summary>
    /// <param name="formattableString"></param>
    /// <returns></returns>
    /// <example>$"{someStatePath}" becomes "`StatePathValue`"</example>
    public static string Interpolated(FormattableString formattableString)
    {
        return formattableString.Escaped;
    }
}