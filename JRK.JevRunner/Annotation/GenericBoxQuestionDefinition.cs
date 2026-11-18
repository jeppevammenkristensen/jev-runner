using JetBrains.Annotations;
using JRK.JevRunner.Requests;

namespace JRK.JevRunner.Annotation;

/// <summary>
/// Creates generic question definitions without requiring a dedicated definition class.
/// </summary>
/// <remarks>This is meant as convenience methods for creating the QuestionDefinitions used by jev Query using the source generator logic</remarks>
public static class QuestionDefinition
{
    /// <summary>
    /// Creates a Noul question while preserving interpolation arguments for instruction formatting.
    /// </summary>
    /// <param name="instructions">The interpolated instructions for the yes-or-no judgment.</param>
    /// <param name="yes">The optional definition of a yes answer.</param>
    /// <param name="no">The optional definition of a no answer.</param>
    public static GenericNoulQuestionDefinition<FormattableString> NoulInterpolated(FormattableString instructions,
        string? yes = null, string? no = null)
    {
        return new GenericNoulQuestionDefinition<FormattableString>(instructions, yes, no);
    }

    /// <summary>
    /// Creates a Noul question with instructions of the supplied type.
    /// </summary>
    /// <typeparam name="T">The instruction type.</typeparam>
    /// <param name="instructions">The instructions for the yes-or-no judgment.</param>
    /// <param name="yes">The optional definition of a yes answer.</param>
    /// <param name="no">The optional definition of a no answer.</param>
    public static GenericNoulQuestionDefinition<T> Noul<T>(T instructions,
        string? yes = null, string? no = null)
    {
        return new GenericNoulQuestionDefinition<T>(instructions, yes, no);
    }

    /// <summary>
    /// Creates a box question while preserving interpolation arguments for instruction formatting.
    /// </summary>
    /// <param name="instructions">The interpolated instructions identifying the image and target.</param>
    public static GenericBoxQuestionDefinition<FormattableString> BoxInterpolated(FormattableString instructions) =>
        new(instructions);

    /// <summary>
    /// Creates a box question with instructions of the supplied type.
    /// </summary>
    /// <typeparam name="T">The instruction type.</typeparam>
    /// <param name="instructions">The instructions identifying the image and target.</param>
    public static GenericBoxQuestionDefinition<T> Box<T>(T instructions) => new(instructions);

    /// <summary>
    /// Creates a choice question while preserving interpolation arguments for instruction formatting.
    /// </summary>
    /// <param name="instructions">The interpolated instructions for selecting a choice.</param>
    /// <param name="choices">The ordered choices. At least one is required.</param>
    public static GenericChoiceQuestionDefinition<FormattableString> ChoiceInterpolated(
        FormattableString instructions, params ChoiceCriteria[] choices) => new(instructions, choices);

    /// <summary>
    /// Creates a choice question with instructions of the supplied type.
    /// </summary>
    /// <typeparam name="T">The instruction type.</typeparam>
    /// <param name="instructions">The instructions for selecting a choice.</param>
    /// <param name="choices">The ordered choices. At least one is required.</param>
    public static GenericChoiceQuestionDefinition<T> Choice<T>(T instructions, params ChoiceCriteria[] choices) =>
        new(instructions, choices);

    /// <summary>
    /// Creates a score question while preserving interpolation arguments for instruction formatting.
    /// </summary>
    /// <param name="instructions">The interpolated instructions describing what to score.</param>
    /// <param name="criterias">The ordered scoring criteria. At least one is required.</param>
    public static GenericScoreQuestionDefinition<FormattableString> ScoreInterpolated(
        FormattableString instructions, params string[] criterias) => new(instructions, criterias);

    /// <summary>
    /// Creates a score question with instructions of the supplied type.
    /// </summary>
    /// <typeparam name="T">The instruction type.</typeparam>
    /// <param name="instructions">The instructions describing what to score.</param>
    /// <param name="criterias">The ordered scoring criteria. At least one is required.</param>
    public static GenericScoreQuestionDefinition<T> Score<T>(T instructions, params string[] criterias) =>
        new(instructions, criterias);
}


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
