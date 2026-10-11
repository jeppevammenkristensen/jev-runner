using JetBrains.Annotations;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;

namespace JRK.JevRunner.Annotation;

/// <summary>
/// Defines a choice question with instructions and choice criteria without requiring a dedicated definition class.
/// </summary>
/// <typeparam name="TInstruction">The type of the instructions.</typeparam>
[UsedImplicitly]
public class GenericChoiceQuestionDefinition<TInstruction> : IChoiceQuestionDefinition<TInstruction>
{
    /// <summary>
    /// Initializes a choice question definition with instructions and ordered choice criteria.
    /// </summary>
    /// <param name="instructions">The instructions describing how to select a choice.</param>
    /// <param name="choices">The ordered choice criteria. Must contain at least one choice.</param>
    public GenericChoiceQuestionDefinition(TInstruction instructions, params ChoiceCriteria[] choices)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        ThrowHelper.ThrowIfEmpty(choices);
        
        Instructions = instructions;
        Choices = choices;
    }

    /// <inheritdoc/>
    public TInstruction Instructions { get; }

    /// <inheritdoc/>
    public ChoiceCriteria[] Choices { get; }
}
