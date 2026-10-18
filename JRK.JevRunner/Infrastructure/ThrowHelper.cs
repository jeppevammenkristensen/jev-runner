using System.Runtime.CompilerServices;

namespace JRK.JevRunner.Infrastructure;

/// <summary>Provides shared validation for non-empty collections.</summary>
internal class ThrowHelper
{
    /// <summary>Throws when the supplied collection contains no elements.</summary>
    /// <exception cref="ArgumentException">The collection is empty.</exception>
    public static void ThrowIfEmpty<T>(IReadOnlyCollection<T> argument,
        [CallerArgumentExpression(nameof(argument))] string? argumentName = null)
    {
        if (argument.Count == 0)
        {
            throw new ArgumentException("The collection cannot be empty", argumentName);
        }
    }
    
}