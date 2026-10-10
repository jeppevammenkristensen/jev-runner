using JRK.JevRunner.Annotation;

namespace AnimalQuiz;

/// <summary>
/// Defines a yes-or-no judgment for Jev to evaluate against the quiz state.
/// Its answer is a <c>Noul</c>, which exposes a numeric value and a yes/no decision.
/// </summary>
/// <param name="Instructions">The statement Jev should evaluate, referring to fields in the shared query state.</param>
/// <param name="Yes">Optional criteria describing a yes answer; used only when both criteria are supplied.</param>
/// <param name="No">Optional criteria describing a no answer; this sample leaves both criteria unset.</param>
public record NoulQuestion(string Instructions, string? Yes, string? No) : INoulQuestionDefinition<string>
{
}
