using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>
/// Verifies that the question factories preserve instructions and ordered criteria.
/// </summary>
public class QuestionDefinitionTest
{
    /// <summary>
    /// Box factories preserve plain instructions and keep state paths available for backtick quoting.
    /// </summary>
    [Fact]
    public void BoxFactories_KeepInstructions()
    {
        var photo = new StatePath("Photo");
        var plain = QuestionDefinition.Box("Find the cat.");
        var interpolated = QuestionDefinition.BoxInterpolated($"Find the cat in {photo}.");

        Assert.Equal("Find the cat.", plain.Instructions);
        Assert.Equal("Find the cat in `Photo`.", interpolated.Instructions.Escaped);
    }

    /// <summary>
    /// Choice factories preserve instructions and forward choices in the supplied order.
    /// </summary>
    [Fact]
    public void ChoiceFactories_KeepInstructionsAndOrderedChoices()
    {
        var photo = new StatePath("Photo");
        var cat = new ChoiceCriteria("cat", "The photo shows a cat.");
        var dog = new ChoiceCriteria("dog", "The photo shows a dog.");
        var plain = QuestionDefinition.Choice("Classify the photo.", cat, dog);
        var interpolated = QuestionDefinition.ChoiceInterpolated($"Classify {photo}.", cat, dog);

        Assert.Equal("Classify the photo.", plain.Instructions);
        Assert.Equal("Classify `Photo`.", interpolated.Instructions.Escaped);
        Assert.Equal(new[] { cat, dog }, plain.Choices);
        Assert.Equal(new[] { cat, dog }, interpolated.Choices);
    }

    /// <summary>
    /// Score factories preserve instructions and forward criteria in the supplied order.
    /// </summary>
    [Fact]
    public void ScoreFactories_KeepInstructionsAndOrderedCriteria()
    {
        var photo = new StatePath("Photo");
        var plain = QuestionDefinition.Score("Score the photo.", "Sharp", "Well lit");
        var interpolated = QuestionDefinition.ScoreInterpolated($"Score {photo}.", "Sharp", "Well lit");

        Assert.Equal("Score the photo.", plain.Instructions);
        Assert.Equal("Score `Photo`.", interpolated.Instructions.Escaped);
        Assert.Equal(new[] { "Sharp", "Well lit" }, plain.Criterias);
        Assert.Equal(new[] { "Sharp", "Well lit" }, interpolated.Criterias);
    }
}
