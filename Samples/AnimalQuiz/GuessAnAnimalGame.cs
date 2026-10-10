using JRK.JevRunner;
using JRK.JevRunner.Responses;
using Spectre.Console;

namespace AnimalQuiz;

/// <summary>
/// Runs one console round in which the player uses yes-or-no questions to discover a hidden animal.
/// Jev evaluates each input against the animal's name instead of using a hard-coded list of animal facts.
/// </summary>
/// <param name="name">The hidden animal's English name, supplied as context to Jev.</param>
/// <param name="runner">The configured client used to send questions to Jev.</param>
public class GuessAnAnimalGame(string name, JevRunner runner)
{
    /// <summary>
    /// Prompts the player, classifies their input, and displays Jev's answer until they guess correctly or give up.
    /// Waits for a key press at the end so the result remains visible before the next round.
    /// </summary>
    public async Task Run()
    {
        Noul animalGuessed = new Noul(0.1);
        AnsiConsole.Write(new FigletText("Want to play a game?"));
        
        while (!animalGuessed.Success)
        {
            var guess = await AnsiConsole.PromptAsync(new TextPrompt<string>("Ask a question or make a guess"));
            // One request carries both the hidden animal and the player's input, with several judgments to evaluate.
            var guessQuery = new GuessQuery(new GuessQueryState(name, guess));
            var response = await runner.Execute(guessQuery.GetRequest());
            // Generated answer helpers read each named judgment from the same response.
            var questionTypeAnswer = guessQuery.GetQuestionTypeAnswer(response);
            var guessQuestionType = guessQuery.QuestionType.GetQuestionType(questionTypeAnswer.Choice);


            if (guessQuestionType is GuessQuestionType.NotAQuestion)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]The Text must be formulated as a question[/]");
                continue;
            }

            if (guessQuestionType is GuessQuestionType.OtherQuestion)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]The question must be a yes or no question[/]");
                continue;
            }

            if (guessQuestionType is GuessQuestionType.UserGivesUp)
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]Good game. It was {name} [/]");
                break;
            }

            var questionIsCorrect = guessQuery.GetAskedQuestionIsCorrectAnswer(response);
            
            if (guessQuestionType is GuessQuestionType.DirectQuestion)
            {
                if (questionIsCorrect.Noul == true)
                {
                    AnsiConsole.MarkupLineInterpolated($"[green]Correct. The animal was {name} [/]");
                    break;
                }
                else
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]That is not the animal I'm thinking off [/]");
                    continue;
                }
            }

            // Show both the yes/no decision and its underlying numeric value for this sample.
            AnsiConsole.MarkupLineInterpolated($"[dim]The answer is: {questionIsCorrect.Noul.Success} ({questionIsCorrect.Noul.Value})[/]");
            
        }

        Console.ReadKey();
    }
}
