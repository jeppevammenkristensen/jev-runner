# JRK.JevRunner

[![JRK.JevRunner](https://img.shields.io/nuget/v/JRK.JevRunner.svg)](https://www.nuget.org/packages/JRK.JevRunner) [![JRK.JevRunner.Image](https://img.shields.io/nuget/v/JRK.JevRunner.Image.svg)](https://www.nuget.org/packages/JRK.JevRunner.Image)

An .NET client for Jev and the TypeSafe System One API. Submit application state and named questions, and consume typed responses.

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

## Models and request validation

Requests without an explicit model use `JevModel.Latest`, including requests with
Box questions. An explicitly supplied model is never overridden. Box questions
require `jev-1.14.0` or later; `JevModel.Preview` remains available as an explicit choice.

`JevVersions` provides `Latest`, `Preview`, `Version1_13_0`, and `Version1_14_0`
string constants and the known-version list `Versions`. Inspect a `JevModel` with
`IsLatest`, `IsPreview`, `IsKnownVersion`, or `IsVersion(version)`.

`Request.StrictValidation` defaults to `false`. Before sending a request, the
runner logs a warning for an unknown model identifier or for Box questions using
`jev-1.13.0`, but still sends the request. Set `StrictValidation = true` to throw
`InvalidOperationException` for either condition before any HTTP request:

```csharp
var request = new Request("Application state", JevModel.Latest)
{
    StrictValidation = true
};
```

The bundled known-version list may lag the API: an unknown identifier is not
necessarily unsupported by TypeSafe. These checks do not replace API validation
or guarantee that a request is valid.

## Long context (opt-in)

Set `request.LongContextEnabled = true` or call the chainable
`request.EnableLongContext()` extension from `JRK.JevRunner` to send
`TypeSafe-Danger-Zone-Long-Context: true` for that request. The default is disabled.

```csharp
var request = new Request("Application state", JevModel.Latest)
    .EnableLongContext()
    .AddQuestion(new NoulQuestionBuilder("ready", "Is the application ready?"));
```

This opts into context beyond TypeSafe's default **64 KB** limit. TypeSafe cautions
that longer context is **slower and less accurate** than staying within the default
limit; enable it only for requests that need it. Other API limits still apply.

## Images (experimental)

Jev's image API is **dreamer-ready**, not production-ready: request formats, limits,
and accuracy may change. Images can be used with Noul, Choice, Score, and Box
questions. Box answers contain normalized `Top`, `Bottom`, `Left`, and `Right`
edges and answer-level `Confidence`.

Use `AssetState` to associate a state field with an image. `AddImageAsset` reads the
file asynchronously, base64-encodes its bytes, and uses its extension as the image
format. It accepts an optional `IFileSystem` for file access.

Using the `runner` initialized in the quick start:

```csharp
using JRK.JevRunner;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using TruePath;

var state = new AssetState();
await state.AddImageAsset(
    "photo",
    Guid.NewGuid().ToString(),
    AbsolutePath.Create(Path.GetFullPath("photo.jpg")));

var request = new Request(state, JevModel.Latest)
    .AddQuestion(new NoulQuestionBuilder(
        "person_present",
        "Is there a person in `photo`?"));

var response = await runner.Execute(request);
Console.WriteLine(response.GetRequiredNoulAnswer("person_present").Noul);
```

Populate the state **before** constructing the request: its assets are collected
by the constructor. A generated `[JevQuery]` can likewise expose an `AssetState`
as its `State` property; populate it before calling `GetRequest()` or `Execute()`.

For manual configuration, add an `ImageAsset(reference, format, base64)` to
`request.Assets` and place the same reference in the state, instructions, or
criteria. Use unique references such as UUIDs to avoid accidental text matches.
Base64 must contain only the encoded bytes, without a `data:` prefix.

Requests containing assets automatically send
`TypeSafe-Danger-Zone-Images: true`. Images are sent in the JSON `assets` map,
not as multipart uploads. A filename or image URL alone does not upload an image.

Prepare PNG or JPEG images before sending: each dimension must be 280–1568 pixels,
each file at most 16 MiB, and the request body at most 32 MiB. Base64 increases the
body size. Rotate the pixels upright; the API does not apply EXIF orientation.
The helper does not resize, rotate, convert, or validate image data. Use a lowercase
`.png`, `.jpg`, or `.jpeg` extension that matches the file contents.
For an explicit per-file check before upload, use `ImageValidator` from the optional
[`JRK.JevRunner.Image` package](JRK.JevRunner.Image/README.md#validate-an-image-before-uploading). Or see [Validate and prepare images](#validate-and-prepare-images).
See the [TypeSafe image documentation](https://docs.typesafe.ai/images) for all limits.

### Validate and prepare images

The optional `JRK.JevRunner.Image` package provides `ImageValidator`, proportional
`ImageScaler.Upscale`/`Downscale`, and experimental automatic asset adjustment.
Validation returns typed dimension or string errors and a result-level
`SuggestedScaling`; call `ThrowIfInvalid()` to reject a failed validation.
Suggestions only describe dimension-error direction and do not guarantee that
resizing will make an image valid. See the [image preparation examples and limitations](JRK.JevRunner.Image/README.md).

To register already-prepared bytes without writing another file:

```csharp
byte[] imageBytes = new JRK.JevRunner.Image.ImageScaler().Downscale(
    AbsolutePath.Create(@"C:\images\photo.jpg"));
var state = new AssetState().AddAsset("photo",
    new ImageAsset(Guid.NewGuid().ToString(), "png", imageBytes));
```

`ImageScaler` returns PNG bytes. `ImageAsset` accepts them through `FileData`,
which exposes `Bytes` and `Base64`; the constructor does not validate or convert
the supplied format.

### Locate objects with Box questions

Using the populated `state` above:

```csharp
var request = new Request(state)
    .AddQuestion(new BoxQuestionBuilder("Head", "Find the head in `photo`."));
var response = await runner.Execute(request);
var head = response.GetRequiredBoxAnswer("Head");
Console.WriteLine($"Top: {head.Top}, left: {head.Left}, confidence: {head.Confidence.Value}");
```

Without an explicit model, execution selects `JevModel.Latest`, including for Box
questions added after construction. An explicitly supplied model is never overridden.
Box questions require `jev-1.14.0` or later; see [request validation](#models-and-request-validation).

For generated queries, expose a `GenericBoxQuestionDefinition<string>` property
or implement `IBoxQuestionDefinition<TInstruction>`. A question property named
`Head` generates `GetHeadAnswer(response)` and a `BoxAnswer` property named `Head`
on the response wrapper. See the [generator's Box example](JRK.JevRunner.SourceGenerators/JRK.JevRunner.SourceGenerators/Readme.md#box-questions).

### Render bounding boxes

The optional `JRK.JevRunner.Image` package provides a SkiaSharp `BoxRenderer`.
The core client does not depend on it. With the original image path and a Box answer:

```csharp
using JRK.JevRunner.Image;

var imagePath = AbsolutePath.Create(Path.GetFullPath("photo.jpg"));
byte[] png = new BoxRenderer().DrawBox(imagePath, head);
await File.WriteAllBytesAsync("annotated.png", png);
```

`DrawBox` accepts one `BoxAnswer` or an `IEnumerable<BoxAnswer>` through `AppliedBox`,
and returns PNG bytes without modifying the source. Multiple boxes share the supplied
color and stroke width. See the [image package documentation](JRK.JevRunner.Image/README.md)
for styling, coordinate validation, native dependencies, and multi-box examples.

## API errors

Known HTTP errors throw `JevKnownException` from `JRK.JevRunner.Infrastructure`.
Its `InnerException` retains the original `HttpRequestException`, including the
status code, and its nullable `Content` exposes the response body as an
`ErrorContent` union (`JsonNode` or `string`). Empty or whitespace-only bodies
produce null content.

```csharp
using JRK.JevRunner.Infrastructure;

try
{
    var response = await runner.Execute(request);
}
catch (JevKnownException error)
{
    Console.Error.WriteLine(error.Message);
    if (error.InnerException is HttpRequestException httpError)
        Console.Error.WriteLine($"HTTP status: {httpError.StatusCode}");
    if (error.Content is { } content)
        Console.Error.WriteLine(content.Value);
}
```

Handled status codes are **400, 401, 413, 415, 422, 429, and 529**, with **509**
retained for compatibility. Other status codes pass through as HTTP exceptions
regardless of whether their response body is JSON or plain text. Transport failures pass through without
an HTTP response. The client does not automatically retry rate-limit or overload
responses.

Non-empty error bodies are parsed as JSON when possible. Plain text, malformed
JSON, and JSON null are retained as raw strings rather than masking the HTTP
failure with a JSON parsing exception.

See the [TypeSafe error-code documentation](https://docs.typesafe.ai/api#errors).

## Samples

See the [samples and run instructions](Samples/README.md) for runnable examples,
including:

- [AnimalQuiz](Samples/README.md#animalquiz): identify a hidden animal through
  yes-or-no questions in an interactive console game.
- [BlackJack (Halvtolv)](Samples/BlackJack/Readme.md): play a Danish blackjack
  variant against a Jev-controlled dealer that reasons about visible cards and
  round history to decide whether to stop or draw.
- [FileGrouping](Samples/README.md#filegrouping): classify files with Jev and move
  them into category folders.
- [FileImageAnalyzer](Samples/README.md#fileimageanalyzer): analyze local photos for
  pets, children, and screenshots, and display images with a child bounding box.

## Generated queries

Alternatively you can mark a partial class or record class with `[JevQuery]` and supply readable `State`
and `JevModel` properties. Note that State can be a string, array or any object.  Question properties implement one of
`INoulQuestionDefinition<TInstruction>`, `IChoiceQuestionDefinition<TInstruction>`,
`IScoreQuestionDefinition<TInstruction>`, or `IBoxQuestionDefinition<TInstruction>` from `JRK.JevRunner.Annotation`.
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

## Dependency notes

This library uses [`TruePath.TestableIO.System.IO`](https://github.com/jeppevammenkristensen/TruePath.TestableIO.System.IO),
which builds on [`TruePath`](https://github.com/shimat/TruePath) and
[`TestableIO.System.IO`](https://github.com/TestableIO/System.IO.Abstractions).

## License

MIT License. Copyright (c) 2026 Jeppe Roi Kristensen.
