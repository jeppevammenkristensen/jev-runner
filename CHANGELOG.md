# Changelog

## [Unreleased]

### Added

- Experimental image support through request assets, with base64-encoded image data in the top-level `assets` map. Requests containing assets automatically send the `TypeSafe-Danger-Zone-Images: true` header.
- `AssetState` stores named asset references and collects their assets when constructing a request. Its asynchronous `AddImageAsset` helper reads an image from a TruePath `AbsolutePath`, with optional `IFileSystem` support, and base64-encodes its bytes.
- `JevKnownException.Content` exposes the API error response content through the nullable `ErrorContent` union.
- Known-error handling for HTTP 400, 413, 415, and 529, while retaining existing 509 handling.

### Changed

- Error translation now asynchronously reads the HTTP response body. Tests have been updated for the asynchronous translator and expanded status-code coverage.
- Added XML documentation for asset state and image-file helpers, and README examples for image requests and API error handling.

### Fixed

- Plain-text and malformed JSON error bodies now remain available as raw `ErrorContent` strings instead of throwing a JSON parsing exception and masking the HTTP failure.
- Rejecting a duplicate asset state field no longer replaces the previously registered asset.
- Updated the request-envelope test for the assets map and added regression coverage for image assets, file loading, generated image queries, request-scoped image headers, and error response content.

## [0.4.0]

### Added

- Source generator warning `JEV006` for value-tuple `State` and question instruction types in `[JevQuery]` queries. The warning explains that tuple element names are not preserved in JSON and recommends a record or anonymous object when meaningful names are needed.

### Fixed

- `JevMessage.Node` now serializes top-level value tuples using `JsonSerializerOptions.IncludeFields = true`, preserving their values instead of producing an empty JSON object (`{}`). This also applies to tuple state forwarded by generated `[JevQuery]` requests.
- Tuple fields use runtime names such as `Item1` and `Item2`; C# tuple element names are not preserved. Nested value tuples within a top-level value tuple are also serialized with these options. Other message types retain the default serialization options.
