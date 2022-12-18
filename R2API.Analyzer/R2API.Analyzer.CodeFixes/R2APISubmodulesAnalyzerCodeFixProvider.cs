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

namespace R2API.Analyzer;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(R2APISubmodulesAnalyzerCodeFixProvider))]
public class R2APISubmodulesAnalyzerCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(R2APISubmodulesAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (declaration == null)
        {
            // TODO: Support properties
            //var declaration2 = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<AccessorDeclarationSyntax>().Where(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)).FirstOrDefault();

            return;
        }

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.CodeFixTitle,
                createChangedDocument: c => AddTheSetHooksCall(
                    root,
                    context.Document, declaration, c,
                    diagnostic.Properties[R2APISubmodulesAnalyzer.SetHooksMethodSymbolExpression],
                    diagnostic.Properties[R2APISubmodulesAnalyzer.SetHooksMethodSymbolName]
                ),
                equivalenceKey: nameof(CodeFixResources.CodeFixTitle)
            ),
            diagnostic
        );
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<Document> AddTheSetHooksCall(SyntaxNode root, Document document,
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        MethodDeclarationSyntax methodDeclaration, CancellationToken _,
        string setHookMethodSymbolExpression, string setHookMethodSymbolName)
    {
        var callToSetHookMethod =
            SyntaxFactory.ExpressionStatement
            (
                SyntaxFactory.InvocationExpression
                (
                    SyntaxFactory.MemberAccessExpression
                    (
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName
                        (
                            setHookMethodSymbolExpression
                        ),
                        SyntaxFactory.Token
                        (
                            SyntaxKind.DotToken
                        ),
                        SyntaxFactory.IdentifierName
                        (
                            setHookMethodSymbolName
                        )
                    )
                )
            )
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newMethodBody = methodDeclaration.Body.WithStatements(methodDeclaration.Body.Statements.Insert(0, callToSetHookMethod));

        var newRoot = root.ReplaceNode(methodDeclaration.Body, newMethodBody);

        return document.WithSyntaxRoot(newRoot);
    }
}
