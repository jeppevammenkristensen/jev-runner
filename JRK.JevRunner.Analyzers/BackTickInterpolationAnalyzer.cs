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

    /// <summary>
    /// Reports explicit backticks around interpolation arguments that are already quoted automatically.
    /// </summary>
    private static readonly DiagnosticDescriptor RedundantBackticksRule = new(
        RedundantBackticksDiagnosticId,
        "Remove redundant backticks around state interpolation",
        "Remove explicit backticks around '{0}'; JevRunner quotes this value automatically",
        "JevRunner.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Explicit backticks around an IBackTick or AssetAndStateReference argument in a FormattableString produce duplicate quoting when JevRunner formats it.");

    /// <summary>
    /// Reports string interpolation that discards automatic quoting of state references and paths.
    /// </summary>
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Preserve backtick quoting in interpolated instructions",
        "Interpolating '{0}' into a string loses automatic backtick quoting; use Escape.Interpolated to format it",
        "JevRunner.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Use Escape.Interpolated to preserve interpolation arguments as a FormattableString and apply automatic backtick quoting to IBackTick and AssetAndStateReference values.");

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
            var assetReference = startContext.Compilation.GetTypeByMetadataName("JRK.JevRunner.Annotation.AssetAndStateReference");
            if (backTick is null && assetReference is null)
                return;

            var formattableString = startContext.Compilation.GetTypeByMetadataName("System.FormattableString");
            var formattable = startContext.Compilation.GetTypeByMetadataName("System.IFormattable");
            startContext.RegisterSyntaxNodeAction(nodeContext =>
                AnalyzeInterpolation(nodeContext, backTick, assetReference, formattableString, formattable),
                SyntaxKind.InterpolatedStringExpression);
        });
    }

    /// <summary>
    /// Finds lost quoting in strings and redundant explicit quoting in preserved interpolations.
    /// </summary>
    /// <param name="context">The interpolation's syntax and semantic analysis context.</param>
    /// <param name="backTick">The runtime quoting marker, if available.</param>
    /// <param name="assetReference">The runtime prepared asset type, if available.</param>
    /// <param name="formattableString">The type preserving interpolation arguments.</param>
    /// <param name="formattable">The interface preserving interpolation arguments.</param>
    private static void AnalyzeInterpolation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? backTick,
        INamedTypeSymbol? assetReference,
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
            if (type is null || !IsAutomaticallyQuoted(type, backTick, assetReference))
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

    /// <summary>
    /// Recognizes marker implementations and prepared assets, including inheritance and generic constraints.
    /// </summary>
    /// <param name="type">The interpolated argument's type or a generic constraint.</param>
    /// <param name="backTick">The runtime quoting marker, if available.</param>
    /// <param name="assetReference">The runtime prepared asset type, if available.</param>
    /// <param name="visited">Type parameters already examined to avoid cyclic constraint traversal.</param>
    /// <returns>Whether the runtime formatter automatically quotes values of this type.</returns>
    private static bool IsAutomaticallyQuoted(ITypeSymbol type, INamedTypeSymbol? backTick,
        INamedTypeSymbol? assetReference,
        HashSet<ITypeSymbol>? visited = null)
    {
        if (SymbolEqualityComparer.Default.Equals(type, backTick) ||
            type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, backTick)))
            return true;

        // The runtime formatter also matches prepared assets and any derived records.
        for (var candidate = type as INamedTypeSymbol; candidate != null; candidate = candidate.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, assetReference))
                return true;
        }

        if (type is not ITypeParameterSymbol parameter)
            return false;

        // Class constraints can supply the marker through their inherited interfaces.
        // Track visited parameters so incomplete or cyclic constraints cannot recurse forever.
        visited ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        return visited.Add(type) && parameter.ConstraintTypes.Any(constraint =>
            IsAutomaticallyQuoted(constraint, backTick, assetReference, visited));
    }
}
