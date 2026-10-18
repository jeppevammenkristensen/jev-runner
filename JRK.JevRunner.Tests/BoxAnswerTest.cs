using System;
using System.Linq;
using System.Text.Json.Nodes;
using JRK.JevRunner.Responses;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies the current Box answer type and dispatch from the API's named answer map.</summary>
public class BoxAnswerTest
{
    /// <summary>Normalized edges and answer-level confidence match the documented response shape.</summary>
    [Theory]
    [InlineData(0.25, 0.82, 0.44, 0.84, 0.92)]
    [InlineData(0, 1, 0, 1, 0)]
    [InlineData(0, 1, 0, 1, 1)]
    public void From_ApiPayload_ReadsEdgesAndAnswerLevelConfidence(
        double top, double bottom, double left, double right, double confidence)
    {
        var payload = CreatePayload(top, bottom, left, right, confidence);

        var answer = BoxAnswer.From(payload);

        Assert.Equal(top, answer.Top);
        Assert.Equal(bottom, answer.Bottom);
        Assert.Equal(left, answer.Left);
        Assert.Equal(right, answer.Right);
        Assert.Equal(confidence, answer.Confidence.Value);
    }

    /// <summary>Box confidence uses the same range validation as other confidence-bearing answers.</summary>
    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void From_InvalidConfidence_Throws(double confidence)
    {
        var payload = CreatePayload(0.25, 0.82, 0.44, 0.84, confidence);

        Assert.Throws<ArgumentOutOfRangeException>(() => BoxAnswer.From(payload));
    }

    /// <summary>Named Box and Noul answers coexist while unrecognized future types are skipped.</summary>
    [Fact]
    public void AnswerFrom_MixedAnswerTypes_PreservesNamedBoxAndNoulAnswers()
    {
        var payload = new JsonObject
        {
            ["mug"] = CreatePayload(0.25, 0.82, 0.44, 0.84, 0.92),
            ["mug_visible"] = JsonNode.Parse("""{"type":"noul","noul":0.95}"""),
            ["future"] = JsonNode.Parse("""{"type":"future"}""")
        };

        var answers = Answer.From(payload).ToArray();

        Assert.Equal(2, answers.Length);
        Assert.Equal("mug", answers[0].Name);
        Assert.Equal(0.92, Assert.IsType<BoxAnswer>(answers[0].Answer.Value).Confidence.Value);
        Assert.Equal("mug_visible", answers[1].Name);
        Assert.Equal(0.95, Assert.IsType<NoulAnswer>(answers[1].Answer.Value).Noul.Value);
    }

    /// <summary>Creates a Box answer with confidence alongside, rather than inside, its coordinate object.</summary>
    private static JsonObject CreatePayload(double top, double bottom, double left, double right, double confidence) =>
        new()
        {
            ["type"] = "box",
            ["box"] = new JsonObject
            {
                ["top"] = top,
                ["bottom"] = bottom,
                ["left"] = left,
                ["right"] = right
            },
            ["confidence"] = confidence
        };
}
