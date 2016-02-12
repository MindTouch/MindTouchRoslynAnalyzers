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
using Microsoft.CodeAnalysis.Diagnostics;

namespace EnumSwitchAnalyzer {

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EnumSwitchAnalyzerAnalyzer : DiagnosticAnalyzer {

        //--- Constants ---
        public const string DiagnosticId = "EnumSwitchAnalyzer";

        //--- Class Fields ---
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Correctness";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        //--- Class Methods ---
        private static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context) {
            var semanticModel = context.SemanticModel;
            TypeInfo switchVariableTypeInfo;
            var missingMembers = EnumSwitchAnalysis.GetMissingEnumMembers(context.Node, semanticModel, out switchVariableTypeInfo);
            if(missingMembers.Any()) {
                var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), switchVariableTypeInfo.Type.Name, string.Join(", ", missingMembers.Select(x => x.Name).ToImmutableArray()));
                context.ReportDiagnostic(diagnostic);
            }
        }

        //--- Methods ---
        public override void Initialize(AnalysisContext context) {
            context.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
        }
    }
}
