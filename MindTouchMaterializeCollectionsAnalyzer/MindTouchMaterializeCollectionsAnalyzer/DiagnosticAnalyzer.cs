/**
 * Copyright (c) 2016 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MindTouchMaterializeCollectionsAnalyzer {

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MaterializeCollectionsAnalyzerAnalyzer : DiagnosticAnalyzer {

        //--- Constants ---
        public const string DiagnosticId = "MaterializeCollectionsAnalyzer";
        private const string Category = "Performance";

        //--- Class Fields ---
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        //--- Class Methods ---
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
            var semanticModel = context.SemanticModel;
            var node = context.Node;

            // check to see if the invocation target is in the current assembly
            var invocationInfo = context.SemanticModel.GetSymbolInfo(node);
            if(invocationInfo.Symbol == null) {
                return;
            }
            if(!invocationInfo.Symbol.ContainingAssembly.Name.Equals(context.SemanticModel.Compilation.AssemblyName)) {
                return;
            }

            // find all arguments
            var argNodes =
                node.DescendantNodes()
                .Where(x => x.Kind() == SyntaxKind.Argument)
                .Select(x => x.DescendantNodes().FirstOrDefault())
                .Where(x => x != null)
                .ToImmutableArray();
            if(argNodes.Any()) {
                foreach(var argument in argNodes) {

                    // do not evaluate out parameters on method invocations
                    if(argument.Parent.GetFirstToken().Kind() == SyntaxKind.OutKeyword) {
                        continue;
                    }
                    var typeInfo = semanticModel.GetTypeInfo(argument);
                    
                    // check if the argument type is IEnumerable, and if it is abstract
                    if(IsAbstractCollectionType(typeInfo)) {
                        if(ShouldReportOnCollectionNode(semanticModel, argument)) {
                            ReportDiagnostic(context, argument.GetLocation());
                        }
                    }
                }
            }
        }

        private static void AnalyzeReturnStatement(SyntaxNodeAnalysisContext context) {
            var semanticModel = context.SemanticModel;
            var node = context.Node.DescendantNodes().FirstOrDefault();
            if(node == null) {
                return;
            }
            var typeInfo = semanticModel.GetTypeInfo(node);
            if(IsAbstractCollectionType(typeInfo)) {
                if(ShouldReportOnCollectionNode(semanticModel, node)) {
                    ReportDiagnostic(context, node.GetLocation());
                }
            }
        }

        private static bool ShouldReportOnCollectionNode(SemanticModel semanticModel, SyntaxNode argument) {
            const bool returnIfUnknown = false;

            // if the variable is an invocation syntax we can assume that it was returned materialized, if it's not an extension method
            switch(argument.Kind()) {
            case SyntaxKind.InvocationExpression:
                var methodCallInfo = semanticModel.GetSymbolInfo(argument);
                if(methodCallInfo.Symbol != null && methodCallInfo.Symbol.Kind == SymbolKind.Method) {
                    var mSymbol = (IMethodSymbol)methodCallInfo.Symbol;

                    // If the method is not an extension method, we assume it returned a materialized collection
                    return mSymbol.IsExtensionMethod && mSymbol.ContainingNamespace.ToDisplayString().Equals("System.Linq");
                }
                break;
            case SyntaxKind.IdentifierName:
                var identifierInfo = semanticModel.GetSymbolInfo(argument);

                // if this identifier came straight from a parameter, assume it is materialized
                if(identifierInfo.Symbol != null && identifierInfo.Symbol.Kind == SymbolKind.Parameter) {

                    //TODO: check if parameter was re-assigned
                    return false;
                }

                // if this is a local identifier, look at where it is defined
                if(identifierInfo.Symbol != null && identifierInfo.Symbol.Kind == SymbolKind.Local) {
                    var declaration = identifierInfo.Symbol.DeclaringSyntaxReferences.FirstOrDefault();

                    // if the declaration is an equals expression, dive into it.
                    var equalsExpression = declaration?.GetSyntax()
                        .ChildNodes()
                        .FirstOrDefault(x => x.Kind() == SyntaxKind.EqualsValueClause);
                    var firstChild = equalsExpression?.ChildNodes().FirstOrDefault();
                    if(firstChild != null) {
                        return ShouldReportOnCollectionNode(semanticModel, firstChild);
                    }

                    // if the variable was assigned somewhere else, find it
                    var containingClass = declaration?.GetSyntax().FirstAncestorOrSelf<MethodDeclarationSyntax>();
                    var localAssignment = containingClass?.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                        .Where(x => x.Left.IsKind(SyntaxKind.IdentifierName))
                        .FirstOrDefault(x =>  (x.Left as IdentifierNameSyntax).Identifier.Text.Equals(((IdentifierNameSyntax)argument).Identifier.Text));
                    if(localAssignment != null) {
                        return ShouldReportOnCollectionNode(semanticModel, localAssignment.Right);
                    }
                }
                break;
            case SyntaxKind.SimpleMemberAccessExpression:
                
                // Assume that member accesses are returned materialized
                return false;
            }
            return returnIfUnknown;
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location) {
            var diagnostic = Diagnostic.Create(Rule, location, "");
            context.ReportDiagnostic(diagnostic);
        }
        
        private static bool IsAbstractCollectionType(TypeInfo typeInfo) {
            if(typeInfo.Type == null || typeInfo.ConvertedType == null) {
                return false;
            }
            return typeInfo.Type.IsAbstract && MaterializedCollectionsUtils.IsCollection(typeInfo.ConvertedType);
        }

        //--- Fields ---
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        //--- Methods ---
        public override void Initialize(AnalysisContext context) {
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeReturnStatement, SyntaxKind.ReturnStatement);
        }
    }
}
