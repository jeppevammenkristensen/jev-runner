using System.Buffers.Text;
using System.Text.Json.Nodes;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies Box question payloads and their inclusion in mixed runtime requests.</summary>
public class BoxQuestionTest
{
    /// <summary>A Box payload contains only the type and instructions, without criteria or its name.</summary>
    [Fact]
    public void Node_TextInstructions_SerializesBoxPayload()
    {
        var question = new BoxQuestion("mug", "the coffee mug in `photo`");

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(
            """{"type":"box","instructions":"the coffee mug in `photo`"}"""), question.Node));
    }

    /// <summary>Structured instructions retain their fields and image references.</summary>
    [Fact]
    public void Node_StructuredInstructions_PreservesStructure()
    {
        var instructions = new {target = "the mug", image = "image-ref"};
        var question = new BoxQuestion("mug", new JevMessage(instructions));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(
            """{"target":"the mug","image":"image-ref"}"""), question.Node!["instructions"]));
    }

    /// <summary>The question union adds Box payloads by name and can serialize them repeatedly.</summary>
    [Fact]
    public void AddTo_BoxQuestion_ReplacesExistingNameAndProducesIndependentNodes()
    {
        Question question = new BoxQuestion("mug", "the mug in `photo`");
        var first = new JsonObject { ["mug"] = "old value" };
        var second = new JsonObject();

        question.AddTo(first);
        question.AddTo(second);

        Assert.Single(first);
        Assert.Equal("box", first["mug"]!["type"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(first, second));
        Assert.NotSame(first["mug"], second["mug"]);
    }

    /// <summary>Box and Noul questions share the image-backed state and retain the requested model.</summary>
    [Fact]
    public void CreateRequestJson_MixedBoxAndNoul_SerializesQuestionsAndAsset()
    {
        var state = new AssetState().AddAsset("photo", new ImageAsset("image-ref", "jpg", (Base64String)"AQID"));
        var request = new Request(state, new JevModel("jev-1.14.0"))
            .AddQuestion(new BoxQuestion("mug", "the coffee mug in `photo`"))
            .AddQuestion(new NoulQuestion("mug_visible", "Is there a mug in `photo`?", null));

        var json = JsonTranslator.CreateRequestJson(request);

        Assert.Equal("jev-1.14.0", json["model"]!.GetValue<string>());
        Assert.Equal("image-ref", json["state"]!["photo"]!.GetValue<string>());
        Assert.Equal("AQID", json["assets"]!["image-ref"]!["data_base64"]!.GetValue<string>());
        Assert.Equal("box", json["questions"]!["mug"]!["type"]!.GetValue<string>());
        Assert.False(json["questions"]!["mug"]!.AsObject().ContainsKey("criteria"));
        Assert.Equal("noul", json["questions"]!["mug_visible"]!["type"]!.GetValue<string>());
    }
}
