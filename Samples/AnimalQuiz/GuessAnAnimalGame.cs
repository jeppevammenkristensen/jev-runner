using JRK.JevRunner;
using Spectre.Console;

namespace AnimalQuiz;

/// <summary>
/// Runs one console round using factual yes-or-no questions and positive animal identity guesses.
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
        AnsiConsole.Write(new FigletText("Want to play a game?"));
        AnsiConsole.MarkupLine("Ask one factual yes/no question, or guess an animal (for example, 'Dolphin').");
        AnsiConsole.MarkupLine("Use the specific animal name; synonyms and translations count. Type 'I give up' to reveal it.");
        
        while (true)
        {
            var guess = await AnsiConsole.PromptAsync(new TextPrompt<string>("Ask a question or make a guess"));
            // One request carries both the hidden animal and the player's input, with several judgments to evaluate.
            var guessQuery = new GuessQuery(new GuessQueryState(name, guess));
            var response = await guessQuery.Execute(runner);
            // Generated answer helpers read each named judgment from the same response.
            var questionTypeAnswer = response.QuestionType;
            var guessQuestionType = guessQuery.QuestionType.GetQuestionType(questionTypeAnswer.Choice);

            if (guessQuestionType is GuessQuestionType.NotAQuestion)
            {
                AnsiConsole.MarkupLine("[red]Ask an animal question, name one animal, or type 'I give up'.[/]");
                continue;
            }

            if (guessQuestionType is GuessQuestionType.OtherQuestion)
            {
                AnsiConsole.MarkupLine("[red]Ask one factual yes/no question about a typical adult animal, or make one positive animal guess.[/]");
                continue;
            }

            if (guessQuestionType is GuessQuestionType.UserGivesUp)
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]Good game. It was {name} [/]");
                break;
            }

            if (guessQuestionType is GuessQuestionType.DirectQuestion)
            {
                var identityAnswer = response.UserGuessTheAnimal;
                var identified = AnswerPolicy.Evaluate(identityAnswer.Noul);
                if (identified is true)
                {
                    AnsiConsole.MarkupLineInterpolated($"[green]Correct. The animal was {name} [/]");
                    break;
                }
                else if (identified is false)
                {
                    AnsiConsole.MarkupLine("[red]That is not the specific animal I'm thinking of.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]I'm not sure that identifies the animal. Try a more specific name.[/]");
                }
                continue;
            }

            var questionIsCorrect = response.AskedQuestionIsCorrect;
            var answer = AnswerPolicy.Evaluate(questionIsCorrect.Noul);
            var answerText = answer switch
            {
                true => "Yes",
                false => "No",
                null => "I'm not sure; please rephrase the question"
            };
            AnsiConsole.MarkupLineInterpolated($"[dim]{answerText} (yes probability: {questionIsCorrect.Noul.Value})[/]");
            
        }

        Console.ReadKey();
    }
}
