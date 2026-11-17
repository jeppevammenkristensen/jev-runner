using JRK.JevRunner.Responses;

namespace AnimalQuiz;

/// <summary>
/// Converts Jev's yes probability into a decision while preserving uncertain answers.
/// </summary>
public static class AnswerPolicy
{
    /// <summary>
    /// Returns yes or no only outside the uncertainty band.
    /// </summary>
    /// <param name="answer">The probability returned by a Noul judgment.</param>
    /// <returns>True at or above 0.7, false at or below 0.3, otherwise null.</returns>
    /// <remarks>
    /// These are initial gameplay thresholds, not measured accuracy guarantees.
    /// Non-finite or out-of-range values are treated as uncertain.
    /// </remarks>
    public static bool? Evaluate(Noul answer)
    {
        var value = answer.Value;
        if (value >= 0 && value <= 0.3)
            return false;
        if (value >= 0.7 && value <= 1)
            return true;
        return null;
    }
}
