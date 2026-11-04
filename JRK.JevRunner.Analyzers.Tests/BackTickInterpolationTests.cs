using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JRK.JevRunner.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace JRK.JevRunner.Analyzers.Tests;

/// <summary>Verifies lost-quoting diagnostics and the string-preserving formatting fix.</summary>
public class BackTickInterpolationTests
{
    private const string Contract = """
        namespace JRK.JevRunner
        {
            internal interface IBackTick { }
            public class StateReference : IBackTick { public string Value => "name"; }
            public class StatePath : IBackTick { }
            public class DerivedReference : StateReference { }
            public static class Escape
            {
                public static string Interpolated(System.FormattableString value) => value.Escaped;
            }
            public static class FormattableStringExtensions
            {
                extension(System.FormattableString value)
                {
                    public string Escaped => value.ToString();
                }
            }
        }
        """;

    [Theory]
    [InlineData("string text = $\"Describe {reference}.\";", 1)]
    [InlineData("var text = $\"Describe {reference}.\";", 1)]
    [InlineData("object text = $\"Describe {reference}.\";", 1)]
    [InlineData("string text = $@\"Describe {reference}.\";", 1)]
    [InlineData("string text = $$\"\"\"Describe {{reference}}.\"\"\";", 1)]
    [InlineData("string text = $\"{reference,20} {path} {derived}\";", 3)]
    [InlineData("System.FormattableString text = $\"Describe {reference}.\";", 0)]
    [InlineData("System.IFormattable text = $\"Describe {reference}.\";", 0)]
    [InlineData("string text = $\"{42} {reference.ToString()} {reference is null}\";", 0)]
    [InlineData("object hidden = reference; string text = $\"{hidden}\";", 0)]
    [InlineData("Consume($\"Describe {reference}.\");", 1)]
    [InlineData("Preserve($\"Describe {reference}.\");", 0)]
    public async Task ReportsOnlyPlainStringInterpolations(string statement, int count)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Source(statement));
        var diagnostics = await Analyze(document);
        Assert.Equal(count, diagnostics.Length);
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal("JEV100", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("Escape.Interpolated", diagnostic.GetMessage());
        });
    }

    [Fact]
    public async Task RecognizesMarkerAndGenericConstraints()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Contract + """
            class C
            {
                string Direct(JRK.JevRunner.IBackTick value) => $"{value}";
                string Generic<T>(T value) where T : JRK.JevRunner.StateReference => $"{value}";
            }
            """);
        Assert.Equal(2, (await Analyze(document)).Length);
    }

    [Theory]
    [InlineData("System.FormattableString text = $\"`{reference}`\";", 1)]
    [InlineData("System.IFormattable text = $\"Describe `{path}`.\";", 1)]
    [InlineData("Preserve($\"`{derived}`\");", 1)]
    [InlineData("Preserve($@\"`{reference}`\");", 1)]
    [InlineData("Preserve($$\"\"\"`{{reference}}`\"\"\");", 1)]
    [InlineData("Preserve($\"`{reference,20}` `{path}`\");", 2)]
    [InlineData("Preserve($\"\\u0060{reference}\\U00000060\");", 1)]
    [InlineData("Preserve($\"{reference}\");", 0)]
    [InlineData("Preserve($\"`{42}`\");", 0)]
    [InlineData("Preserve($\"`{reference.Value}`\");", 0)]
    [InlineData("Preserve($\"`{reference}\");", 0)]
    [InlineData("Preserve($\"{reference}`\");", 0)]
    [InlineData("Preserve($\"` {reference} `\");", 0)]
    public async Task ReportsRedundantBackticksOnlyForPreservedMarkerArguments(string statement, int count)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Source(statement));
        var diagnostics = await Analyze(document);
        Assert.Equal(count, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("JEV101", diagnostic.Id));
    }

    [Fact]
    public async Task ExplicitBackticksInPlainStringStillReportOnlyLostQuoting()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Source("string text = $\"`{reference}`\";"));
        Assert.Equal("JEV100", Assert.Single(await Analyze(document)).Id);
    }

    [Theory]
    [InlineData("Preserve($\"Describe `{reference}`.\");", "Preserve($\"Describe {reference}.\");")]
    [InlineData("Preserve($@\"`{reference}`\");", "Preserve($@\"{reference}\");")]
    [InlineData("Preserve($$\"\"\"`{{path}}`\"\"\");", "Preserve($$\"\"\"{{path}}\"\"\");")]
    [InlineData("Preserve($\"`{reference,20}`\");", "Preserve($\"{reference,20}\");")]
    [InlineData("Preserve($\"\\u0060{reference}\\U00000060\");", "Preserve($\"{reference}\");")]
    public async Task FixRemovesOnlyTheAdjacentBackticks(string statement, string expected)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Source(statement));
        var diagnostic = Assert.Single(await Analyze(document));
        var actions = new List<CodeAction>();
        await new BackTickInterpolationCodeFixProvider().RegisterCodeFixesAsync(
            new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action),
                TestContext.Current.CancellationToken));
        var action = Assert.Single(actions);
        Assert.Equal("Remove redundant backticks", action.Title);
        var operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
        var solution = Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution;
        var fixedDocument = solution.GetDocument(document.Id)!;
        Assert.Equal(Source(expected), (await fixedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString());
        Assert.Empty(await Analyze(fixedDocument));
    }

    [Fact]
    public async Task IgnoresUnrelatedInterfaceWithSameShortName()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, """
            interface IBackTick { }
            class Reference : IBackTick { }
            class C { string M(Reference value) => $"{value}"; }
            """);
        Assert.Empty(await Analyze(document));
    }

    [Theory]
    [InlineData("string text = $\"Describe {reference}.\";")]
    [InlineData("string text = $\"{reference} {path}\";")]
    [InlineData("string text = $$\"\"\"Describe {{reference}}.\"\"\";")]
    [InlineData("Consume($\"Describe {reference}.\");")]
    public async Task FixFormatsEntireInterpolationAndStillCompiles(string statement)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Source(statement));
        var diagnostics = await Analyze(document);
        var actions = new List<CodeAction>();
        var provider = new BackTickInterpolationCodeFixProvider();
        await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostics[0],
            (action, _) => actions.Add(action), TestContext.Current.CancellationToken));
        var action = Assert.Single(actions);
        Assert.Equal("Use Escape.Interpolated", action.Title);
        var operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
        var change = Assert.Single(operations.OfType<ApplyChangesOperation>());
        var fixedDocument = change.ChangedSolution.GetDocument(document.Id)!;
        var text = (await fixedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        Assert.Contains("Escape.Interpolated(", text);
        Assert.DoesNotContain("global::JRK.JevRunner", text);
        var fixedRoot = (await fixedDocument.GetSyntaxRootAsync(TestContext.Current.CancellationToken))!;
        Assert.Contains(fixedRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>(),
            invocation => invocation.Expression.ToString() == "Escape.Interpolated");
        Assert.DoesNotContain("((FormattableString)", text);
        Assert.Empty(await Analyze(fixedDocument));
        Assert.NotNull(provider.GetFixAllProvider());
    }

    [Theory]
    [InlineData("")]
    [InlineData("public static class FormattableStringExtensions { }")]
    [InlineData("public static class Escape { public static string Interpolated(string value) => value; }")]
    [InlineData("public static class Escape { public static object Interpolated(System.FormattableString value) => value; }")]
    public async Task DoesNotOfferFixWithoutInterpolationHelper(string helper)
    {
        using var workspace = new AdhocWorkspace();
        var source = """
            namespace JRK.JevRunner { internal interface IBackTick { } public class StateReference : IBackTick { } }
            class C { string M(JRK.JevRunner.StateReference value) => $"{value}"; }
            """;
        var document = CreateDocument(workspace, source + "namespace JRK.JevRunner { " + helper + " }");
        var diagnostics = await Analyze(document);
        var actions = new List<CodeAction>();
        await new BackTickInterpolationCodeFixProvider().RegisterCodeFixesAsync(
            new CodeFixContext(document, Assert.Single(diagnostics), (action, _) => actions.Add(action),
                TestContext.Current.CancellationToken));
        Assert.Empty(actions);
    }

    [Fact]
    public async Task FixBindsToJevRunnerHelperEvenWhenEscapeIsShadowed()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Source("""
            string Escape = "local";
            string text = $"Describe {reference}.";
            """));
        var diagnostic = Assert.Single(await Analyze(document));
        var actions = new List<CodeAction>();
        await new BackTickInterpolationCodeFixProvider().RegisterCodeFixesAsync(
            new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action),
                TestContext.Current.CancellationToken));
        var operations = await Assert.Single(actions).GetOperationsAsync(TestContext.Current.CancellationToken);
        var fixedDocument = Assert.Single(operations.OfType<ApplyChangesOperation>())
            .ChangedSolution.GetDocument(document.Id)!;
        Assert.Empty(await Analyze(fixedDocument));
        var root = (await fixedDocument.GetSyntaxRootAsync(TestContext.Current.CancellationToken))!;
        var invocation = Assert.Single(root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>(),
            node => node.Expression.ToString().EndsWith("Escape.Interpolated", StringComparison.Ordinal));
        var model = (await fixedDocument.GetSemanticModelAsync(TestContext.Current.CancellationToken))!;
        var symbol = Assert.IsAssignableFrom<IMethodSymbol>(
            model.GetSymbolInfo(invocation, TestContext.Current.CancellationToken).Symbol);
        Assert.Equal("JRK.JevRunner.Escape", symbol.ContainingType.ToDisplayString());
    }

    /// <summary>Creates a compilable usage fixture with both string and formattable parameter targets.</summary>
    private static string Source(string statement) => Contract + """
        class C
        {
            void Consume(string value) { }
            void Preserve(System.FormattableString value) { }
            void M(JRK.JevRunner.StateReference reference, JRK.JevRunner.StatePath path, JRK.JevRunner.DerivedReference derived)
            {
        """ + statement + "\n} }";

    /// <summary>Creates a workspace document so the same fixture supports semantic analysis and code fixes.</summary>
    private static Document CreateDocument(AdhocWorkspace workspace, string source)
    {
        var project = workspace.AddProject("AnalyzerTests", LanguageNames.CSharp)
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location) });
        return project.AddDocument("Usage.cs", SourceText.From(source));
    }

    /// <summary>Rejects invalid fixtures before retrieving diagnostics, including after applying a fix.</summary>
    private static async Task<ImmutableArray<Diagnostic>> Analyze(Document document)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var compilation = (await document.Project.GetCompilationAsync(cancellationToken))!;
        Assert.Empty(compilation.GetDiagnostics(cancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BackTickInterpolationAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(cancellationToken);
    }
}
