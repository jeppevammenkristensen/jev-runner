using Microsoft.CodeAnalysis;

namespace JRK.JevRunner.SourceGenerators;

/// <summary>
/// Couples a query property with its builder, typed response lookup, and configuration contract.
/// </summary>
internal sealed class QuestionMapping
{
    /// <summary>
    /// Associates a query property with its validated question construction and answer lookup contracts.
    /// </summary>
    /// <param name="property">The query property supplying the question definition.</param>
    /// <param name="interface">The constructed question definition interface implemented by the property type.</param>
    /// <param name="builder">The runtime builder used to construct the question.</param>
    /// <param name="answer">The runtime answer type returned by the response accessor.</param>
    /// <param name="accessor">The response method name used to retrieve the required named answer.</param>
    /// <param name="configuration">The configuration strategy applied to the question builder.</param>
    public QuestionMapping(IPropertySymbol property, INamedTypeSymbol @interface, INamedTypeSymbol builder,
        INamedTypeSymbol answer,
        string accessor, ConfigurationKind configuration)
    {
        Property = property;
        Interface = @interface;
        Builder = builder;
        Answer = answer;
        Accessor = accessor;
        Configuration = configuration;
    }

    /// <summary>
    /// Gets the query property supplying the question definition and generated answer name.
    /// </summary>
    public IPropertySymbol Property { get; }
    /// <summary>
    /// Gets the constructed interface exposing the question's instructions and configuration.
    /// </summary>
    public INamedTypeSymbol Interface { get; }
    /// <summary>
    /// Gets the runtime builder type used to construct the question.
    /// </summary>
    public INamedTypeSymbol Builder { get; }
    /// <summary>
    /// Gets the runtime answer type exposed by the generated answer accessor.
    /// </summary>
    public INamedTypeSymbol Answer { get; }
    /// <summary>
    /// Gets the response method name used to retrieve the required named answer.
    /// </summary>
    public string Accessor { get; }
    /// <summary>
    /// Gets the strategy used to apply criteria or choices to the question builder.
    /// </summary>
    public ConfigurationKind Configuration { get; }
}
