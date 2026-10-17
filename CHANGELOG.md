# Changelog

## [0.4.0]

### Added

- Source generator warning `JEV006` for value-tuple `State` and question instruction types in `[JevQuery]` queries. The warning explains that tuple element names are not preserved in JSON and recommends a record or anonymous object when meaningful names are needed.

### Fixed

- `JevMessage.Node` now serializes top-level value tuples using `JsonSerializerOptions.IncludeFields = true`, preserving their values instead of producing an empty JSON object (`{}`). This also applies to tuple state forwarded by generated `[JevQuery]` requests.
- Tuple fields use runtime names such as `Item1` and `Item2`; C# tuple element names are not preserved. Nested value tuples within a top-level value tuple are also serialized with these options. Other message types retain the default serialization options.
