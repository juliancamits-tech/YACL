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

namespace BetterEnums.YACL.Analyzers.EnumNullable;

/// <summary>
/// Provides the automatic code fix for <see cref="NullableEnumAnalyzer"/>.
/// Removes the <c>?</c> from the nullable enum type and replaces any
/// adjacent <c>null</c> literal with <c>EnumType.None</c>.
/// </summary>
/// <remarks>
/// Null literals are replaced only in the immediate declaration context
/// (variable initializer or parameter default value). For return types and
/// generic arguments the <c>?</c> is removed and the developer is responsible
/// for updating the remaining usages.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableEnumCodeFixProvider)), Shared]
public sealed class NullableEnumCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Const.DiagnosticId.EnumNotNullable);

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

        var nullableType = root.FindNode(span, getInnermostNodeForTie: true) as NullableTypeSyntax
                        ?? root.FindToken(span.Start).Parent?
                               .AncestorsAndSelf()
                               .OfType<NullableTypeSyntax>()
                               .FirstOrDefault();

        if (nullableType is null) return;

        var enumTypeName = nullableType.ElementType.ToString();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Replace '{enumTypeName}?' with '{enumTypeName}' (use '{enumTypeName}.None' instead of null)",
                createChangedDocument: ct =>
                    ApplyFixAsync(context.Document, nullableType, enumTypeName, ct),
                equivalenceKey: nameof(NullableEnumCodeFixProvider)),
            diagnostic);
    }

    // -------------------------------------------------------------------------

    private static async Task<Document> ApplyFixAsync(
        Document document,
        NullableTypeSyntax nullableType,
        string enumTypeName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        // All replacements applied in a single ReplaceNodes call to avoid
        // working on a stale tree.
        var replacements = new Dictionary<SyntaxNode, SyntaxNode>
        {
            // 1. Remove '?'  →  keep the inner ElementType with the same trivia.
            [nullableType] = nullableType.ElementType.WithTriviaFrom(nullableType)
        };

        // 2. In supported parent contexts, replace 'null' → 'EnumType.None'.
        CollectNullReplacements(nullableType, enumTypeName, replacements);

        var newRoot = root.ReplaceNodes(
            replacements.Keys,
            (original, _) => replacements[original]);

        return document.WithSyntaxRoot(newRoot);
    }

    // -------------------------------------------------------------------------
    // Null literal collection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Inspects the immediate syntactic parent of the <c>NullableTypeSyntax</c>
    /// and registers any <c>null</c> literal that should be replaced by
    /// <c>EnumType.None</c>.
    /// </summary>
    private static void CollectNullReplacements(
        NullableTypeSyntax nullableType,
        string enumTypeName,
        Dictionary<SyntaxNode, SyntaxNode> replacements)
    {
        var noneAccess = BuildNoneAccess(enumTypeName);

        switch (nullableType.Parent)
        {
            // MyEnum? x = null;   /   private MyEnum? _field = null;
            case VariableDeclarationSyntax varDecl:
                {
                    foreach (var variable in varDecl.Variables)
                    {
                        if (TryGetNullLiteral(variable.Initializer?.Value, out var lit))
                            replacements[lit!] = noneAccess;
                    }
                    break;
                }

            // void Method(MyEnum? status = null)
            case ParameterSyntax parameter:
                {
                    if (TryGetNullLiteral(parameter.Default?.Value, out var lit))
                        replacements[lit!] = noneAccess;
                    break;
                }

                // All other contexts (return types, generic arguments, etc.):
                // the '?' is removed; the developer handles the remaining usages.
        }
    }

    // -------------------------------------------------------------------------
    // Syntax helpers
    // -------------------------------------------------------------------------

    /// <summary>Builds the <c>EnumType.None</c> member-access expression.</summary>
    private static MemberAccessExpressionSyntax BuildNoneAccess(string enumTypeName) =>
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(enumTypeName),
            SyntaxFactory.IdentifierName("None"));

    private static bool TryGetNullLiteral(
        ExpressionSyntax? expression,
        out LiteralExpressionSyntax? literal)
    {
        if (expression is LiteralExpressionSyntax l &&
            l.IsKind(SyntaxKind.NullLiteralExpression))
        {
            literal = l;
            return true;
        }

        literal = null;
        return false;
    }
}