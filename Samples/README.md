# Samples

Runnable examples of using JRK.JevRunner with Jev and the TypeSafe System One API.
Open [Samples.slnx](Samples.slnx) in your IDE, or run a sample from the command line.
The samples include AnimalQuiz and FileGrouping.

## Prerequisites

- The .NET 11 SDK (currently in preview).
- A TypeSafe API key and internet access. The samples send requests to the live API.

The samples reference the published `JRK.JevRunner` NuGet package, so you do not need
to build the client library separately. `dotnet run` restores dependencies and builds
the selected sample automatically.

## AnimalQuiz

[AnimalQuiz](AnimalQuiz/) is an interactive console game. It picks a hidden animal
from the bundled animal list, and you identify it by asking yes-or-no questions such
as "Does it live in water?" or making a direct guess such as "Is it a dolphin?".
You can also give up to reveal the answer. Press a key after a round to move on to
the next animal, or use Ctrl+C to stop the application.

The sample demonstrates:

- Passing structured application state (the hidden animal and your input) to Jev.
- Using a `[JevQuery]` class to generate requests and typed answer helpers.
- Combining choice-based input classification and Noul yes/no judgments in one request.
- Displaying both the yes/no decision and its underlying numeric value.

### Run from the command line

Run these commands **from the repository root**. In PowerShell, set the API key for
the current terminal session and start the game:

```powershell
$env:JEV_TOKEN = "<your-typesafe-api-key>"
dotnet run --project Samples/AnimalQuiz/AnimalQuiz.csproj
```

For Bash or a compatible shell:

```bash
export JEV_TOKEN="<your-typesafe-api-key>"
dotnet run --project Samples/AnimalQuiz/AnimalQuiz.csproj
```

Unlike the root README's quick-start example, this sample reads **`JEV_TOKEN`**, not
`TYPESAFE_API_KEY`. Do not commit your key to source control.

Alternatively, pass the key explicitly (be aware that command-line arguments may
be recorded in shell history or visible to other processes):

```shell
dotnet run --project Samples/AnimalQuiz/AnimalQuiz.csproj -- --api-key "<your-typesafe-api-key>"
```

To reveal the hidden animal at the start of each round, enable cheat mode:

```shell
dotnet run --project Samples/AnimalQuiz/AnimalQuiz.csproj -- --cheat-mode
```

To see the available command-line options:

```shell
dotnet run --project Samples/AnimalQuiz/AnimalQuiz.csproj -- --help
```

### Run from an IDE

1. Open [Samples.slnx](Samples.slnx).
2. Select `AnimalQuiz` as the startup/run project.
3. Set `JEV_TOKEN` in the run configuration's environment variables, or supply
   `--api-key "<your-typesafe-api-key>"` as a program argument.
4. Run the project in an interactive console.

### Explore the code

- [Program.cs](AnimalQuiz/Program.cs): command-line options, API-key configuration,
  loading the animal list, and starting rounds.
- [GuessAnAnimalGame.cs](AnimalQuiz/GuessAnAnimalGame.cs): the interactive game loop
  and handling Jev's responses.
- [GuessQuery.cs](AnimalQuiz/GuessQuery.cs): the generated-query definition that
  groups the judgments for each turn.
- [GuessQueryState.cs](AnimalQuiz/GuessQueryState.cs): the shared state sent to Jev.

## FileGrouping

[FileGrouping](FileGrouping/) is a command-line sample that classifies files in a
directory using their names, extensions, and timestamps as application state. Its
choice question asks Jev to select a category such as Installer, Video, Music,
Images, Script, Archive, Receipt, or Document based on the name and extension.
It does not read file contents or process subdirectories recursively.

**This sample moves files.** Run it on a disposable copy of a directory first.
Files are moved into category subfolders under the supplied directory; files
classified as `Indeterminate` stay in their original location (an empty
`Indeterminate` folder is still created). There is no dry-run option.

The sample demonstrates:

- Using a `[JevQuery]` with `GenericChoiceQuestionDefinition<string>` and typed
  choice answers to group files.
- Executing queries in parallel and displaying progress with Spectre.Console.
- Summing input tokens, output tokens, and request counts from response usage.

### Run from the command line

Run these commands **from the repository root**, replacing the path with the
directory you want to organize. In PowerShell:

```powershell
$env:JEV_TOKEN = "<your-typesafe-api-key>"
dotnet run --project Samples/FileGrouping/FileGrouping.csproj -- "C:\path\to\files-copy"
```

For Bash or a compatible shell:

```bash
export JEV_TOKEN="<your-typesafe-api-key>"
dotnet run --project Samples/FileGrouping/FileGrouping.csproj -- "/path/to/files-copy"
```

FileGrouping reads **`JEV_TOKEN`**, not `TYPESAFE_API_KEY`, and does not provide an
`--api-key` option. Do not commit your key to source control.

To see the command-line help:

```shell
dotnet run --project Samples/FileGrouping/FileGrouping.csproj -- --help
```

### Run from an IDE

1. Open [Samples.slnx](Samples.slnx).
2. Select `FileGrouping` as the startup/run project.
3. Set `JEV_TOKEN` in the run configuration's environment variables.
4. Supply the directory path as a program argument, quoting paths with spaces.

### Explore the code

- [FileGrouping.cs](FileGrouping/FileGrouping.cs): command-line setup, directory
  validation, parallel classification, file moves, usage totals, and the
  `FileState` and `FileQuery` definitions.
- [FileGrouping.csproj](FileGrouping/FileGrouping.csproj): the console application
  and its published package references.
