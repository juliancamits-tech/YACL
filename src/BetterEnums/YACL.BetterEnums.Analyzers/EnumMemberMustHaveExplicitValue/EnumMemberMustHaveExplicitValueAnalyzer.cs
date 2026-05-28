using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BetterEnums.YACL.Analyzers.EnumMemberMustHaveExplicitValue;

/// <summary>
/// Enforces that every enum member carries an explicit numeric value (<c>= N</c>).
/// </summary>
/// <remarks>
/// <para>
/// When members rely on implicit, compiler-assigned values, inserting a new member
/// between two existing ones silently renumbers every subsequent member.
/// This breaks database-stored values, serialized payloads, and switch expressions
/// without any compile-time warning.
/// </para>
/// <para>
/// With explicit values you can safely insert an intermediate state at any position
/// by assigning the next available number, leaving all existing values intact.
/// </para>
/// <example>
/// <code>
/// // ❌ Fragile – inserting between A and C shifts C from 2 to 3
/// enum Status { None = 0, A, B }
///
/// // ✅ Stable – new intermediate states get their own numbers, nothing shifts
/// enum Status { None = 0, A = 1, B = 2 }
/// </code>
/// </example>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumMemberMustHaveExplicitValueAnalyzer : DiagnosticAnalyzer
{

    private static readonly LocalizableString Title =
        "Enum member must have an explicit numeric value";

    private static readonly LocalizableString MessageFormat =
        "Enum member '{0}' in '{1}' must have an explicit value (e.g. '= {2}'). " +
        "Implicit values shift when members are reordered or inserted.";

    private static readonly LocalizableString Description =
        "Every enum member must declare its numeric value explicitly. " +
        "Implicit values are fragile: inserting a new member between two existing ones " +
        "silently changes the values of all subsequent members, breaking any code that " +
        "persists or compares enum values by number.";

    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new(
        Const.DiagnosticId.EnumMemberMustHaveExplicitValue,
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
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.EnumMemberDeclaration);
    }

    // -------------------------------------------------------------------------

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var member = (EnumMemberDeclarationSyntax)context.Node;

        // Already compliant.
        if (member.EqualsValue is not null) return;

        if (member.Parent is not EnumDeclarationSyntax enumDeclaration) return;

        // Resolve the current implicit value so the message can show the suggestion.
        var symbol = context.SemanticModel
            .GetDeclaredSymbol(member, context.CancellationToken) as IFieldSymbol;

        var implicitValue = symbol?.ConstantValue is not null
            ? FormatValue(symbol.ConstantValue)
            : "?";

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            member.Identifier.GetLocation(),
            member.Identifier.ValueText,
            enumDeclaration.Identifier.ValueText,
            implicitValue));
    }

    // -------------------------------------------------------------------------

    /// <summary>Formats the boxed constant value as a decimal string.</summary>
    private static string FormatValue(object constantValue) =>
        constantValue switch
        {
            byte v => v.ToString(),
            sbyte v => v.ToString(),
            short v => v.ToString(),
            ushort v => v.ToString(),
            int v => v.ToString(),
            uint v => v.ToString(),
            long v => v.ToString(),
            ulong v => v.ToString(),
            _ => "0"
        };
}