using JRK.JevRunner.Responses;
using Xunit;

namespace AnimalQuiz.Tests;

/// <summary>
/// Checks the local probability policy without contacting Jev.
/// </summary>
public class AnswerPolicyTests
{
    /// <summary>
    /// Confirms that clear answers and threshold boundaries produce the intended decisions.
    /// </summary>
    /// <param name="probability">The model's probability of yes.</param>
    /// <param name="expected">The expected gameplay answer.</param>
    [Theory]
    [InlineData(0.0, false)]
    [InlineData(0.3, false)]
    [InlineData(0.7, true)]
    [InlineData(1.0, true)]
    public void ClearAnswersProduceDecisions(double probability, bool expected)
    {
        Assert.Equal(expected, AnswerPolicy.Evaluate(new Noul(probability)));
    }

    /// <summary>
    /// Confirms that ambiguous answers are not silently presented as no.
    /// </summary>
    /// <param name="probability">A probability inside the uncertainty band.</param>
    [Theory]
    [InlineData(0.300001)]
    [InlineData(0.5)]
    [InlineData(0.699999)]
    public void UncertainAnswersHaveNoDecision(double probability)
    {
        Assert.Null(AnswerPolicy.Evaluate(new Noul(probability)));
    }
}
