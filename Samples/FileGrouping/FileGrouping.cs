using System.Collections.Concurrent;
using Spectre.Console.Cli;
using TruePath;
using Spectre.Console;
using FileBasedApp.Toolkit;
using System.IO.Abstractions;
using FileBasedApp.Toolkit.CommandCli;
using JetBrains.Annotations;
using JRK.JevRunner;
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;
using JRK.JevRunner.Responses;

var commandApp = new CommandApp<RunCommand>().WithDescription("Runs through files in a given directory and groups them into a concrete folder");
commandApp.Configure(ctx =>
{
    ctx.UseAssemblyInformationalVersion();
});
return await commandApp.RunAsync(args);

public record FileGroup(ChoiceAnswer Group, AbsolutePath Path);

public class RunCommand : AsyncCommand<RunCommand.Settings> // For sync only you can use Command (and have Execute instead of ExecuteAsync)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var folderToProcess = settings.DirectoryPath;
        
        var runner = JevRunner.Init(Environment.GetEnvironmentVariable("JEV_TOKEN")!);
        
        ConcurrentBag<FileGroup> fileGroups = [];
        ConcurrentBag<Usage> usages = [];

        var files = folderToProcess.GetFiles();

        await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
            .StartAsync("Classifying files...", async ctx =>
            {
                await Parallel.ForEachAsync(files, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                }, async (item, c) =>
                {
                    FileQuery query = new FileQuery(new FileState(item));
                    var result = await query.Execute(runner, c);
                    ctx.Status($"Processed {item.GetFilenameWithoutExtension().EscapeMarkup()}");
                    fileGroups.Add(new(result.Groups, item));
                    usages.Add(result.Response.Usage);
                });
            });
        
        
        
        foreach (var grouping in fileGroups.GroupBy(x => x.Group.Choice))
        {
            AnsiConsole.MarkupLineInterpolated($"[green bold]{grouping.Key}[/]");

            bool isIndeterminate = grouping.Key == "Indeterminate";
            
            var groupDirectory = folderToProcess / grouping.Key;
            groupDirectory.CreateDirectory();

            foreach (var fileGroup in grouping.Select(x => x.Path).OrderBy(x => x.FileName))
            {
                AnsiConsole.MarkupLineInterpolated($"[dim]{fileGroup.FileName}[/]");

                if (!isIndeterminate)
                {
                    fileGroup.FileMove(groupDirectory /  fileGroup.FileName);    
                }
                
            }
        }

        var inputTokens = usages.Sum(s => s.InputTokens);
        var outputTokens = usages.Sum(s => s.OutputTokens);
        var requests = usages.Count;

        AnsiConsole.MarkupLineInterpolated($"[green] [bold]InputTokens[/] {inputTokens} [bold]OutputToken[/] {outputTokens} [bold]Requests[/] {requests} [/]");
        
        return 0; // 0 for success
    }

    public class Settings : ExtendedCommandSettings
    {
        [CommandArgument(0, "<PATH>")]
        public string? Directory { get; set; }

        protected override ValidationResult DoValidate()
        {
            if (!string.IsNullOrWhiteSpace(Directory))
            {
                var path = new LocalPath(Directory);
                path = path.ExpandHomeDirectory();

                DirectoryPath = this.TryGetDirectory(path.Value, false, true, PredefinedRootPath.CurrentDirectory);
            }
            else
            {
                DirectoryPath = TryGetDirectory(Directory, false, true, PredefinedRootPath.ExecutionFolder);    
            }
            
            return base.DoValidate();
        }

        public AbsolutePath DirectoryPath { get; set; }
    }
}


public class FileState(AbsolutePath path)
{
    private readonly IFileInfo _fileInfo = path.NewFileInfo();

    [UsedImplicitly]
    public string FileExtension => path.GetExtensionWithoutDot();
    
    [UsedImplicitly]
    public string FileName => path.GetFilenameWithoutExtension();

    [UsedImplicitly]
    public DateTime Created => _fileInfo.CreationTime;
    [UsedImplicitly]
    public DateTime Modified => _fileInfo.LastWriteTime;
    [UsedImplicitly]
    public DateTime Now => DateTime.Now;
}

[JevQuery]
public partial class FileQuery
{
    public FileQuery(FileState state)
    {
        State = state;
    }

    public FileState State { get; }
    public JevModel JevModel { get; } = JevModel.Latest;

    public GenericChoiceQuestionDefinition<string> Groups { get; } = new(
        "Analyse the FileExtension and FileName to match the best fitting group",
        new ChoiceCriteria("Installer", "The file seems to be an installer"),
        new ChoiceCriteria("Video", "The file is a video"),
        new ChoiceCriteria("Music", $"The file is a music file"),
        new ChoiceCriteria("Images",  $"The file is an image file"),
        new ChoiceCriteria("Script", "The file is a script"),
        new ChoiceCriteria("Archive", "The files is an archive, (zippable)"),
        new ChoiceCriteria("Receipt", "The file appears to be a reciept/confirmation"),
        new ChoiceCriteria("DatabaseRelated", "Files related to development. Database files (dacpac, backup, "),
        new ChoiceCriteria("DataFiles", "Json, xml etc"),
        new ChoiceCriteria("CodingRelated", "wasm, html, cs etc"),
        new ChoiceCriteria("Email files", "File dumps of emails"),
        new ChoiceCriteria("Spreadsheet", "This is a spreadsheet"),
        new ChoiceCriteria("Document", "The file is document. Like word or pdf"),
        new ChoiceCriteria("Indeterminate", "The files does not match the already defined choices"));

}
