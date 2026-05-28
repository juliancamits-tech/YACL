using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterEnums.YACL.Analyzers.EnumMemberMustHaveExplicitValue;

/// <summary>
/// Provides the automatic code fix for <see cref="EnumMemberMustHaveExplicitValueAnalyzer"/>.
/// </summary>
/// <remarks>
/// Two actions are offered:
/// <list type="bullet">
///   <item><description>
///     <b>Single fix</b> — adds <c>= X</c> to the specific flagged member,
///     where <c>X</c> is the value the compiler would assign implicitly.
///   </description></item>
///   <item><description>
///     <b>Bulk fix</b> — adds <c>= X</c> to every implicit member in the
///     containing enum in one operation.
///   </description></item>
/// </list>
/// Both fixes preserve the current semantic values so no behavior changes.
/// The developer can then reassign values to leave gaps for future members.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumMemberMustHaveExplicitValueCodeFixProvider)), Shared]
public sealed class EnumMemberMustHaveExplicitValueCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Const.DiagnosticId.EnumMemberMustHaveExplicitValue);

    /// <inheritdoc/>
    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var span = diagnostic.Location.SourceSpan;

        var member = root.FindToken(span.Start).Parent?
            .AncestorsAndSelf()
            .OfType<EnumMemberDeclarationSyntax>()
            .FirstOrDefault();

        if (member is null) return;
        if (member.Parent is not EnumDeclarationSyntax enumDeclaration) return;

        var memberName = member.Identifier.ValueText;
        var enumName = enumDeclaration.Identifier.ValueText;

        // ── Fix 1: single member ──────────────────────────────────────────────
        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Add explicit value to '{memberName}'",
                createChangedDocument: ct =>
                    ApplyToMemberAsync(context.Document, member, ct),
                equivalenceKey: $"{nameof(EnumMemberMustHaveExplicitValueCodeFixProvider)}.Single"),
            diagnostic);

        // ── Fix 2: all implicit members in this enum ──────────────────────────
        bool hasMoreImplicit = enumDeclaration.Members
            .Any(m => m != member && m.EqualsValue is null);

        if (hasMoreImplicit)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Add explicit values to all members in '{enumName}'",
                    createChangedDocument: ct =>
                        ApplyToAllMembersAsync(context.Document, enumDeclaration, ct),
                    equivalenceKey: $"{nameof(EnumMemberMustHaveExplicitValueCodeFixProvider)}.All"),
                diagnostic);
        }
    }

    // -------------------------------------------------------------------------
    // Fixes
    // -------------------------------------------------------------------------

    private static async Task<Document> ApplyToMemberAsync(
        Document document,
        EnumMemberDeclarationSyntax member,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (root is null || semanticModel is null) return document;

        var symbol = semanticModel.GetDeclaredSymbol(member, cancellationToken) as IFieldSymbol;
        if (symbol?.ConstantValue is null) return document;

        var updated = member.WithEqualsValue(BuildEqualsValue(symbol.ConstantValue));
        return document.WithSyntaxRoot(root.ReplaceNode(member, updated));
    }

    private static async Task<Document> ApplyToAllMembersAsync(
        Document document,
        EnumDeclarationSyntax enumDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (root is null || semanticModel is null) return document;

        // Collect every implicit member → its replacement node.
        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

        foreach (var member in enumDeclaration.Members)
        {
            if (member.EqualsValue is not null) continue;

            var symbol = semanticModel.GetDeclaredSymbol(member, cancellationToken) as IFieldSymbol;
            if (symbol?.ConstantValue is null) continue;

            replacements[member] = member.WithEqualsValue(BuildEqualsValue(symbol.ConstantValue));
        }

        if (replacements.Count == 0) return document;

        var newRoot = root.ReplaceNodes(
            replacements.Keys,
            (original, _) => replacements[original]);

        return document.WithSyntaxRoot(newRoot);
    }

    // -------------------------------------------------------------------------
    // Syntax helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds an <c>= X</c> clause with spaces around the equals token.
    /// Emits the narrowest integer literal that represents the value without
    /// a type suffix (e.g., <c>= 3</c> instead of <c>= 3L</c>).
    /// </summary>
    private static EqualsValueClauseSyntax BuildEqualsValue(object constantValue) =>
        SyntaxFactory.EqualsValueClause(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    GetNumericLiteralToken(constantValue)))
            .WithEqualsToken(
                SyntaxFactory.Token(SyntaxKind.EqualsToken)
                    .WithLeadingTrivia(SyntaxFactory.Space)
                    .WithTrailingTrivia(SyntaxFactory.Space));

    /// <summary>
    /// Returns the tightest <see cref="SyntaxToken"/> for <paramref name="constantValue"/>
    /// without a type suffix. Values that fit in <see cref="int"/> are emitted as plain
    /// integer literals; larger values use <c>long</c> or <c>ulong</c> as needed.
    /// </summary>
    private static SyntaxToken GetNumericLiteralToken(object constantValue) =>
        constantValue switch
        {
            // Narrow types → widen to int (no suffix)
            byte v => SyntaxFactory.Literal((int)v),
            sbyte v => SyntaxFactory.Literal((int)v),
            short v => SyntaxFactory.Literal((int)v),
            ushort v => SyntaxFactory.Literal((int)v),

            // Int: direct (no suffix)
            int v => SyntaxFactory.Literal(v),

            // Uint: emit as int if safe, preserves no-suffix style
            uint v => v <= int.MaxValue
                        ? SyntaxFactory.Literal((int)v)
                        : SyntaxFactory.Literal(v),

            // Long: emit as int if safe
            long v => v is >= int.MinValue and <= int.MaxValue
                        ? SyntaxFactory.Literal((int)v)
                        : SyntaxFactory.Literal(v),

            // Ulong: emit as int/long if safe
            ulong v => v <= (ulong)int.MaxValue
                        ? SyntaxFactory.Literal((int)v)
                        : SyntaxFactory.Literal(v),

            _ => SyntaxFactory.Literal(0)
        };
}