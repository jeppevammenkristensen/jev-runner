using JRK.JevRunner.Infrastructure;

namespace JRK.JevRunner.Annotation;

/// <summary>
/// Defines a score question with instructions and scoring criteria without requiring a dedicated definition class.
/// </summary>
/// <typeparam name="TInstruction">The type of the instructions.</typeparam>
public class GenericScoreQuestionDefinition<TInstruction> : IScoreQuestionDefinition<TInstruction>
{
    /// <summary>
    /// Initializes a score question definition with instructions and ordered scoring criteria.
    /// </summary>
    /// <param name="instructions">The instructions describing what should be scored.</param>
    /// <param name="criterias">The ordered scoring criteria. Must contain at least one criterion.</param>
    public GenericScoreQuestionDefinition(TInstruction instructions, params string[] criterias)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        ThrowHelper.ThrowIfEmpty(criterias);
        
        Instructions = instructions;
        Criterias = criterias;
    }

    /// <inheritdoc/>
    public TInstruction Instructions { get; }

    /// <inheritdoc/>
    public string[] Criterias { get; }
}
