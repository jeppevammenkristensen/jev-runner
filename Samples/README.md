# Samples

Runnable examples of using JRK.JevRunner with Jev and the TypeSafe System One API.
Open [Samples.slnx](Samples.slnx) in your IDE, or run a sample from the command line.
The samples include AnimalQuiz, BlackJack (Halvtolv), FileGrouping, and FileImageAnalyzer.

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

## BlackJack (Halvtolv)

[BlackJack](BlackJack/Readme.md) is an interactive console game of Halvtolv, a
Danish blackjack variant where the target is **11½ points**. Play against a
Jev-controlled dealer that chooses whether to stop or draw using the game rules,
visible cards, and previous round statistics. Your first card is hidden from the
dealer while it makes its decisions.

The sample demonstrates:

- Passing structured game state and round history to Jev.
- Defining stop/continue choices with `GenericChoiceQuestionDefinition<string>`.
- Using generated typed choice responses to drive the dealer's turn.
- Making decisions with incomplete information about the player's hand.

### Run from the command line

Run these commands **from the repository root**. In PowerShell:

```powershell
$env:JEV_TOKEN = "<your-typesafe-api-key>"
dotnet run --project Samples/BlackJack/BlackJack.csproj
```

For Bash or a compatible shell:

```bash
export JEV_TOKEN="<your-typesafe-api-key>"
dotnet run --project Samples/BlackJack/BlackJack.csproj
```

BlackJack reads **`JEV_TOKEN`**, not `TYPESAFE_API_KEY`, and does not provide an
`--api-key` option. Do not commit your key to source control.

### Run from an IDE

1. Open [Samples.slnx](Samples.slnx).
2. Select `BlackJack` as the startup/run project.
3. Set `JEV_TOKEN` in the run configuration's environment variables.
4. Run the project in an interactive console.

### Explore the code

- [Program.cs](BlackJack/Program.cs): API-key configuration and starting the game.
- [Game.cs](BlackJack/Game.cs): interactive rounds, dealer decisions, and outcomes.
- [DealerQuery.cs](BlackJack/DealerQuery.cs): the stop/continue query definition.
- [DealerState.cs](BlackJack/DealerState.cs): rules, round history, and current hands
  supplied to Jev.
- [Readme.md](BlackJack/Readme.md): game rules and background.

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

## FileImageAnalyzer

[FileImageAnalyzer](FileImageAnalyzer/) is an interactive console sample that
recursively finds JPEG and PNG files in a directory and asks Jev whether each image
contains a pet or child, or is a screenshot. It displays the numeric Noul answers
and the image in the terminal. When the child answer exceeds `0.65`, it draws a
black bounding box using the query's child Box answer. Press a key after each
successfully analyzed image to continue; individual file failures are reported
without stopping the remaining analysis.

**This sample uploads image content to the live TypeSafe API.** Only use images
you have permission to share, especially photos of children. It does not move or
overwrite source files. The image API and automatic image adjustment are
experimental; see the [image package documentation](../JRK.JevRunner.Image/README.md)
for limitations and native dependencies. Terminal image rendering uses
SixLabors.ImageSharp; review its license requirements before commercial use.

The sample demonstrates:

- Preparing an image asset with `AddImageAssetWithAutoAdjustment` and a custom
  `AssetState` before executing a generated `[JevQuery]`.
- Combining Noul judgments and a Box question with generated typed answers.
- Drawing a bounding box with `BoxRenderer` and displaying images with
  Spectre.Console.
- Reporting per-image and total token usage and estimated cost using a hard-coded
  `JevPrice`, rather than fetching current API pricing.

### Run from the command line

Run these commands **from the repository root**, replacing the path with the
directory containing your images. In PowerShell:

```powershell
$env:JEV_TOKEN = "<your-typesafe-api-key>"
dotnet run --project Samples/FileImageAnalyzer/FileImageAnalyzer.csproj -- "C:\path\to\images" --max-items 5 --skip 0
```

For Bash or a compatible shell:

```bash
export JEV_TOKEN="<your-typesafe-api-key>"
dotnet run --project Samples/FileImageAnalyzer/FileImageAnalyzer.csproj -- "/path/to/images" --max-items 5 --skip 0
```

The directory argument is optional and defaults to the current working directory.
`--max-items` defaults to `20`; `--skip` defaults to `0` and skips matching images
before applying the maximum. Enumeration is not explicitly sorted, so these
options do not guarantee stable paging between runs.

FileImageAnalyzer reads **`JEV_TOKEN`**, not `TYPESAFE_API_KEY`. Alternatively,
supply `--api-key "<your-typesafe-api-key>"`, but be aware that command-line
arguments may be recorded in shell history or visible to other processes. Do not
commit your key to source control.

To see the command-line help:

```shell
dotnet run --project Samples/FileImageAnalyzer/FileImageAnalyzer.csproj -- --help
```

### Run from an IDE

1. Open [Samples.slnx](Samples.slnx).
2. Select `FileImageAnalyzer` as the startup/run project.
3. Set `JEV_TOKEN` in the run configuration's environment variables, or supply
   `--api-key "<your-typesafe-api-key>"` as a program argument.
4. Supply the image directory as a program argument, quoting paths with spaces,
   and optionally add `--max-items` and `--skip`.
5. Run the project in an interactive console.

### Explore the code

- [Program.cs](FileImageAnalyzer/Program.cs): console command setup.
- [RunCommand.cs](FileImageAnalyzer/RunCommand.cs): API-key and directory
  configuration, `--max-items`, and `--skip`.
- [DirectoryAnalysisOperation.cs](FileImageAnalyzer/DirectoryAnalysisOperation.cs):
  recursive enumeration, image preparation, query execution, rendering, and usage totals.
- [PhotoAssetState.cs](FileImageAnalyzer/PhotoAssetState.cs): the image asset state.
- [ImageQuery.cs](FileImageAnalyzer/ImageQuery.cs): pet, child, screenshot, and
  child bounding-box question definitions.
