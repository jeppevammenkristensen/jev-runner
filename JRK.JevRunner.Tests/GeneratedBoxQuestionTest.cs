using System;
using System.Text.Json.Nodes;
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;
using JRK.JevRunner.Responses;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies generated box requests and answer lookups against the runtime API.</summary>
public partial class GeneratedBoxQuestionTest
{
    /// <summary>Box questions coexist with other question types and serialize without criteria.</summary>
    [Fact]
    public void GetRequest_MixedQuestions_SerializesBoxInstructions()
    {
        var query = new ImageQuery();
        var request = query.GetRequest();
        var questions = new JsonObject();
        Assert.Equal(2, request.Questions.Count);
        foreach (var question in request.Questions)
            question.AddTo(questions);

        var box = Assert.IsType<JsonObject>(questions["Head"]);
        Assert.Equal("box", box["type"]!.GetValue<string>());
        Assert.Equal(query.Head.Instructions, box["instructions"]!.GetValue<string>());
        Assert.False(box.ContainsKey("criteria"));
        Assert.Equal("noul", questions["Visible"]!["type"]!.GetValue<string>());
    }

    /// <summary>Generated methods and wrapper properties return the original box including confidence.</summary>
    [Fact]
    public void AnswerAccessors_BoxAnswer_ReturnOriginalAnswer()
    {
        var query = new ImageQuery();
        var answer = new BoxAnswer(0.1, 0.9, 0.2, 0.8, 0.95);
        var response = new Response([new("Head", answer)], new Usage(0, 0));
        var wrapper = new ImageQueryResponseWrapper(response);

        Assert.Same(answer, query.GetHeadAnswer(response));
        Assert.Same(answer, wrapper.Head);
        Assert.Same(response, wrapper.Response);
        Assert.Equal(0.95, wrapper.Head.Confidence.Value);
    }

    /// <summary>Box accessors retain the runtime failure behavior for missing and mismatched answers.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnswerAccessors_InvalidAnswer_Throw(bool missing)
    {
        var response = new Response([], new Usage(0, 0));
        if (!missing)
            response.Answers.Add(new("Head", new NoulAnswer(0.5)));
        var query = new ImageQuery();
        var wrapper = new ImageQueryResponseWrapper(response);

        Assert.Throws<InvalidOperationException>(() => query.GetHeadAnswer(response));
        Assert.Throws<InvalidOperationException>(() => wrapper.Head);
    }

    /// <summary>Generic definitions reject absent instructions consistently with other definition helpers.</summary>
    [Fact]
    public void GenericBoxDefinition_NullInstructions_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new GenericBoxQuestionDefinition<string>(null!));

    /// <summary>Combines a generic box definition with an interface-typed noul definition.</summary>
    [JevQuery]
    private partial class ImageQuery
    {
        public JevMessage State => "Search the supplied Photo image.";
        public JevModel JevModel => global::JRK.JevRunner.Requests.JevModel.Latest;
        public GenericBoxQuestionDefinition<string> Head { get; } = new("Find the head in `Photo`.");
        public INoulQuestionDefinition<string> Visible => new GenericNoulQuestionDefinition<string>("Is a head visible?", null, null);
    }
}
