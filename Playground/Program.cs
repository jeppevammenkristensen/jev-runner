using System.IO.Abstractions;
using AutoSpectre;
using Dumpify;
using FileBasedApp.Toolkit;
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Image;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using Playground;
using SkiaSharp;
using Spectre.Console;

var connector = new Connector().Prompt();
var jevRunner = connector.GetRunner();

var imagePath = Environment.SpecialFolder.UserProfile.GetSpecialFolder() / "Pictures" / "Billede1.jpg";

var jevQuery = new JevQuery((await new FileData("Some.txt", "bla bla bla").AddImageAsset("photo", "person_image",imagePath)));
var request = jevQuery.GetRequest();
request.AddQuestion(new BoxQuestionBuilder("Find_Head", "Find the head in `Photo`"));

var result = await jevRunner.Execute(request);
var requiredBoxAnswer = result.GetRequiredBoxAnswer("Find_Head");
var rendered = new BoxRenderer().DrawBox(imagePath, requiredBoxAnswer);

var outputPng = imagePath / ".." / "output.png";
await outputPng.WriteAllBytesAsync(rendered, CancellationToken.None);
requiredBoxAnswer.Dump();


return;

public class FileData : AssetState
{
    public string File { get; }
    public string Extension { get; }

    public FileData(string file, string extension)
    {
        File = file;
        Extension = extension;
    }
}


/// <summary>Provides instructions and optional yes/no criteria for evaluating file content.</summary>
public class QuestionDefinition : INoulQuestionDefinition<string[]>
{
    public QuestionDefinition(string[] instructions)
    {
        Instructions = instructions;
        Yes = "Hello";
    }

    public string[] Instructions { get; set; }
    public string? Yes { get; }
    public string? No { get; } = null;
}


/// <summary>Provides instructions and ordered choices for classifying file content.</summary>
public class ChoiceQuestionDefinition : IChoiceQuestionDefinition<string>
{
    public ChoiceQuestionDefinition(string instructions, params ChoiceCriteria[] choices)
    {
        Instructions = instructions;
        Choices = choices;
    }

    public string Instructions { get; }
    public ChoiceCriteria[] Choices { get; }
}

/// <summary>Provides instructions and criteria for scoring the supplied file's information quality.</summary>
public class ScoreQuestionDefinition : IScoreQuestionDefinition<string>
{
    public ScoreQuestionDefinition(string instructions, params string[] criterias)
    {
        Instructions = instructions;
        Criterias = criterias;
    }

    public string Instructions { get; }
    public string[] Criterias { get; }
}

[JevQuery]
public partial class JevQuery
{
    public JevQuery(FileData state)
    {
        State = state;
        JevModel = JevModel.Preview;
        HasBeard = new GenericNoulQuestionDefinition<string>("`photo` has a person with a beard in it", null,null);
        
        ChoiceQuestionDefinition = new ChoiceQuestionDefinition(
            "Classify the text in the supplied file.",
            new ChoiceCriteria("Placeholder", "The text is filler or placeholder content, such as 'bla bla bla'."),
            new ChoiceCriteria("Meaningful",
                "The text conveys meaningful information rather than placeholder content."));
        ScoreQuestionDefinition = new ScoreQuestionDefinition(
            "Score the information quality of the text in the supplied file.",
            "The text conveys meaningful information rather than filler or placeholder content.",
            "The text is clear and understandable.",
            "The text provides specific, useful details.");
    }
    
    private FileData State { get;  }
    private JevModel JevModel { get; }

    public INoulQuestionDefinition<string> HasBeard { get;  }
    public ChoiceQuestionDefinition ChoiceQuestionDefinition { get;  }
    public ScoreQuestionDefinition ScoreQuestionDefinition { get;  }
}
