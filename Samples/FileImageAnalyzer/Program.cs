using FileImageAnalyzer;
using Spectre.Console.Cli;

var commandApp = new CommandApp<RunCommand>().WithDescription("Enter the description here");
commandApp.Configure(ctx =>
{
    ctx.UseAssemblyInformationalVersion();
});
return await commandApp.RunAsync(args);