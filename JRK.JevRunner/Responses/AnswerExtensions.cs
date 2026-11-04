namespace JRK.JevRunner.Responses;

/// <summary>Filters answer sequences to the requested answer kind while preserving their order.</summary>
public static class AnswerExtensions
{
    /// <summary>Enumerates only the noul answers in the supplied sequence.</summary>
    /// <param name="answers">The sequence of mixed answer kinds to filter.</param>
    public static IEnumerable<NoulAnswer> OnlyNoul(this IEnumerable<Answer> answers)
    {
        foreach (var answer in answers)
        {
            if (answer is NoulAnswer noulAnswer)
            {
                yield return noulAnswer;
            }
        }
    }
    
    /// <summary>Enumerates only the choice answers in the supplied sequence.</summary>
    /// <param name="answers">The sequence of mixed answer kinds to filter.</param>
    public static IEnumerable<ChoiceAnswer> OnlyChoice(this IEnumerable<Answer> answers)
    {
        foreach (var answer in answers)
        {
            if (answer is ChoiceAnswer noulAnswer)
            {
                yield return noulAnswer;
            }
        }
    }
    
    /// <summary>Enumerates only the score answers in the supplied sequence.</summary>
    /// <param name="answers">The sequence of mixed answer kinds to filter.</param>
    public static IEnumerable<ScoreAnswer> OnlyScoreAnswer(this IEnumerable<Answer> answers)
    {
        foreach (var answer in answers)
        {
            if (answer is ScoreAnswer scoreAnswer)
            {
                yield return scoreAnswer;
            }
        }
    }

    /// <summary>Enumerates only the box answers in the supplied sequence.</summary>
    /// <param name="answers">The sequence of mixed answer kinds to filter.</param>
    public static IEnumerable<BoxAnswer> OnlyBoxAnswer(this IEnumerable<Answer> answers)
    {
        foreach (var answer in answers)
        {
            if (answer is BoxAnswer boxAnswer)
            {
                yield return boxAnswer;
            }
        }
    }
}
