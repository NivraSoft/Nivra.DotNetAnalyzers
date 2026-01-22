using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Nivra.DotNetAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NivraDotNetAnalyzersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NivraDotNetAnalyzers";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register a syntax node action for method declarations and invocations
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;

            // Analyze both method declarations and invocation expressions
            if (node is BaseMethodDeclarationSyntax methodDeclaration)
            {
                AnalyzeParameters(context, methodDeclaration.ParameterList.Parameters);
            }
            else if (node is InvocationExpressionSyntax invocation)
            {
                AnalyzeParameters(context, invocation.ArgumentList.Arguments);
            }
        }

        private void AnalyzeParameters(SyntaxNodeAnalysisContext context, SeparatedSyntaxList<SyntaxNode> parameters)
        {
            if (parameters.Count > 1)
            {
                foreach (var pair in parameters.Zip(parameters.Skip(1), (first, second) => (first, second)))
                {
                    var firstLine = pair.first.GetLocation().GetLineSpan().EndLinePosition.Line;
                    var secondLine = pair.second.GetLocation().GetLineSpan().StartLinePosition.Line;
                    if (firstLine == secondLine)
                    {
                        // Report diagnostic for each pair of parameters/arguments on the same line
                        var diagnosticLocation = Location.Create(
                            context.Node.SyntaxTree,
                            TextSpan.FromBounds(pair.first.SpanStart, pair.second.Span.End));
                        var diagnostic = Diagnostic.Create(Rule, diagnosticLocation);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
