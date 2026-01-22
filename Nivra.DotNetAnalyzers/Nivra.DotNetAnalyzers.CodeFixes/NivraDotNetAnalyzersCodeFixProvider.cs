using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Nivra.DotNetAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NivraDotNetAnalyzersCodeFixProvider)), Shared]
    public class NivraDotNetAnalyzersCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NivraDotNetAnalyzersAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the node identified by the diagnostic
            var node = root.FindNode(diagnosticSpan);

            // Register a code action that will invoke the fix
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Place each parameter on a new line",
                    createChangedDocument: c => FormatParametersAsync(context.Document, node, c),
                    equivalenceKey: "PlaceEachParameterOnNewLine"),
                diagnostic);
        }

        private async Task<Document> FormatParametersAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode newRoot = null;
            string indent = "";

            // Always walk up to the parent method or invocation if node is a parameter/argument or their list
            if (node is ParameterSyntax param && node.Parent is ParameterListSyntax plist && plist.Parent is BaseMethodDeclarationSyntax method)
            {
                indent = method.GetLeadingTrivia().ToString();
                var formattedParameters = FormatParameters(plist.Parameters, indent);
                var newMethod = method.WithParameterList(plist.WithParameters(formattedParameters));
                newRoot = root.ReplaceNode(method, newMethod);
            }
            else if (node is ArgumentSyntax arg && node.Parent is ArgumentListSyntax alist && alist.Parent is InvocationExpressionSyntax invocation)
            {
                indent = invocation.GetLeadingTrivia().ToString();
                var formattedArguments = FormatParameters(alist.Arguments, indent);
                var newInvocation = invocation.WithArgumentList(alist.WithArguments(formattedArguments));
                newRoot = root.ReplaceNode(invocation, newInvocation);
            }
            else if (node is ParameterListSyntax parameterList && parameterList.Parent is BaseMethodDeclarationSyntax parentMethod)
            {
                indent = parentMethod.GetLeadingTrivia().ToString();
                var formattedParameters = FormatParameters(parameterList.Parameters, indent);
                var newMethod = parentMethod.WithParameterList(parameterList.WithParameters(formattedParameters));
                newRoot = root.ReplaceNode(parentMethod, newMethod);
            }
            else if (node is ArgumentListSyntax argumentList && argumentList.Parent is InvocationExpressionSyntax parentInvocation)
            {
                indent = parentInvocation.GetLeadingTrivia().ToString();
                var formattedArguments = FormatParameters(argumentList.Arguments, indent);
                var newInvocation = parentInvocation.WithArgumentList(argumentList.WithArguments(formattedArguments));
                newRoot = root.ReplaceNode(parentInvocation, newInvocation);
            }
            else if (node is BaseMethodDeclarationSyntax methodDeclaration)
            {
                indent = methodDeclaration.GetLeadingTrivia().ToString();
                var formattedParameters = FormatParameters(methodDeclaration.ParameterList.Parameters, indent);
                var newMethod = methodDeclaration.WithParameterList(methodDeclaration.ParameterList.WithParameters(formattedParameters));
                newRoot = root.ReplaceNode(methodDeclaration, newMethod);
            }
            else if (node is InvocationExpressionSyntax invocationExpr)
            {
                // Use the parent node's leading trivia to calculate indentation
                indent = invocationExpr.GetLeadingTrivia().ToString();

                // Format the arguments to place each on a new line with proper indentation
                var formattedArgs = invocationExpr.ArgumentList.Arguments.Select((argument, index) =>
                    argument.WithLeadingTrivia(
                        SyntaxFactory.ElasticCarriageReturnLineFeed,    
                        SyntaxFactory.Whitespace(indent + "    ")
                    )
                ).ToList();

                // Replace the argument list with the formatted arguments
                var newInvocation = invocationExpr.WithArgumentList(
                    invocationExpr.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(formattedArgs, invocationExpr.ArgumentList.Arguments.GetSeparators()))
                );

                // Replace the invocation expression in the syntax tree
                newRoot = root.ReplaceNode(invocationExpr, newInvocation);
            }

            if (newRoot == null)
                return document;

            return document.WithSyntaxRoot(newRoot);
        }

        private SeparatedSyntaxList<T> FormatParameters<T>(SeparatedSyntaxList<T> parameters, string indent) where T : SyntaxNode
        {
            if (!parameters.Any())
                return parameters;

            // Place the first parameter on a new line after the opening parenthesis
            var formattedParameters = parameters.Select((p, i) =>
                i == 0
                    ? p.WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed, SyntaxFactory.Whitespace(indent + "    "))
                    : p.WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed, SyntaxFactory.Whitespace(indent + "    "))
            ).ToList();
            return SyntaxFactory.SeparatedList(formattedParameters, parameters.GetSeparators());
        }
    }
}
