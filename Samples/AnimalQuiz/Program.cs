using System.IO.Abstractions;
using System.Text.Json.Nodes;
using AnimalQuiz;
using FileBasedApp.Toolkit;
using FileBasedApp.Toolkit.CommandCli;
using JRK.JevRunner;
using Spectre.Console;
using Spectre.Console.Cli;

// Spectre.Console.Cli handles command-line parsing and validation before starting the quiz.
var commandApp = new CommandApp<RunCommand>().WithDescription("Enter the description here");
commandApp.Configure(ctx =>
{
    ctx.UseAssemblyInformationalVersion();
});
return await commandApp.RunAsync(args);
namespace AnimalQuiz
{
    /// <summary>
    /// Starts the sample by loading animal names, selecting animals at random, and running a quiz round for each selection.
    /// </summary>
    public class RunCommand : AsyncCommand<RunCommand.Settings> // For sync only you can use Command (and have Execute instead of ExecuteAsync)
    {
        /// <summary>
        /// Reads the bundled English/Danish animal list and uses each selected animal's English name as the hidden answer.
        /// Reuses the configured Jev client across all rounds.
        /// </summary>
        /// <returns>Zero when all selected rounds have finished.</returns>
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            var commonAnimalsJson = PathUtil.GetExecutionFolder() / "common-animals.json";
            List<(string english, string danish)> animals = new();
        
            // The project copies this data beside the executable, so it can be read independently of the working directory.
            await using (var fileSystemStream = commonAnimalsJson.OpenRead())
            {
                var result = await JsonNode.ParseAsync(fileSystemStream, cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Failed to parse JSON");
                foreach (var jsonNode in result.AsArray())
                {
                    var obj = jsonNode?.AsObject();
                    if (obj is null)
                        continue;
                
                    animals.Add((obj["english"]!.GetValue<string>(), obj["danish"]!.GetValue<string>()));
                }
            }

            Random random = new Random();
            // GetItems samples with replacement: animals can appear more than once rather than forming a shuffled deck.
            var randomItems = random.GetItems(animals.ToArray(), animals.Count);

            while (randomItems.Length > 0)
            {
                var animal = randomItems[0];
                randomItems = randomItems[1..];
                AnsiConsole.Console.Clear();

                if (settings.CheatMode)
                {
                    AnsiConsole.MarkupLineInterpolated($"[bold]{animal.english}[/]");    
                }

                var guessAnAnimalGame = new GuessAnAnimalGame(animal.english, settings.Runner);
                await guessAnAnimalGame.Run();
            }
        
            return 0; // 0 for success
        }

        /// <summary>Accepts the Jev API key and prepares the client before the quiz command runs.</summary>
        public class Settings : ExtendedCommandSettings
        {
            /// <summary>Gets or sets the API key supplied with <c>--api-key</c>, falling back to <c>JEV_TOKEN</c>.</summary>
            [CommandOption("--api-key", false)] public string JevApiKey { get; set; } = string.Empty;
            
            
            [CommandOption("--cheat-mode")]
            public bool CheatMode { get; set; }
            
            
            /// <summary>Gets or sets the Jev client initialized during settings validation and shared by the quiz rounds.</summary>
            public JevRunner Runner { get; set; } = null!;

            /// <summary>
            /// Uses the command-line API key when present, otherwise reads <c>JEV_TOKEN</c>, then initializes the Jev client.
            /// </summary>
            /// <exception cref="InvalidOperationException">Neither the option nor the environment supplies an API key.</exception>
            protected override ValidationResult DoValidate()
            {
                if (string.IsNullOrWhiteSpace(JevApiKey))
                {
                    var token = Environment.GetEnvironmentVariable("JEV_TOKEN");
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        JevApiKey = token;
                    }
                    else
                    {
                        throw new InvalidOperationException("No Api key defined or found as environment variable");
                    }
                }
            
                Runner = JevRunner.Init(JevApiKey);
                return base.DoValidate();
            }
        }
    }
}
