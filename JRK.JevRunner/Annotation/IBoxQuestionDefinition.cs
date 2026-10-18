namespace JRK.JevRunner.Annotation;

/// <summary>Defines instructions identifying a target and the image to search for a generated box question.</summary>
/// <typeparam name="TInstructionType">An instruction representation compatible with <see cref="Requests.JevMessage"/>.</typeparam>
/// <remarks>Query properties implementing this interface generate a box question and typed box answer accessors.</remarks>
public interface IBoxQuestionDefinition<out TInstructionType>
{
    /// <summary>Gets instructions identifying the target and the image asset in the query state.</summary>
    TInstructionType Instructions { get; }
}
