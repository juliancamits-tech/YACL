using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterEnums.YACL.Analyzers.EnumMustHaveNoneValue;

/// <summary>
/// Provides the automatic code fix for <see cref="EnumMustHaveNoneValueAnalyzer"/>.
/// </summary>
/// <remarks>
/// Two variants are handled:
/// <list type="bullet">
///   <item><description>
///     <c>None</c> does not exist → inserts <c>None = 0</c> as the first member,
///     inheriting the indentation from the existing first member.
///   </description></item>
///   <item><description>
///     <c>None</c> exists but its value is not 0 → replaces its <c>EqualsValueClause</c>
///     with <c>= 0</c>.
///   </description></item>
/// </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumMustHaveNoneValueCodeFixProvider)), Shared]
public sealed class EnumMustHaveNoneValueCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Const.DiagnosticId.EnumMustHaveNoneValue);

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

        // Diagnostic is reported on the enum identifier — its parent is the declaration.
        var enumDeclaration = root.FindToken(span.Start).Parent?
            .AncestorsAndSelf()
            .OfType<EnumDeclarationSyntax>()
            .FirstOrDefault();

        if (enumDeclaration is null) return;

        bool noneExists = diagnostic.Properties.TryGetValue(
            EnumMustHaveNoneValueAnalyzer.NoneExistsPropertyKey, out var val)
            && val == "true";

        var title = noneExists
            ? "Set 'None' member value to 0"
            : "Add 'None = 0' as first member";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: ct =>
                    ApplyFixAsync(context.Document, enumDeclaration, noneExists, ct),
                equivalenceKey: nameof(EnumMustHaveNoneValueCodeFixProvider)),
            diagnostic);
    }

    // -------------------------------------------------------------------------

    private static async Task<Document> ApplyFixAsync(
        Document document,
        EnumDeclarationSyntax enumDeclaration,
        bool noneExists,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var newEnumDeclaration = noneExists
            ? SetNoneMemberToZero(enumDeclaration)
            : AddNoneMemberAsFirst(enumDeclaration);

        if (newEnumDeclaration is null) return document;

        return document.WithSyntaxRoot(root.ReplaceNode(enumDeclaration, newEnumDeclaration));
    }

    /// <summary>
    /// Finds the existing <c>None</c> member and replaces its value with <c>= 0</c>.
    /// </summary>
    private static EnumDeclarationSyntax? SetNoneMemberToZero(EnumDeclarationSyntax enumDeclaration)
    {
        var noneMember = enumDeclaration.Members
            .FirstOrDefault(m => m.Identifier.ValueText == "None");

        if (noneMember is null) return null;

        var updatedMember = noneMember.WithEqualsValue(BuildEqualsZero());
        return enumDeclaration.ReplaceNode(noneMember, updatedMember);
    }

    /// <summary>
    /// Inserts a new <c>None = 0</c> member at index 0, copying the existing
    /// first member's leading trivia to preserve indentation.
    /// </summary>
    private static EnumDeclarationSyntax AddNoneMemberAsFirst(EnumDeclarationSyntax enumDeclaration)
    {
        // Steal leading trivia (indentation) from the existing first member.
        var leadingTrivia = enumDeclaration.Members.Count > 0
            ? enumDeclaration.Members[0].GetLeadingTrivia()
            : SyntaxTriviaList.Create(SyntaxFactory.LineFeed);

        var noneMember = SyntaxFactory
            .EnumMemberDeclaration(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.Identifier("None"),
                BuildEqualsZero())
            .WithLeadingTrivia(leadingTrivia);

        return enumDeclaration.WithMembers(
            enumDeclaration.Members.Insert(0, noneMember));
    }

    // -------------------------------------------------------------------------

    /// <summary>Builds the <c>= 0</c> equals-value clause with proper spacing.</summary>
    private static EqualsValueClauseSyntax BuildEqualsZero() =>
        SyntaxFactory.EqualsValueClause(
            SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(0)))
        .WithEqualsToken(
            SyntaxFactory.Token(SyntaxKind.EqualsToken)
                .WithLeadingTrivia(SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space));
}