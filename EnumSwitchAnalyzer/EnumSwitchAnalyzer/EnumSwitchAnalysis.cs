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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumSwitchAnalyzer {
    internal class EnumSwitchAnalysis {

        //--- Class Methods ---
        public static IEnumerable<ISymbol> GetMissingEnumMembers(SyntaxNode switchNode, SemanticModel semanticModel, out IdentifierNameSyntax switchVariable) {
            switchVariable = switchNode.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            var switchVariableTypeInfo = semanticModel.GetTypeInfo(switchVariable);

            // check if we are switching over an enum
            if(switchVariableTypeInfo.Type.TypeKind == TypeKind.Enum) {

                // get all the enum values
                var enumMembers = switchVariableTypeInfo.Type.GetMembers().Where(x => x.Kind == SymbolKind.Field).ToImmutableArray();

                // get all case statements
                var caseSwitchLabels = switchNode
                    .DescendantNodes().OfType<CaseSwitchLabelSyntax>()
                    .Select(x => x.DescendantNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault())
                    .Where(x => x != null)
                    .Select(x => semanticModel.GetSymbolInfo(x).Symbol)
                    .ToImmutableHashSet();

                // make sure we have all cases covered
                return enumMembers.Where(x => !caseSwitchLabels.Contains(x)).ToImmutableArray();
            }
            return Enumerable.Empty<ISymbol>();
        }
    }
}
