# JRK.JevRunner.Analyzers

The main `JRK.JevRunner` package depends on this package, so NuGet installs the
analyzer and code fix automatically. Only this package contains their DLLs; the
core and Image packages do not embed duplicate copies. They are compile-time assets,
not runtime assemblies. An explicit reference is optional:

```sh
dotnet add package JRK.JevRunner.Analyzers
```

The package contains compile-time analyzers, not runtime assemblies. The analyzer and code fix
are packaged together under `analyzers/dotnet/cs/`, in separate assemblies so the
command-line analyzer does not depend on IDE-only Workspaces APIs (RS1038).
There are two production projects and one shared test project.

## JEV101: Remove redundant backticks

Warns when an `IBackTick` argument in a `FormattableString` or `IFormattable`
interpolation has explicit backticks immediately on both sides. JevRunner adds
its own backticks when formatting the argument, so the explicit pair duplicates
the quoting:

```csharp
FormattableString incorrect = $"Describe `{field}`."; // JEV101
FormattableString correct = $"Describe {field}.";
```

The **Remove redundant backticks** code fix removes just the adjacent pair,
preserving the argument, surrounding text, and interpolation type. Normal,
verbatim, and raw strings are supported, as are Unicode-escaped backticks.
Fix All is supported. Plain string targets still report JEV100 instead.

Repository projects using project references need explicit analyzer references;
Playground and JRK.JevRunner.Image include these. Image NuGet consumers receive
the tooling through the core package dependency; Image does not bundle it again.

## JEV100: Preserve backtick quoting

Warns on each statically typed `JRK.JevRunner.IBackTick` argument (including
`StateReference`, `StatePath`, derived types, and constrained type parameters)
in an interpolation that becomes a plain string:

```csharp
StateReference field = new("name");
string incorrect = $"Describe {field}."; // JEV100
FormattableString instructions = $"Describe {field}."; // Preserves arguments
// Pass instructions to JevRunner, or use instructions.Escaped to format it.
```

`FormattableString` and `IFormattable` targets are allowed. Preserving the arguments
does not itself quote them; JevRunner's formatting path must still process them.
Raw and verbatim interpolations are supported. Values hidden behind `object` or
`dynamic`, explicit `ToString()` calls, composite `string.Format` calls, and custom
interpolated-string handlers are outside this initial rule's scope.

The **Use Escape.Interpolated** code fix changes the entire interpolation
to `Escape.Interpolated($"Describe {field}.")`, adding the JevRunner
namespace import if needed. This retains a string result while applying quoting.
Fix All is supported. The fix is offered when the compatible Escape.Interpolated helper is
available in the compilation. The fix starts with a root-qualified helper call,
then uses semantic simplification to produce the short name where safe. Namespace
or global qualification is retained when needed to avoid shadowed names such as
Escape or JRK.

Generic constraints are followed transitively, including class constraints whose
base classes or interfaces implement the marker. This applies to both rules.

The rule is a warning by default. Configure or disable it in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.JEV100.severity = none
```
