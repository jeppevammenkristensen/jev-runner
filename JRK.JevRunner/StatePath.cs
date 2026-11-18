using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace JRK.JevRunner;

public static class StateExtensions 
{
    /// <summary>
    /// Builds a state path from a property selector with optional constant array indexes.
    /// </summary>
    /// <typeparam name="T">The type of the root state.</typeparam>
    /// <typeparam name="TValue">The type of the selected value.</typeparam>
    /// <param name="source">The state that supplies the selector's root type; it is not read.</param>
    /// <param name="expression">A selector rooted at its lambda parameter.</param>
    /// <returns>The selected path without the lambda parameter name.</returns>
    /// <exception cref="ArgumentException">
    /// The selector is empty or contains an unsupported expression.
    /// </exception>
    /// <remarks>
    /// Selectors can chain instance properties and index arrays with non-negative integer constants.
    /// The selector is not executed. Examples:
    /// <list type="bullet">
    /// <item><description><c>x =&gt; x.Name</c> produces <c>Name</c>.</description></item>
    /// <item><description><c>x =&gt; x.Address.City</c> produces <c>Address.City</c>.</description></item>
    /// <item><description><c>x =&gt; x.Collection[1].Name</c> produces <c>Collection[1].Name</c>.</description></item>
    /// <item><description><c>x =&gt; x.Foo.Bar.Qux[1].Items[2]</c> produces <c>Foo.Bar.Qux[1].Items[2]</c>.</description></item>
    /// </list>
    /// </remarks>
    public static StatePath GetPath<T, TValue>(
        // ReSharper disable once UnusedParameter.Global
        this T source,
        Expression<Func<T, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var root = expression.Parameters[0];
        
        // Throw if it state => state
        if (ReferenceEquals(expression.Body, root))
            throw new ArgumentException("Select a property or array element.", nameof(expression));

        return new StatePath(BuildPath(expression.Body));

        string BuildPath(Expression node)
        {
            switch (node)
            {
                case MemberExpression { Member: PropertyInfo property, Expression: { } target }:
                    return ReferenceEquals(target, root)
                        ? property.Name
                        : $"{BuildPath(target)}.{property.Name}";

                case BinaryExpression
                {
                    NodeType: ExpressionType.ArrayIndex, // Must be and array index. For instance [2] or[1]
                    Left: var array,
                    Right: ConstantExpression { Value: int index } // Catch the index
                } when index >= 0:
                    return $"{BuildPath(array)}[{index.ToString(CultureInfo.InvariantCulture)}]";

                default:
                    throw new ArgumentException(
                        "Use instance properties and non-negative constant array indexes rooted at the lambda parameter.",
                        nameof(expression));
            }
        }
    }
}

/// <summary>Identifies a state field by path for backtick-quoted interpolation into instructions.</summary>
/// <remarks>Validates the text, but does not resolve the path against request state.</remarks>
public class StatePath : IBackTick
{
    /// <summary>
    /// Gets the validated, unquoted path identifying a field in request state.
    /// </summary>
    public string Path { get; }

    /// <summary>Creates a state path without whitespace or backticks.</summary>
    /// <param name="path">The nonempty path, such as <c>ticket.messages[0].text</c>.</param>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ArgumentException">The path is empty or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">The path contains whitespace or backticks.</exception>
    public StatePath(string path)
    {
        StateRegex.ThrowIfInvalid(path);
        Path = path;
    }

    /// <inheritdoc/>
    string IBackTick.Value => Path;
    
}
