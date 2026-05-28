using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BetterEnums.YACL.Analyzers.EnumNullable;

/// <summary>
/// Prevents nullable enum types (<c>MyEnum?</c>) anywhere in the code.
/// </summary>
/// <remarks>
/// Nullable enums are unnecessary when every enum already has a <c>None = 0</c>
/// member (enforced by <see cref="EnumMustHaveNoneValueAnalyzer"/>).
/// Using <c>MyEnum.None</c> is always clearer than <c>null</c>.
/// This analyzer fires on every <c>NullableTypeSyntax</c> whose element type
/// resolves to an enum, covering fields, properties, parameters, local variables,
/// return types, and generic type arguments.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumNullableAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title =
        "Nullable enum type is not allowed";

    private static readonly LocalizableString MessageFormat =
        "'{0}?' is a nullable enum. Use '{0}' and represent the absence of a value with '{0}.None'.";

    private static readonly LocalizableString Description =
        "Enum types must never be nullable. Declare every enum with a 'None = 0' member " +
        "and use that member instead of null. " +
        "This makes intent explicit, avoids null reference exceptions, " +
        "and leverages the safe default value provided by 'None = 0'.";

    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new(
        Const.DiagnosticId.EnumNotNullable,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NullableType);
    }

    // -------------------------------------------------------------------------

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var nullableType = (NullableTypeSyntax)context.Node;

        var typeInfo = context.SemanticModel
            .GetTypeInfo(nullableType.ElementType, context.CancellationToken);

        // Only flag nullable enum types; ignore nullable value/reference types.
        if (typeInfo.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumSymbol)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            nullableType.GetLocation(),
            enumSymbol.Name));
    }
}