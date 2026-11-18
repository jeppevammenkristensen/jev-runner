using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace JRK.JevRunner;

/// <summary>
/// Builds references to state fields from property and array selectors.
/// </summary>
public static class StateExtensions
{
    /// <summary>
    /// Builds a <see cref="StatePath"/> from a property selector with optional constant array indexes.
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
    /// If the state itself is an array, the path starts with the index.
    /// The selector is not executed. Examples:
    /// <list type="bullet">
    /// <item><description><c>x =&gt; x.Name</c> produces <c>Name</c>.</description></item>
    /// <item><description><c>x =&gt; x.Address.City</c> produces <c>Address.City</c>.</description></item>
    /// <item><description><c>x =&gt; x.Collection[1].Name</c> produces <c>Collection[1].Name</c>.</description></item>
    /// <item><description><c>x =&gt; x[0].Name</c> produces <c>[0].Name</c>.</description></item>
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
        if (ReferenceEquals(expression.Body, root))
            throw new ArgumentException("Select a property or array element.", nameof(expression));

        return new StatePath(BuildPath(expression.Body, root));
    }

    /// <summary>
    /// Builds a <see cref="StatePath"/> from a property selector with optional constant array indexes.
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
    /// If the state itself is an array, the path starts with the index.
    /// The selector is not executed. Examples:
    /// <list type="bullet">
    /// <item><description><c>x =&gt; x.Name</c> produces <c>Name</c>.</description></item>
    /// <item><description><c>x =&gt; x.Address.City</c> produces <c>Address.City</c>.</description></item>
    /// <item><description><c>x =&gt; x.Collection[1].Name</c> produces <c>Collection[1].Name</c>.</description></item>
    /// <item><description><c>x =&gt; x[0].Name</c> produces <c>[0].Name</c>.</description></item>
    /// <item><description><c>x =&gt; x.Foo.Bar.Qux[1].Items[2]</c> produces <c>Foo.Bar.Qux[1].Items[2]</c>.</description></item>
    /// </list>
    /// </remarks>
    public static StatePath GetPath<T, TValue>(
        // ReSharper disable once UnusedParameter.Global
        Expression<Func<T, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var root = expression.Parameters[0];
        if (ReferenceEquals(expression.Body, root))
            throw new ArgumentException("Select a property or array element.", nameof(expression));

        return new StatePath(BuildPath(expression.Body, root));
    }

    /// <summary>
    /// Builds a <see cref="StatePath"/> from a selector rooted at the current instance.
    /// </summary>
    /// <typeparam name="TValue">The type of the selected value.</typeparam>
    /// <param name="expression">A chain of instance properties and non-negative constant array indexes.</param>
    /// <returns>The selected path, such as <c>SomeProperty[0].Text</c>, without reading the instance.</returns>
    /// <exception cref="ArgumentException">The selector does not identify a supported path.</exception>
    public static StatePath GetPath<TValue>(Expression<Func<TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return new StatePath(BuildPath(expression.Body, null));
    }

    /// <summary>
    /// Walks a selector without evaluating it and omits its root from the resulting path.
    /// </summary>
    /// <param name="node">The expression representing the selected part of the state.</param>
    /// <param name="root">The state parameter, or null when the selector uses the current instance.</param>
    /// <returns>The unquoted path for the selected part of the state.</returns>
    private static string BuildPath(Expression node, ParameterExpression? root)
    {
        switch (node)
        {
            case MemberExpression { Member: PropertyInfo property, Expression: { } target }:
                // A parameterless selector represents the current instance as a constant.
                return ReferenceEquals(target, root) || (root is null && target is ConstantExpression)
                    ? property.Name
                    : $"{BuildPath(target, root)}.{property.Name}";

            case BinaryExpression
            {
                NodeType: ExpressionType.ArrayIndex,
                Left: var array,
                Right: ConstantExpression { Value: int index }
            } when index >= 0:
                var prefix = ReferenceEquals(array, root) ? string.Empty : BuildPath(array, root);
                return $"{prefix}[{index.ToString(CultureInfo.InvariantCulture)}]";

            default:
                throw new ArgumentException(
                    root is null
                        ? "Use instance properties and non-negative constant array indexes rooted at the current instance."
                        : "Use instance properties and non-negative constant array indexes rooted at the lambda parameter.",
                    "expression");
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
