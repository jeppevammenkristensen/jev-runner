using JetBrains.Annotations;

namespace JRK.JevRunner.Annotation;

/// <summary>Defines a box question without requiring a dedicated definition class.</summary>
/// <typeparam name="TInstruction">The type of the image-search instructions.</typeparam>
[UsedImplicitly]
public class GenericBoxQuestionDefinition<TInstruction> : IBoxQuestionDefinition<TInstruction>
{
    /// <summary>Creates a box question definition with instructions identifying a target and image.</summary>
    /// <param name="instructions">The instructions identifying the target to locate and the image to search.</param>
    public GenericBoxQuestionDefinition(TInstruction instructions)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        Instructions = instructions;
    }

    /// <inheritdoc/>
    public TInstruction Instructions { get; }
}
