using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace JRK.JevRunner;

/// <summary>Validates state references and paths before they are quoted in instructions.</summary>
internal static partial class StateRegex
{
    [GeneratedRegex("^[^`\\s]+$")]
    internal static partial Regex ValidPathName { get; }

    /// <summary>Rejects empty state identifiers and identifiers containing whitespace or backticks.</summary>
    /// <param name="value">The state name or path to validate.</param>
    /// <param name="paramName">The parameter name to report; defaults to the caller's value expression.</param>
    /// <exception cref="ArgumentException">The value is empty or consists only of whitespace.</exception>
    /// <exception cref="ArgumentNullException">The value is null.</exception>
    /// <exception cref="InvalidOperationException">The value contains whitespace or backticks.</exception>
    internal static void ThrowIfInvalid(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        if (ValidPathName.IsMatch(value))
            return;

        throw new InvalidOperationException($"{paramName} cannot have whitespace or the backtick character `");
    }
}
