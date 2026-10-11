using JetBrains.Annotations;

namespace JRK.JevRunner.Annotation;

/// <summary>
/// A generic <see cref="INoulQuestionDefinition{TInstructionType}"/> that you use if you don't want to define this
/// in an individual class
/// </summary>
/// <typeparam name="TInstruction">The type of the instructions</typeparam>
/// <remarks>Note only if text is set for Yes and No they will be used</remarks>
[UsedImplicitly]
public class GenericNoulQuestionDefinition<TInstruction> : INoulQuestionDefinition<TInstruction>
{
    /// <summary>
    /// A generic <see cref="INoulQuestionDefinition{TInstructionType}"/> that you use if you don't want to define this
    /// in an individual class
    /// </summary>
    /// <param name="instructions">The instructions</param>
    /// <param name="yes">The text to help define yes. Can be null.</param>
    /// <param name="no">The text to help define no. Can be null</param>
    /// <remarks>Note only if text is set for Yes and No they will be used</remarks>
    public GenericNoulQuestionDefinition(TInstruction instructions, string? yes, string? no)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        
        Instructions = instructions;
        Yes = yes;
        No = no;
    }

    /// <inheritdoc/>
    public TInstruction Instructions { get; }

    /// <inheritdoc/>
    public string? Yes { get; }

    /// <inheritdoc/>
    public string? No { get; }
}
