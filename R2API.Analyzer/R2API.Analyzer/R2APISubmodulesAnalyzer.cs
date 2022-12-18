using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace R2API.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class R2APISubmodulesAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "R2APISubmodulesAnalyzer";

    // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
    // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.R2APISubmodulesAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.R2APISubmodulesAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.R2APISubmodulesAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public const string SetHooksMethodName = "SetHooks";
    public const string UnsetHooksMethodName = "UnsetHooks";

    // Used for passing the SetHooks method full name to the code fixer
    public const string SetHooksMethodSymbolExpression = nameof(SetHooksMethodSymbolExpression);
    public const string SetHooksMethodSymbolName = nameof(SetHooksMethodSymbolName);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCodeBlockAction(CheckIfPublicApiMethodsLazyInitHooks);
    }

    private void CheckIfPublicApiMethodsLazyInitHooks(CodeBlockAnalysisContext codeBlockContext)
    {
        // We only care about method bodies.
        if (codeBlockContext.OwningSymbol.Kind != SymbolKind.Method)
        {
            return;
        }

        // FIXME: The analyzer doesnt seem to work for expression bodied methods ( => Thing() )

        IMethodSymbol method = (IMethodSymbol)codeBlockContext.OwningSymbol;

        // Not a public api method, we don't care
        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        // We unsubscribe from an api event, don't enforce setting hooks for this case.
        if (method.MethodKind == MethodKind.EventRemove)
        {
            return;
        }

        // SetHooks and UnsetHooks methods will obviously never call SetHooks
        if (method.Name == SetHooksMethodName ||
            method.Name == UnsetHooksMethodName)
        {
            return;
        }

        var setHooksMethodSymbol = GetSetHooksMethodRecursive(method);
        var submoduleDoesNotContainASetHooksMethod = setHooksMethodSymbol == null;
        if (submoduleDoesNotContainASetHooksMethod)
        {
            return;
        }

        BlockSyntax block = (BlockSyntax)codeBlockContext.CodeBlock?.ChildNodes()?.FirstOrDefault(n => n.Kind() == SyntaxKind.Block);
        if (block == null)
        {
            return;
        }

        foreach (var statement in block.Statements)
        {
            if (statement is ExpressionStatementSyntax expressionStatement &&
                expressionStatement.Expression is InvocationExpressionSyntax invocationExpression)
            {
                if (SymbolEqualityComparer.Default.Equals(codeBlockContext.SemanticModel.GetSymbolInfo(invocationExpression).Symbol, setHooksMethodSymbol))
                {
                    // one of the invocation statement of the method is calling SetHooks, all good.
                    return;
                }
            }
        }

        EmitError(codeBlockContext, method, setHooksMethodSymbol);
    }

    private static void EmitError(CodeBlockAnalysisContext codeBlockContext, IMethodSymbol method, IMethodSymbol setHookMethodSymbol)
    {
        var containingType = setHookMethodSymbol.ContainingType.ToString();

        Diagnostic diagnostic = Diagnostic.Create(
            descriptor: Rule,
            location: method.Locations[0],
            messageArgs: method.Name,
            properties: new Dictionary<string, string>
            {
                {
                    SetHooksMethodSymbolExpression,
                    // TODO: There is definitly a cleaner way to remove the namespace prefix but I can't be bothered to do that right now.
                    // The "R2API." prefix should ideally only be removed if the current source document have a using R2API; at the top
                    containingType.Replace("R2API.", "")
                },
                {
                    SetHooksMethodSymbolName,
                    setHookMethodSymbol.Name.ToString()
                }
            }.ToImmutableDictionary()
        );

        codeBlockContext.ReportDiagnostic(diagnostic);
    }

    private static IMethodSymbol GetSetHooksMethodRecursive(IMethodSymbol methodSymbol)
    {
        var t = methodSymbol.ContainingType;

        while (true)
        {
            if (t == null)
            {
                break;
            }

            var setHookMethodSymbol = t.GetMembers(SetHooksMethodName).Where(s => s.GetType().GetInterface(nameof(IMethodSymbol)) != null).FirstOrDefault();
            if (setHookMethodSymbol == null)
            {
                // could be a nested type, check parents
                t = t.ContainingType;
            }
            else
            {
                return (IMethodSymbol)setHookMethodSymbol;
            }
        }

        return null;
    }
}
