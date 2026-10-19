# Release notes

Track changes for the current release here as they are made. This log starts here;
earlier changes are intentionally not backfilled.

When publishing a release, replace `Unreleased` with its version and release date,
then add a new `Unreleased` section at the top. Remove empty categories from
published release notes.

## Unreleased

### Added

- `ImageAsset.FromByteArray(name, format, bytes)` encodes bytes as standard padded
  base64 without line breaks or a `data:` prefix; callers supply the matching format.
- `ImageValidationResult` exposes typed dimension or string errors, `SuggestedScaling`,
  and `ThrowIfInvalid()`. Errors expose `GetErrorMessage()`; `ImageDimensionError.GetErrors()`
  checks both dimensions against inclusive custom bounds.
- Result-level scaling suggestions group dimension violations: undersized dimensions
  suggest upscaling, oversized dimensions suggest downscaling, and mixed violations
  report `ScalingNotPossible`. With no dimension errors the suggestion is `None`,
  even if other validation errors exist. Suggestions do not guarantee valid output.
- Experimental `AssetState.AddImageAssetWithAutoAdjustment` validates, attempts one
  suggested resize, and registers the result. Unchanged files retain their format
  and bytes; resized images become PNG. File access consistently uses the supplied
  file system. Scaled output is not revalidated; see the image README for limitations.
- Image package `ImageScaler.Upscale` and `Downscale` use the shorter/longer side
  to proportionally reach the minimum/maximum bound (280/1568 by default), returning
  PNG bytes without changing the source. Results still outside the other API
  dimension limit are returned without validation errors.
- Optional image package `ImageValidator` for per-file PNG/JPEG validation,
  dimensions, file size, animation, EXIF orientation, and decoding checks, returning
  detected properties and errors without modifying the file. Request-wide limits
  still require separate validation.
- Experimental image requests using base64-encoded assets and automatic
  `TypeSafe-Danger-Zone-Images` opt-in. `AssetState.AddImageAsset` loads images
  asynchronously through TruePath with optional injectable file access.
- Runtime and source-generated Box questions, normalized `BoxAnswer` coordinates
  with confidence, and typed answer accessors and response wrapper properties.
- Optional `JRK.JevRunner.Image` package with a SkiaSharp `BoxRenderer` that draws
  one or multiple boxes and returns PNG bytes without modifying the source image.
- `AnswerExtensions` filtering helpers: `OnlyNoul()`, `OnlyChoice()`,
  `OnlyScoreAnswer()`, and `OnlyBoxAnswer()` return answers of the selected type.
- API error response content through `JevKnownException.Content`, plus known-error
  handling for HTTP 400, 413, 415, and 529 alongside existing 509 support.
- FileGrouping command-line sample for classifying files with generated Jev
  choice queries, moving them into category folders, and reporting token usage
  and request counts. Files classified as `Indeterminate` are left in place.
- FileGrouping run instructions and file-move safety guidance in the samples
  README, with a link from the main README.

### Changed

- Playground demonstrates automatic image adjustment and head/mustache detection,
  prints all named answers, and renders box answers with confidence of at least
  0.65, relying on automatic model selection instead of an explicit preview model.
- Image rendering accepts multiple boxes through the preview C# `AppliedBox` union.
- Error translation reads response bodies asynchronously. Documentation and
  regression coverage have been expanded for image requests and API errors.

### Fixed

- Requests without an explicit model re-evaluate model selection before execution:
  Box questions added after construction select `JevModel.Preview`; requests without
  Box questions use `JevModel.Latest`. Explicit model choices are preserved.
- HTTP exception messages preserve the failure description and returned error
  content. Plain-text and malformed JSON bodies remain available as raw content
  instead of masking the HTTP failure with a JSON parsing exception.
- Duplicate asset state fields no longer replace previously registered assets
  before throwing.

## 0.3.1

## Changed

- Removed unnecessary Jetbrains.Annotation dependency

## 0.3.0

### Added

- Generated `Execute` methods for annotated queries, forwarding the generated
  request and optional cancellation token to `JevRunner` and returning
  `Task<{QueryName}ResponseWrapper>` with typed question properties and the original
  `.Response`.
- Generated `{QueryName}ResponseWrapper` classes that expose the original
  `Response` and getter-only, typed answer properties for each supported question.
  These properties reuse the query's required-answer lookup logic, including
  errors for missing answers or mismatched answer types. Wrappers support nested
  and generic queries and escaped question names.
- Source-generator test coverage for query execution, cancellation-token
  forwarding, response wrappers, all supported answer types, live answer lookups,
  lookup failures, and nested generic queries.

### Changed

- Generated `query.Execute(runner, cancellationToken)` now returns a response
  wrapper instead of a raw `Response`. After awaiting it, use the wrapper's named
  question properties for typed answers, or `.Response` where a raw `Response`
  is required, including calls to existing `Get{QuestionName}Answer(Response)`
  accessors. `GetRequest()` remains available for manual execution.

### Fixed

- Generated wrapper-returning `Execute` methods preserve generic query type
  arguments and avoid naming their response local after a containing type parameter.

### Removed

<!-- Removed features or APIs, including migration guidance when needed. -->
