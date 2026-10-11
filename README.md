# JRK.JevRunner

[![NuGet](https://img.shields.io/nuget/v/JRK.JevRunner.svg)](https://www.nuget.org/packages/JRK.JevRunner)

An unofficial .NET client for Jev and the TypeSafe System One API. Submit application state and named questions, and consume typed responses.

## Requirements

- .NET 11 or later
- A TypeSafe API key

## Installation

```shell
dotnet add package JRK.JevRunner
```

The package includes its source generators automatically; no separate generator package or
analyzer reference is needed when consuming it through NuGet. The generator assembly targets
`netstandard2.0` for compiler-host compatibility, while the client targets `net11.0`.

For repository development, analyzer project references are not transitive. See the sample
project's explicit analyzer reference. Build and pack the client with:

```shell
dotnet pack JRK.JevRunner/JRK.JevRunner.csproj -c Release
dotnet build JRK.JevRunner.SourceGenerators/JRK.JevRunner.SourceGenerators.Sample/JRK.JevRunner.SourceGenerators.Sample.csproj -c Release -p:UsePackedJevRunner=true
```

The second command is a build-only packaging smoke check: it consumes the locally packed
NuGet without an explicit generator reference and compiles code that requires generated types.

Package restores audit both direct and transitive dependencies and fail on known vulnerabilities
or audit-feed failures. To repeat the advisory check:

```shell
dotnet restore all.slnx --force-evaluate
dotnet package list --project all.slnx --vulnerable --include-transitive --no-restore
```

## Quick start

Set the `TYPESAFE_API_KEY` environment variable to your API key. Do not commit API keys to source control.

```csharp
using JRK.JevRunner;
using JRK.JevRunner.Requests;

var apiKey = Environment.GetEnvironmentVariable("TYPESAFE_API_KEY")
    ?? throw new InvalidOperationException("Set TYPESAFE_API_KEY before running.");

var runner = JevRunner.Init(apiKey);
var request = new Request("The brown fox jumped over the lazy dog.", JevModel.Latest)
    .AddQuestion(new NoulQuestionBuilder(
        "dog_was_jumped_over",
        "Did any animal jump over a lazy dog?"));

var response = await runner.Execute(request);
Console.WriteLine(response.GetRequiredNoulAnswer("dog_was_jumped_over"));
```

## Samples

See the [samples and run instructions](Samples/README.md) for runnable examples,
starting with the interactive AnimalQuiz console game.

## Generated queries

Alternatively you can mark a partial class or record class with `[JevQuery]` and supply readable `State`
and `JevModel` properties. Note that State can be a string, array or any object.  Question properties implement one of
`INoulQuestionDefinition<TInstruction>`, `IChoiceQuestionDefinition<TInstruction>`,
or `IScoreQuestionDefinition<TInstruction>` from `JRK.JevRunner.Annotation`.
The generator adds `GetRequest()`, `Execute(runner, cancellationToken)`, and named
typed answer accessors such as `GetBudgetAnswer(Response)`. Generated `Execute`
returns `Task<{QueryName}ResponseWrapper>`. The wrapper exposes getter-only typed
answer properties named after the query's questions and the original `.Response`.

For example, using the `runner` initialized in the quick start:

```csharp
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;

var query = new AnimalQuery();
var result = await query.Execute(runner);
Console.WriteLine(result.DogJumpedOver);
var response = result.Response; // Original Response for direct runtime API access.

[JevQuery]
public partial class AnimalQuery
{
    public string State => "The brown fox jumped over the lazy dog.";
    public JevModel JevModel => global::JRK.JevRunner.Requests.JevModel.Latest;
    public JumpQuestion DogJumpedOver { get; } = new();
}

public class JumpQuestion : INoulQuestionDefinition<string>
{
    public string Instructions => "Did any animal jump over a lazy dog?";
    public string? Yes => "An animal jumped over a lazy dog.";
    public string? No => "No animal jumped over a lazy dog.";
}
```

`Execute(runner, cancellationToken)` builds the request and forwards the optional
cancellation token to the runner. In this example it returns an
`AnimalQueryResponseWrapper` with a `NoulAnswer` property named `DogJumpedOver`.
`GetRequest()` and `GetDogJumpedOverAnswer(Response)` remain available for manual
execution and answer lookup. Wrapper answer properties throw if an answer is
missing or has a mismatched type.
Criteria are generated only when both `Yes` and `No` are non-null; otherwise
criteria remain unset.

Question attributes have been replaced by definition interfaces. Choice definitions
provide `ChoiceCriteria[] Choices`; score definitions provide `string[] Criterias`
and now have only the instruction generic parameter. Noul definitions provide
optional `Yes` and `No` descriptions: both must be non-null to generate criteria.
If either is null, the runtime omits the JSON `criteria` member.

See the [generator contract and diagnostics](JRK.JevRunner.SourceGenerators/JRK.JevRunner.SourceGenerators/Readme.md)
and the [mixed-query sample](JRK.JevRunner.SourceGenerators/JRK.JevRunner.SourceGenerators.Sample/Examples.cs).

This package is targeting C# 15 and dotnet sdk which is currently in the time of writing in preview

## License

MIT License. Copyright (c) 2026 Jeppe Roi Kristensen.
