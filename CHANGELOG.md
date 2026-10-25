# Changelog

## [Unreleased]

### Added

- `JevPrice` represents decimal prices per million input and output tokens, with the output rate defaulting to zero. `Usage.CalculateCost(price)` calculates the combined cost at those rates.
- `Usage.Zero` provides zero token counts. `Usage.Add(other)` returns independent input and output totals without changing either operand. `IEnumerable<Usage>.SumUsage()` aggregates usages and returns zero for an empty sequence.
- Unit coverage for usage pricing, addition, and aggregation, including zero values, independent token categories, counts beyond the 32-bit range, deferred sequences, and unchanged operands.

### Changed

- **Breaking:** `Usage` is now a `readonly record struct` rather than a record class. It has value semantics and a zero-valued default. Update callers that rely on null, reference identity, or class constraints; use `Usage?` if an absent value is needed.
- Source-generated query `Execute` methods now accept `IJevRunner` instead of the concrete `JevRunner`, allowing interface-typed runners and alternative implementations while preserving cancellation-token forwarding and typed response wrappers.
- Package version prefix is now `0.6.0`, with `dev-02` for Debug builds and `preview-01` for non-Debug builds.

### Fixed

- Source-generator tests now include `IJevRunner` in their independent runtime contract and verify execution through an interface-typed runner, including the generated signature, request and cancellation-token forwarding, and response wrapping.
- Corrected a typo in the typed response lookup test documentation.

## [0.5.0]

### Added

- `ImageAsset.FromByteArray(name, format, bytes)` creates an asset with standard padded base64 without line breaks or a data URI prefix. The supplied format must match the bytes; no conversion or validation is performed.
- `ImageValidationResult` exposes typed `ImageDimensionError` or string errors, `SuggestedScaling`, and `ThrowIfInvalid()`. `ImageValidationError.GetErrorMessage()` formats either error case; `ImageDimensionError.GetErrors()` checks both axes against inclusive custom bounds.
- Result-level `SuggestedScaling` reports `Upscale` for undersized dimensions, `Downscale` for oversized dimensions, `ScalingNotPossible` when both kinds occur, and `None` when there are no dimension errors. Suggestions ignore non-dimension errors and do not guarantee that a proportional resize satisfies both bounds.
- Experimental `AssetState.AddImageAssetWithAutoAdjustment` in the image package validates a file, attempts one suggested resize, and registers its bytes. Unchanged files retain their detected format and bytes; resized images become PNG. All file access uses the supplied file system. Adjusted output is not revalidated.
- `JRK.JevRunner.Image.ImageScaler.Upscale` and `Downscale` proportionally scale from the shorter or longer side respectively, defaulting to 280 and 1568 pixels. Return PNG bytes without modifying the source or rejecting results whose other dimension remains outside API limits; custom positive bounds and injectable file access are supported.
- `JRK.JevRunner.Image.ImageValidator` checks actual PNG/JPEG contents, inclusive 280–1568-pixel axis bounds, the 16 MiB file limit, PNG animation, EXIF orientation, and pixel decoding without modifying the file. Returns detected properties and validation errors, with injectable file access. Request-wide limits remain separate.
- Source-generated Box questions through `IBoxQuestionDefinition<T>` and `GenericBoxQuestionDefinition<T>`, including typed answer methods and response wrapper properties.
- Runtime Box questions through `BoxQuestionBuilder` and normalized `BoxAnswer` coordinates with confidence, accessible through `Response.GetRequiredBoxAnswer`.
- Optional `JRK.JevRunner.Image` package with a SkiaSharp `BoxRenderer` that reads an image through TruePath and an injectable file system, draws normalized `BoxAnswer` outlines, and returns PNG bytes without modifying the source. Its `AppliedBox` union accepts one answer or an enumerable of answers with shared color and stroke width.
- Experimental image support through request assets, with base64-encoded image data in the top-level `assets` map. Requests containing assets automatically send the `TypeSafe-Danger-Zone-Images: true` header.
- `AssetState` stores named asset references and collects their assets when constructing a request. Its asynchronous `AddImageAsset` helper reads an image from a TruePath `AbsolutePath`, with optional `IFileSystem` support, and base64-encodes its bytes.
- `JevKnownException.Content` exposes the API error response content through the nullable `ErrorContent` union.
- Known-error handling for HTTP 400, 413, 415, and 529, while retaining existing 509 handling.
- `AnswerExtensions` helpers `OnlyNoul()`, `OnlyChoice()`, `OnlyScoreAnswer()`, and `OnlyBoxAnswer()` filter answer sequences by type and return typed results.

### Changed

- Playground now uses automatic image adjustment for generated head and mustache queries, prints all named answers, and renders box answers with confidence of at least 0.65 without explicitly selecting a model.
- Image rendering uses preview C# union support and accepts multiple boxes in a single call.
- Error translation now asynchronously reads the HTTP response body. Tests have been updated for the asynchronous translator and expanded status-code coverage.
- Added XML documentation for asset state and image-file helpers, and README examples for image requests and API error handling.

### Fixed

- Requests constructed without an explicit model now re-evaluate model selection immediately before execution, so Box questions added after construction select `JevModel.Preview`; requests without Box questions use `JevModel.Latest`. Explicitly supplied models remain unchanged.
- Known HTTP exception messages retain the failure description and append any returned error content.
- Plain-text and malformed JSON error bodies now remain available as raw `ErrorContent` strings instead of throwing a JSON parsing exception and masking the HTTP failure.
- Rejecting a duplicate asset state field no longer replaces the previously registered asset.
- Updated the request-envelope test for the assets map and added regression coverage for image assets, file loading, generated image queries, request-scoped image headers, and error response content.

## [0.4.0]

### Added

- Source generator warning `JEV006` for value-tuple `State` and question instruction types in `[JevQuery]` queries. The warning explains that tuple element names are not preserved in JSON and recommends a record or anonymous object when meaningful names are needed.

### Fixed

- `JevMessage.Node` now serializes top-level value tuples using `JsonSerializerOptions.IncludeFields = true`, preserving their values instead of producing an empty JSON object (`{}`). This also applies to tuple state forwarded by generated `[JevQuery]` requests.
- Tuple fields use runtime names such as `Item1` and `Item2`; C# tuple element names are not preserved. Nested value tuples within a top-level value tuple are also serialized with these options. Other message types retain the default serialization options.
