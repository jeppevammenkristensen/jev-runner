using System.ComponentModel;
using FileBasedApp.Toolkit;
using FileBasedApp.Toolkit.CommandCli;
using JetBrains.Annotations;
using JRK.JevRunner;
using Spectre.Console;
using Spectre.Console.Cli;
using TruePath;

namespace FileImageAnalyzer;

/// <summary>Runs interactive image analysis for the selected directory.</summary> 
[UsedImplicitly]
public sealed class RunCommand : AsyncCommand<RunCommand.Settings> // For sync only you can use Command (and have Execute instead of ExecuteAsync)
    {
        /// <summary>
        /// Analyzes images in the selected directory using the configured Jev client.
        /// </summary>
        /// <returns>Zero when directory analysis has finished.</returns>
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            await new DirectoryAnalysisOperation(settings.Runner, settings.DirectoryPath)
                .Analyse(settings.MaxItems, settings.Skip);
            return 0; // 0 for success
        }

        /// <summary>Accepts the API key and directory and prepares the Jev client before image analysis runs.</summary>
        public class Settings : ExtendedCommandSettings
        {
            /// <summary>Gets or sets the API key supplied with <c>--api-key</c>, falling back to <c>JEV_TOKEN</c>.</summary>
            [CommandOption("--api-key")]
            private string JevApiKey { get; set; } = string.Empty;
            
            /// <summary>Gets or sets the optional directory path supplied on the command line.</summary>
            [CommandArgument(0, "[Path]")]
            public string? Directory { get; set; }

            /// <summary>Gets or sets the maximum number of matching images to analyze.</summary>
            [CommandOption("--max-items <COUNT>")]
            [DefaultValue(20)]
            [Description("Maximum number of images to analyze.")]
            public int MaxItems { get; set; } = 20;

            /// <summary>Gets or sets the number of matching images to skip before analysis.</summary>
            [CommandOption("--skip <COUNT>")]
            [DefaultValue(0)]
            [Description("Number of images to skip before analysis.")]
            public int Skip { get; set; }
            
            /// <summary>Gets the directory resolved during settings validation, defaulting to the current working directory.</summary>
            public AbsolutePath DirectoryPath { get; private set; }
            
            
            /// <summary>Gets or sets the Jev client initialized during settings validation and used for image analysis.</summary>
            public JevRunner Runner { get; set; } = null!;

            /// <summary>
            /// Resolves the API key from the command line or <c>JEV_TOKEN</c>, initializes the Jev client, and resolves the directory.
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
                        throw new InvalidOperationException("No Api key defined or found as environment variable (JEV_TOKEN)");
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
