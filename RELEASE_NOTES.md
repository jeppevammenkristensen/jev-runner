# Release notes

Track changes for the current release here as they are made. This log starts here;
earlier changes are intentionally not backfilled.

When publishing a release, replace `Unreleased` with its version and release date,
then add a new `Unreleased` section at the top. Remove empty categories from
published release notes.

## Unreleased

### Added

- Generated `Execute` methods for annotated queries, forwarding the generated
  request and cancellation token to `JevRunner` and returning its `Response`.
- Generated `{QueryName}ResponseWrapper` classes that expose the original
  `Response` and getter-only, typed answer properties for each supported question.
  These properties reuse the query's required-answer lookup logic, including
  errors for missing answers or mismatched answer types. Wrappers support nested
  and generic queries and escaped question names.
- Source-generator test coverage for query execution, cancellation-token
  forwarding, response wrappers, all supported answer types, live answer lookups,
  lookup failures, and nested generic queries.

### Changed

<!-- Changes to existing behavior or APIs. Note breaking changes explicitly. -->

### Fixed

<!-- Bug fixes. -->

### Removed

<!-- Removed features or APIs, including migration guidance when needed. -->
