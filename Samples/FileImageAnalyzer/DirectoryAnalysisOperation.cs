using System.IO.Abstractions;
using FileBasedApp.Toolkit;
using JRK.JevRunner;
using JRK.JevRunner.Image;
using SkiaSharp;
using Spectre.Console;
using TruePath;

namespace FileImageAnalyzer;

/// <summary>Analyzes images in a directory using Jev and displays the results and token costs.</summary>
public class DirectoryAnalysisOperation
{
    private readonly JevRunner _runner;
    private readonly AbsolutePath _path;
    private readonly IFileSystem _fileSystem;

    /// <summary>Creates an image analysis operation for the specified directory using the local file system.</summary>
    public DirectoryAnalysisOperation(JevRunner runner, AbsolutePath path) : this(runner, path, new FileSystem())
    {
    }

    /// <summary>Creates an image analysis operation using the supplied runner, directory, and file system.</summary>
    public DirectoryAnalysisOperation(JevRunner runner, AbsolutePath path, IFileSystem fileSystem)
    {
        _runner = runner;
        _path = path;
        _fileSystem = fileSystem;
    }

    /// <summary>Processes JPEG and PNG files recursively, displays each result, and reports total token usage and cost.</summary>
    /// <param name="maxFiles">The maximum number of matching files to process.</param>
    /// <param name="filesToSkip">The number of matching files to skip before processing.</param>
    /// <remarks>Waits for a key press after each successfully analyzed image and continues past individual file failures.</remarks>
    public async Task Analyse(int maxFiles = 20, int filesToSkip = 0)
    {
        // The price per 1 millions tokens
        JevPrice price = new JevPrice(0.042m);
        
        HashSet<string> types =  [with(StringComparer.OrdinalIgnoreCase),"JPG", "JPEG", "PNG"];
        List<Usage> usages = new();
        
        foreach (var file in _path.EnumerateAllFiles("*.*", _fileSystem).Where(x => types.Contains(x.GetExtensionWithoutDot()))
                     .Skip(filesToSkip)
                     .Take(maxFiles))
        {
            AnsiConsole.MarkupLineInterpolated($"[green]Processing file: {file}[/]");

            try
            {
                PhotoAssetState state = new PhotoAssetState(file);
                
                // The file is evaluated and added to the state. Using the PhotoAssetName. The
                // Photo is used in questions in the ImageQuery as a reference to search in
                // If you wanted to you modify the PhotoAssetState to do the file evaluation for you
                await state.AddImageAssetWithAutoAdjustment(PhotoAssetState.PhotoAssetName, "File", file);
                
                // Initialize the Image query and execute it
                var query = new ImageQuery(state);
                var result = await query.Execute(_runner);
              
                
                
                // Get the answers for the questions

                AnsiConsole.MarkupLineInterpolated($"[dim]Has pet {result.HasPet.Noul.Value}[/]");
                AnsiConsole.MarkupLineInterpolated($"[dim]Has child {result.HasChild.Noul.Value}[/]");
                AnsiConsole.MarkupLineInterpolated($"[dim]Is Screenshot {result.IsScreenShot.Noul.Value}[/]");

                // Example of getting the registered image as Bytes
                var imageAsBytes = state.GetRequiredImageAsset(PhotoAssetState.PhotoAssetName).FileData.Bytes;
                
                
                if (result.HasChild.Noul.Value > 0.65)
                {
                    imageAsBytes = new BoxRenderer().DrawBox(file, result.ChildBox, SKColors.Black, 20f);
                }
                
                // Render the image in the console. 
                // Note that this uses SixLabors.ImageSharp which requires a license for 
                // commercial use. 
                var image = new CanvasImage(imageAsBytes)
                    .MaxWidth(225);
                AnsiConsole.Write(image);
                
                
                AnsiConsole.MarkupLineInterpolated($"[dim]Tokens {result.Response.Usage.InputTokens}-{result.Response.Usage.OutputTokens} {result.Response.Usage.CalculateCost(price)}[/]");
                usages.Add(result.Response.Usage);
                
                AnsiConsole.Write("Press a key to continue");
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

        AnsiConsole.MarkupLineInterpolated($"[green][bold]Total[/]Tokens {sumUsage.InputTokens}-{sumUsage.OutputTokens} {sumUsage.CalculateCost(price)}[/]");
        
    }
    
}