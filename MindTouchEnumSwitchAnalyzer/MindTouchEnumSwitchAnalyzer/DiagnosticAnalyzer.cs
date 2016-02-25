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

namespace MindTouchEnumSwitchAnalyzer {

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EnumSwitchAnalyzerAnalyzer : DiagnosticAnalyzer {

        //--- Constants ---
        public const string DIAGNOSTIC_ID = "EnumSwitchAnalyzer";
        private const string Category = "Correctness";

        //--- Class Fields ---
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DIAGNOSTIC_ID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        //--- Class Methods ---
        private static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context) {
            var semanticModel = context.SemanticModel;
            IdentifierNameSyntax switchVariable;
            var missingMembers = EnumSwitchAnalysis.GetMissingEnumMembers((SwitchStatementSyntax)context.Node, semanticModel, out switchVariable);
            if(missingMembers.Any()) {
                var switchVariableTypeInfo = semanticModel.GetTypeInfo(switchVariable);
                var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), switchVariableTypeInfo.Type.Name, string.Join(", ", missingMembers.Select(x => x.Name).ToImmutableArray()));
                context.ReportDiagnostic(diagnostic);
            }
        }

        //--- Properties ---
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        //--- Methods ---
        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
    }
}
