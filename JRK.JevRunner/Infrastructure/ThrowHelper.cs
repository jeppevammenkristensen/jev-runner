using System.Runtime.CompilerServices;

namespace JRK.JevRunner.Infrastructure;

/// <summary>Provides shared validation for non-empty collections.</summary>
internal class ThrowHelper
{
    /// <summary>Throws when the supplied collection contains no elements.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="argument">The collection required to contain at least one element.</param>
    /// <param name="argumentName">The argument name included in the exception, inferred from the caller's expression when omitted.</param>
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
