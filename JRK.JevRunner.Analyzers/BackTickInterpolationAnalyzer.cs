using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JRK.JevRunner.Analyzers;

/// <summary>Finds state-reference interpolations that lose automatic quoting through string conversion.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BackTickInterpolationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The diagnostic identifier for an interpolation that loses backtick quoting.</summary>
    public const string DiagnosticId = "JEV100";

    /// <summary>The diagnostic identifier for explicit backticks around an automatically quoted argument.</summary>
    public const string RedundantBackticksDiagnosticId = "JEV101";

    private static readonly DiagnosticDescriptor RedundantBackticksRule = new(
        RedundantBackticksDiagnosticId,
        "Remove redundant backticks around state interpolation",
        "Remove explicit backticks around '{0}'; JevRunner quotes this value automatically",
        "JevRunner.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Explicit backticks around an StateReference or StatePath argument in a FormattableString produce duplicate quoting when JevRunner formats it.");

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Preserve backtick quoting in interpolated instructions",
        "Interpolating '{0}' into a string loses automatic backtick quoting; use Escape.Interpolated to format it",
        "JevRunner.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Use Escape.Interpolated to preserve interpolation arguments as a FormattableString and apply automatic backtick quoting to state references and paths.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RedundantBackticksRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(startContext =>
        {
            var backTick = startContext.Compilation.GetTypeByMetadataName("JRK.JevRunner.IBackTick");
            if (backTick is null)
                return;

            var formattableString = startContext.Compilation.GetTypeByMetadataName("System.FormattableString");
            var formattable = startContext.Compilation.GetTypeByMetadataName("System.IFormattable");
            startContext.RegisterSyntaxNodeAction(nodeContext =>
                AnalyzeInterpolation(nodeContext, backTick, formattableString, formattable),
                SyntaxKind.InterpolatedStringExpression);
        });
    }

    /// <summary>Finds lost quoting in strings and redundant explicit quoting in preserved interpolations.</summary>
    private static void AnalyzeInterpolation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol backTick,
        INamedTypeSymbol? formattableString,
        INamedTypeSymbol? formattable)
    {
        var expression = (InterpolatedStringExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken);
        var preservesArguments = SymbolEqualityComparer.Default.Equals(typeInfo.ConvertedType, formattableString) ||
            SymbolEqualityComparer.Default.Equals(typeInfo.ConvertedType, formattable);

        // Also catch string interpolation subsequently boxed as object, but not custom interpolation handlers.
        if (!preservesArguments && typeInfo.Type?.SpecialType != SpecialType.System_String &&
            typeInfo.ConvertedType?.SpecialType != SpecialType.System_String)
            return;

        foreach (var interpolation in expression.Contents.OfType<InterpolationSyntax>())
        {
            var type = context.SemanticModel.GetTypeInfo(interpolation.Expression, context.CancellationToken).Type;
            if (type is null || !IsBackTick(type, backTick))
                continue;

            if (!preservesArguments)
                context.ReportDiagnostic(Diagnostic.Create(Rule, interpolation.Expression.GetLocation(), type.Name));
            else if (HasSurroundingBackticks(expression, interpolation))
                context.ReportDiagnostic(Diagnostic.Create(RedundantBackticksRule, interpolation.GetLocation(), type.Name));
        }
    }

    /// <summary>Checks literal text immediately adjacent to the hole, including raw and verbatim strings.</summary>
    private static bool HasSurroundingBackticks(InterpolatedStringExpressionSyntax expression, InterpolationSyntax interpolation)
    {
        var index = expression.Contents.IndexOf(interpolation);
        return index > 0 && index + 1 < expression.Contents.Count &&
            expression.Contents[index - 1] is InterpolatedStringTextSyntax before &&
            expression.Contents[index + 1] is InterpolatedStringTextSyntax after &&
            before.TextToken.ValueText.EndsWith("`", System.StringComparison.Ordinal) &&
            after.TextToken.ValueText.StartsWith("`", System.StringComparison.Ordinal);
    }

    /// <summary>Recognizes the marker itself, inherited implementations, and constrained type parameters.</summary>
    private static bool IsBackTick(ITypeSymbol type, INamedTypeSymbol backTick,
        HashSet<ITypeSymbol>? visited = null)
    {
        if (SymbolEqualityComparer.Default.Equals(type, backTick) ||
            type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, backTick)))
            return true;

        if (type is not ITypeParameterSymbol parameter)
            return false;

        // Class constraints can supply the marker through their inherited interfaces.
        // Track visited parameters so incomplete or cyclic constraints cannot recurse forever.
        visited ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        return visited.Add(type) && parameter.ConstraintTypes.Any(constraint => IsBackTick(constraint, backTick, visited));
    }
}
