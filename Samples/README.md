# Samples

Runnable examples of using JRK.JevRunner with Jev and the TypeSafe System One API.
Open [Samples.slnx](Samples.slnx) in your IDE, or run a sample from the command line.
There is currently one sample.

## Prerequisites

- The .NET 11 SDK (currently in preview).
- A TypeSafe API key and internet access. The sample sends requests to the live API.

The sample references the published `JRK.JevRunner` NuGet package, so you do not need
to build the client library separately. `dotnet run` restores dependencies and builds
the sample automatically.

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
