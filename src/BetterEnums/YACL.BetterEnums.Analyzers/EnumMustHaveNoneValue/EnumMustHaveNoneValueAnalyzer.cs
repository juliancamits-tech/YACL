using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BetterEnums.YACL.Analyzers.EnumMustHaveNoneValue;

/// <summary>
/// Enforces that every <c>enum</c> declares a member named <c>None</c> with
/// the constant value <c>0</c>.
/// </summary>
/// <remarks>
/// <para>
/// Having <c>None = 0</c> as the first member guarantees that:
/// <list type="bullet">
///   <item><description>The default (uninitialized) value of the enum is always <c>None</c>.</description></item>
///   <item><description>Nullable enum types (<c>MyEnum?</c>) are never needed — use <c>MyEnum.None</c> instead.</description></item>
/// </list>
/// </para>
/// <para>
/// The check is semantic: an implicit <c>None</c> at position 0 (value inferred as 0
/// by the compiler) is accepted, not just <c>None = 0</c> spelled out explicitly.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumMustHaveNoneValueAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic property key that signals the code fix whether a <c>None</c>
    /// member already exists but has the wrong value.
    /// </summary>
    internal const string NoneExistsPropertyKey = "NoneExists";

    private static readonly LocalizableString Title =
        "Enum must declare a 'None = 0' member";

    private static readonly LocalizableString MessageFormat =
        "Enum '{0}' must have a member named 'None' with value 0 as the safe default value";

    private static readonly LocalizableString Description =
        "Every enum must declare a 'None = 0' member. " +
        "This ensures the default value of uninitialized variables is always meaningful, " +
        "and eliminates the need for nullable enum types (MyEnum?).";

    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new(
        Const.DiagnosticId.EnumMustHaveNoneValue,
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
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.EnumDeclaration);
    }

    // -------------------------------------------------------------------------

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var enumDeclaration = (EnumDeclarationSyntax)context.Node;

        var symbol = context.SemanticModel
            .GetDeclaredSymbol(enumDeclaration, context.CancellationToken);

        if (symbol is null) return;

        // Find a member named exactly "None".
        var noneField = symbol.GetMembers("None")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();

        // Already compliant: "None" exists AND its constant value is 0.
        if (noneField is not null && IsZero(noneField.ConstantValue))
            return;

        var properties = ImmutableDictionary.Create<string, string?>()
            .Add(NoneExistsPropertyKey, noneField is not null ? "true" : "false");

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            enumDeclaration.Identifier.GetLocation(),
            properties,
            symbol.Name));
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="constantValue"/> is the numeric
    /// zero for any enum underlying type (<c>byte</c> through <c>ulong</c>).
    /// </summary>
    internal static bool IsZero(object? constantValue) =>
        constantValue switch
        {
            byte v => v == 0,
            sbyte v => v == 0,
            short v => v == 0,
            ushort v => v == 0,
            int v => v == 0,
            uint v => v == 0U,
            long v => v == 0L,
            ulong v => v == 0UL,
            _ => false
        };
}