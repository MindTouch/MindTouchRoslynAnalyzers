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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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
                .Select(x => x.DescendantNodes().First())
                .ToImmutableArray();
            if(argNodes.Any()) {
                foreach(var argument in argNodes) {
                    var typeInfo = semanticModel.GetTypeInfo(argument);

                    // check if the argument type is IEnumerable, and if it is abstract
                    if(MaterializedCollectionsUtils.IsCollection(typeInfo.ConvertedType) && typeInfo.Type.IsAbstract) {
                        var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), "");
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static void AnalyzeReturnStatement(SyntaxNodeAnalysisContext context) {
            var semanticModel = context.SemanticModel;
            var node = context.Node.DescendantNodes().First();
            var typeInfo = semanticModel.GetTypeInfo(node);

            // check if the argument type is IEnumerable, and if it is abstract
            if(MaterializedCollectionsUtils.IsCollection(typeInfo.ConvertedType) && typeInfo.Type.IsAbstract) {
                var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), "");
                context.ReportDiagnostic(diagnostic);
            }
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
