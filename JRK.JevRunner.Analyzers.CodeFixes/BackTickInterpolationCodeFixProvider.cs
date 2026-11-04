using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace JRK.JevRunner.Analyzers;

/// <summary>Preserves interpolation arguments and applies JevRunner's backtick-aware formatting.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BackTickInterpolationCodeFixProvider)), Shared]
public sealed class BackTickInterpolationCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("JEV100", "JEV101");

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var interpolation = root?.FindNode(context.Span).AncestorsAndSelf()
            .OfType<InterpolatedStringExpressionSyntax>().FirstOrDefault();
        if (root is not CompilationUnitSyntax compilationUnit || interpolation is null)
            return;

        if (context.Diagnostics.Any(diagnostic => diagnostic.Id == "JEV101"))
        {
            var hole = root.FindNode(context.Span).AncestorsAndSelf().OfType<InterpolationSyntax>().FirstOrDefault();
            if (hole is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Remove redundant backticks",
                    cancellationToken => RemoveBackticksAsync(context.Document, interpolation, hole, cancellationToken),
                    equivalenceKey: "RemoveRedundantBackticks"),
                context.Diagnostics);
            return;
        }

        // Do not offer a fix against older runtimes without the interpolation helper.
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var escape = model?.Compilation.GetTypeByMetadataName("JRK.JevRunner.Escape");
        var formattableString = model?.Compilation.GetTypeByMetadataName("System.FormattableString");
        if (escape is null || !escape.GetMembers("Interpolated").OfType<IMethodSymbol>().Any(method =>
                method.IsStatic && method.DeclaredAccessibility == Accessibility.Public && !method.IsGenericMethod &&
                method.ReturnType.SpecialType == SpecialType.System_String && method.Parameters.Length == 1 &&
                method.Parameters[0].RefKind == RefKind.None &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, formattableString)))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use Escape.Interpolated",
                cancellationToken => ApplyFixAsync(context.Document, compilationUnit, interpolation, cancellationToken),
                equivalenceKey: "ApplyBackTickFormatting"),
            context.Diagnostics);
    }

    /// <summary>Removes only the two adjacent backticks, preserving raw-string delimiters, indentation, and other text.</summary>
    private static async Task<Document> RemoveBackticksAsync(Document document, InterpolatedStringExpressionSyntax expression,
        InterpolationSyntax hole, CancellationToken cancellationToken)
    {
        var index = expression.Contents.IndexOf(hole);
        var before = ((InterpolatedStringTextSyntax)expression.Contents[index - 1]).TextToken;
        var after = ((InterpolatedStringTextSyntax)expression.Contents[index + 1]).TextToken;
        var beforeLength = BacktickSourceLength(before.Text, atStart: false);
        var afterLength = BacktickSourceLength(after.Text, atStart: true);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return document.WithText(text.WithChanges(
            new TextChange(new TextSpan(before.Span.End - beforeLength, beforeLength), string.Empty),
            new TextChange(new TextSpan(after.SpanStart, afterLength), string.Empty)));
    }

    /// <summary>Accounts for backticks represented as Unicode escapes in ordinary interpolated string text.</summary>
    private static int BacktickSourceLength(string text, bool atStart)
    {
        foreach (var spelling in new[] { "`", @"\u0060", @"\U00000060" })
        {
            if (atStart ? text.StartsWith(spelling, System.StringComparison.Ordinal) : text.EndsWith(spelling, System.StringComparison.Ordinal))
                return spelling.Length;
        }

        return 0;
    }

    /// <summary>Passes the entire interpolation to the formattable-string helper, retaining a string result.</summary>
    private static async Task<Document> ApplyFixAsync(Document document, CompilationUnitSyntax root,
        InterpolatedStringExpressionSyntax interpolation, CancellationToken cancellationToken)
    {
        var replacement = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseExpression("global::JRK.JevRunner.Escape.Interpolated")
                    .WithAdditionalAnnotations(Simplifier.Annotation),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(interpolation.WithoutTrivia()))))
            .WithTriviaFrom(interpolation)
            .WithAdditionalAnnotations(Formatter.Annotation);
        var updated = root.ReplaceNode(interpolation, replacement);
        if (!root.Usings.Any(directive => directive.Alias is null &&
                directive.StaticKeyword.IsKind(SyntaxKind.None) &&
                (directive.Name?.ToString() == "JRK.JevRunner" ||
                 directive.Name?.ToString() == "global::JRK.JevRunner")))
        {
            updated = updated.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("JRK.JevRunner"))
                .WithAdditionalAnnotations(Formatter.Annotation));
        }

        // Apply simplification here rather than relying on the IDE to process annotations.
        // This produces Escape.Interpolated while retaining qualification when a local name conflicts.
        return await Simplifier.ReduceAsync(document.WithSyntaxRoot(updated), Simplifier.Annotation,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
