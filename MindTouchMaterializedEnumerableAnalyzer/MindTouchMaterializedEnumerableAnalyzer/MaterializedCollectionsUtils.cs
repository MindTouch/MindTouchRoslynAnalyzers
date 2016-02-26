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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindTouchMaterializedEnumerableAnalyzer {
    internal class MaterializedCollectionsUtils {

        //--- Class Methods ---
        internal static bool IsCollection(ITypeSymbol symbol) => (symbol != null) && symbol.Interfaces.Any(x => (x.Name == typeof(IEnumerable).Name) || (x.Name == typeof(IEnumerable<>).Name));
        internal static bool IsAbstractCollectionType(TypeInfo typeInfo) => (typeInfo.Type != null) && (typeInfo.ConvertedType != null) && (typeInfo.Type.IsAbstract && IsCollection(typeInfo.ConvertedType));

        internal static bool ShouldReportOnCollectionNode(SemanticModel semanticModel, SyntaxNode argument) {
            const bool returnIfUnknown = false;

            // if the variable is an invocation syntax we can assume that it was returned materialized, if it's not an extension method
            switch(argument.Kind()) {
            case SyntaxKind.InvocationExpression:
                var methodCallInfo = semanticModel.GetSymbolInfo(argument);
                if((methodCallInfo.Symbol != null) && (methodCallInfo.Symbol.Kind == SymbolKind.Method)) {
                    var mSymbol = (IMethodSymbol)methodCallInfo.Symbol;
                    var typeInfo = semanticModel.GetTypeInfo(argument);

                    // If the method is not an extension method, we assume it returned a materialized collection
                    return IsAbstractCollectionType(typeInfo) && mSymbol.IsExtensionMethod && mSymbol.ContainingNamespace.ToDisplayString().Equals("System.Linq");
                }
                break;
            case SyntaxKind.IdentifierName:
                var identifierInfo = semanticModel.GetSymbolInfo(argument);

                // if this identifier came straight from a parameter, assume it is materialized
                if(identifierInfo.Symbol != null && identifierInfo.Symbol.Kind == SymbolKind.Parameter) {

                    //TODO(2016-02-24, yurig): if parameter is modified locally, we should warn
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
                        .LastOrDefault(x => (x.Left as IdentifierNameSyntax).Identifier.Text.Equals(((IdentifierNameSyntax)argument).Identifier.Text));
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
    }
}
