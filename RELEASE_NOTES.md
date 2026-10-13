# Release notes

Track changes for the current release here as they are made. This log starts here;
earlier changes are intentionally not backfilled.

When publishing a release, replace `Unreleased` with its version and release date,
then add a new `Unreleased` section at the top. Remove empty categories from
published release notes.

## Unreleased

### Added

- FileGrouping command-line sample for classifying files with generated Jev
  choice queries, moving them into category folders, and reporting token usage
  and request counts. Files classified as `Indeterminate` are left in place.
- FileGrouping run instructions and file-move safety guidance in the samples
  README, with a link from the main README.

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
