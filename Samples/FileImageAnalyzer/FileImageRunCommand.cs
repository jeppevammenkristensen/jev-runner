using System.IO.Abstractions;
using System.Text.Json.Nodes;
using FileBasedApp.Toolkit;
using FileBasedApp.Toolkit.CommandCli;
using JRK.JevRunner;
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Image;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using Spectre.Console;
using Spectre.Console.Cli;
using TruePath;

namespace FileImageAnalyzer;

public class DirectoryAnalysisOperation
{
    private readonly JevRunner _runner;
    private readonly AbsolutePath _path;
    private readonly IFileSystem _fileSystem;

    public DirectoryAnalysisOperation(JevRunner runner, AbsolutePath path) : this(runner, path, new FileSystem())
    {
    }

    public DirectoryAnalysisOperation(JevRunner runner, AbsolutePath path, IFileSystem fileSystem)
    {
        _runner = runner;
        _path = path;
        _fileSystem = fileSystem;
    }

    public async Task Analyse(int maxFiles = 20)
    {
        // The price per 1 millions tokens
        JevPrice price = new JevPrice(0.042m);
        
        HashSet<string> types =  [with(StringComparer.OrdinalIgnoreCase),"JPG", "JPEG", "PNG"];
        List<Usage> usages = new();
        
        foreach (var file in _path.EnumerateAllFiles("*.*", _fileSystem).Where(x => types.Contains(x.GetExtensionWithoutDot())).Take(maxFiles))
        {
            AnsiConsole.MarkupLineInterpolated($"[green]Processing file: {file}[/]");

            try
            {
                AssetState state = new AssetState();
                await state.AddImageAssetWithAutoAdjustment("Photo", "File", file);
                var query = new ImageQuery(state);
                var result = await query.Execute(_runner);


                if (state.GetRequiredImageAsset("Photo") is not ImageAsset img)
                {
                    
                }
                var image = new CanvasImage(state.GetRequiredImageAsset("Photo").FileData.Bytes).MaxWidth(50);
                AnsiConsole.Write(image);

                AnsiConsole.MarkupLineInterpolated($"[dim]Has pet {result.HasPet.Noul.Value}[/]");
                AnsiConsole.MarkupLineInterpolated($"[dim]Has child {result.HasChild.Noul.Value}[/]");
                
                AnsiConsole.MarkupLineInterpolated($"[dim]Tokens {result.Response.Usage.InputTokens}-{result.Response.Usage.OutputTokens} {result.Response.Usage.CalculateCost(price)}[/]");
                usages.Add(result.Response.Usage);
                
                AnsiConsole.Write("Write a key to continue");
                Console.ReadKey();

            }
            catch (Exception e)
            {
                AnsiConsole.WriteLine($"[red]Failed to process file: {file}[/]");
                AnsiConsole.WriteException(e);
            }
        }
        
        AnsiConsole.Write(new Rule());
        var sumUsage = usages.SumUsage();

        AnsiConsole.MarkupLineInterpolated($"[green]Tokens {sumUsage.InputTokens}-{sumUsage.OutputTokens} {sumUsage.CalculateCost(price)}[/]");
        
    }
    
}

[JevQuery]
public partial class ImageQuery
{
    public ImageQuery(AssetState state)
    {
        State = state;
    }

    public AssetState State { get; set; }

    public GenericNoulQuestionDefinition<string> HasPet =>
        new GenericNoulQuestionDefinition<string>("`Photo` has a Pet in the image", null, null);
    
    public GenericNoulQuestionDefinition<string> HasChild =>
        new GenericNoulQuestionDefinition<string>("`Photo` has a Child in the image", null, null);
    
}

public class RunCommand : AsyncCommand<RunCommand.Settings> // For sync only you can use Command (and have Execute instead of ExecuteAsync)
    {
        /// <summary>
        /// Reads the bundled English/Danish animal list and uses each selected animal's English name as the hidden answer.
        /// Reuses the configured Jev client across all rounds.
        /// </summary>
        /// <returns>Zero when all selected rounds have finished.</returns>
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            await new DirectoryAnalysisOperation(settings.Runner, settings.DirectoryPath).Analyse();
            return 0; // 0 for success
        }

        /// <summary>Accepts the Jev API key and prepares the client before the quiz command runs.</summary>
        public class Settings : ExtendedCommandSettings
        {
            /// <summary>Gets or sets the API key supplied with <c>--api-key</c>, falling back to <c>JEV_TOKEN</c>.</summary>
            [CommandOption("--api-key", false)] public string JevApiKey { get; set; } = string.Empty;
            
            [CommandArgument(0, "[Path]")]
            public string? Directory { get; set; }
            
            public AbsolutePath DirectoryPath { get; private set; }
            
            
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

                if (!string.IsNullOrWhiteSpace(Directory))
                {
                    DirectoryPath = TryGetDirectory(new LocalPath(Directory).ExpandHomeDirectory().Value, false, true);
                }
                else
                {
                    DirectoryPath = TryGetDirectory(null,true, true, PathUtil.GetCurrentWorkingFolder());
                }
                
                return base.DoValidate();
            }
        }
    }