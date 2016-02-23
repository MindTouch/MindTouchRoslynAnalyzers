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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;

namespace MaterializeCollectionsAnalyzer {

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MaterializeCollectionsAnalyzerAnalyzer : DiagnosticAnalyzer {

        //--- Constants ---
        public const string DiagnosticId = "MaterializeCollectionsAnalyzer";
        private const string Category = "Performance";

        //--- Class Fields ---
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        //--- Class Methods ---
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
            var semanticModel = context.SemanticModel;
            var node = context.Node;

            // find all arguments
            var argNodes =
                node.DescendantNodes()
                .Where(x => x.Kind() == SyntaxKind.Argument)
                .Select(x => x.DescendantNodes().FirstOrDefault())
                .Where(x => x != null)
                .ToImmutableArray();
            if(argNodes.Any()) {
                foreach(var argument in argNodes) {
                    var typeInfo = semanticModel.GetTypeInfo(argument);

                    // check if the argument type is IEnumerable, and if it is abstract
                    if(IsCollectionType(typeInfo)) {
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
            if(IsCollectionType(typeInfo)) {
                if(ShouldReportOnCollectionNode(semanticModel, node)) {
                    ReportDiagnostic(context, node.GetLocation());
                }
            }
        }

        private static bool ShouldReportOnCollectionNode(SemanticModel semanticModel, SyntaxNode argument) {

            // if the variable is an invocation syntax we can assume that it was returned materialized
            switch(argument.Kind()) {
            case SyntaxKind.InvocationExpression:
                var methodCallInfo = semanticModel.GetSymbolInfo(argument);
                if(methodCallInfo.Symbol != null && methodCallInfo.Symbol.Kind == SymbolKind.Method) {
                    var mSymbol = (IMethodSymbol)methodCallInfo.Symbol;

                    // If the method is not an extension method, we assume it returned a materialized collection
                    if(!mSymbol.IsExtensionMethod) {
                        return false;
                    }
                }
                break;
            case SyntaxKind.IdentifierName:
                var identifierInfo = semanticModel.GetSymbolInfo(argument);

                // if this identifier came straight from a parameter, assume it is materialized
                if(identifierInfo.Symbol != null && identifierInfo.Symbol.Kind == SymbolKind.Parameter) {
                    return false;
                }

                // if this is a local identifier, look at where it is defined
                if(identifierInfo.Symbol != null && identifierInfo.Symbol.Kind == SymbolKind.Local) {
                    var declaration = identifierInfo.Symbol.DeclaringSyntaxReferences.FirstOrDefault();
                    var equalsExpression = declaration?.GetSyntax()
                        .ChildNodes()
                        .FirstOrDefault(x => x.Kind() == SyntaxKind.EqualsValueClause);
                    var firstChild = equalsExpression?.ChildNodes().FirstOrDefault();
                    if(firstChild != null) {
                        return ShouldReportOnCollectionNode(semanticModel, firstChild);
                    }
                }
                break;
            case SyntaxKind.SimpleMemberAccessExpression:
                
                // Assume that member accesses are returned materialized
                return false;
            }
            return true;
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location) {
            var diagnostic = Diagnostic.Create(Rule, location, "");
            context.ReportDiagnostic(diagnostic);
        }
        
        private static bool IsCollectionType(TypeInfo typeInfo) {
            if(typeInfo.Type == null || typeInfo.ConvertedType == null) {
                return false;
            }
            return MaterializedCollectionsUtils.IsCollection(typeInfo.ConvertedType) && typeInfo.Type.IsAbstract;
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
