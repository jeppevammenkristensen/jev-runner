using System.IO.Abstractions;
using AutoSpectre;
using Dumpify;
using FileBasedApp.Toolkit;
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Image;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using JRK.JevRunner.Responses;
using Playground;
using SkiaSharp;
using System.IO;
using JRK.JevRunner;
using Spectre.Console;
using TruePath;



var connector = new Connector().Prompt();
var jevRunner = connector.GetRunner();

var imagePath = Environment.SpecialFolder.UserProfile.GetSpecialFolder() / "Pictures" / "Billede1.jpg";

var jevQuery =
    new JevQuery((await new FileData("Some.txt", "bla bla bla").AddImageAssetWithAutoAdjustment(JevQuery.Photo, "person_image", imagePath)));
var response = await jevQuery.Execute(jevRunner);


foreach (var responseAnswer in response.Response.Answers)
{
    AnsiConsole.MarkupLineInterpolated($"[bold]{responseAnswer.Name}[/]");

    responseAnswer.Answer.Value.Dump();
}

var boxAnswers = response.Response.Answers.Select(x => x.Answer).OnlyBoxAnswer().Where(x => x.Confidence.Value >= 0.65);

var rendered = new BoxRenderer().DrawBox(imagePath, boxAnswers, SKColors.Green);

var outputPng = imagePath / ".." / "output3.png";
await outputPng.WriteAllBytesAsync(rendered, CancellationToken.None);

return;

public class FileData : AssetState
{
    public string File { get; set; }
    public string Extension { get; set; }

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
public class ScoreQuestionDefinition : IScoreQuestionDefinition<FormattableString>
{
    public ScoreQuestionDefinition(FormattableString instructions, params string[] criterias)
    {
        Instructions = instructions;
        Criterias = criterias;
    }

    public FormattableString Instructions { get; }
    public string[] Criterias { get; }
}

[JevQuery]
public partial class JevQuery
{
    public static readonly StateReference Photo = nameof(Photo);
    public static readonly StateReference SecondPhoto = nameof(SecondPhoto);
    
    public JevQuery(FileData state)
    {
        State = state;
        HasBeard = new GenericNoulQuestionDefinition<FormattableString>($"{Photo} has a person with a beard in it", null, null);
        FindPerson = new GenericBoxQuestionDefinition<FormattableString>($"Find the head in the {Photo}");
        FindMustache = new GenericBoxQuestionDefinition<FormattableString>($"Find the mustache in the {Photo}");

        ChoiceQuestionDefinition = new ChoiceQuestionDefinition(
            "Classify the text in the supplied file.",
            new ChoiceCriteria("Placeholder", "The text is filler or placeholder content, such as 'bla bla bla'."),
            new ChoiceCriteria("Meaningful",
                "The text conveys meaningful information rather than placeholder content."));
        ScoreQuestionDefinition = new ScoreQuestionDefinition(
            $"Score the information quality of {new StatePath(nameof(File))} the text in the supplied file.",
            "The text conveys meaningful information rather than filler or placeholder content.",
            "The text is clear and understandable.",
            "The text provides specific, useful details.");
    }

    public GenericBoxQuestionDefinition<FormattableString> FindMustache { get; set; }

    public GenericBoxQuestionDefinition<FormattableString> FindPerson { get; private set; }

    private FileData State { get; }
    public INoulQuestionDefinition<FormattableString> HasBeard { get; }
    public ChoiceQuestionDefinition ChoiceQuestionDefinition { get; }
    public ScoreQuestionDefinition ScoreQuestionDefinition { get; }
}