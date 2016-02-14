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
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumSwitchAnalyzer {
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumSwitchAnalyzerCodeFixProvider)), Shared]
    public class EnumSwitchAnalyzerCodeFixProvider : CodeFixProvider {

        //--- Constants ---
        private const string title = "Add missing enum fields";
        
        //--- Class Methods ---
        private static async Task<Document> AddMissingEnumFields(Document document, SyntaxToken typeDecl, CancellationToken cancellationToken) {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var node = typeDecl.Parent;
            IdentifierNameSyntax switchVariable;
            var missingMembers = EnumSwitchAnalysis.GetMissingEnumMembers(node, semanticModel, out switchVariable);
            if(missingMembers.Any()) {

                // get existing switchSections
                var existingSections = node.DescendantNodes().OfType<SwitchSectionSyntax>().ToImmutableArray();
                SwitchSectionSyntax defaultSection = null;
                if(existingSections.Any() && existingSections.Last().DescendantNodes().OfType<DefaultSwitchLabelSyntax>().Any()) {
                    defaultSection = existingSections.Last();
                    existingSections = existingSections.Take(existingSections.Length - 1).ToImmutableArray();
                }

                // generate missing case statements
                var switchVariableTypeInfo = semanticModel.GetTypeInfo(switchVariable);
                var newCaseStatements = missingMembers.Select(missingMember =>
                    SyntaxFactory.CaseSwitchLabel(
                        SyntaxFactory.Token(SyntaxKind.CaseKeyword),
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(switchVariableTypeInfo.Type.Name),
                            SyntaxFactory.IdentifierName(missingMember.Name)
                        ),
                        SyntaxFactory.Token(SyntaxKind.ColonToken))
                ).Select(caseSection => SyntaxFactory.SwitchSection(
                    SyntaxFactory.List<SwitchLabelSyntax>(new[] { caseSection }),
                    SyntaxFactory.List<StatementSyntax>().Add(
                        SyntaxFactory.ThrowStatement(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.ParseTypeName(nameof(NotImplementedException)),
                                SyntaxFactory.ArgumentList(),
                                null
                            )
                        )
                    )
                )).ToImmutableArray();

                // insert case statements after the last one
                var tree = await document.GetSyntaxTreeAsync(cancellationToken);
                var root = (CompilationUnitSyntax)tree.GetRoot(cancellationToken);
                var switchStatement = SyntaxFactory.SwitchStatement(
                        SyntaxFactory.IdentifierName(switchVariable.Identifier)
                            .WithLeadingTrivia(switchVariable.GetLeadingTrivia())
                            .WithTrailingTrivia(switchVariable.GetTrailingTrivia())
                    )
                        .WithSections(new SyntaxList<SwitchSectionSyntax>()
                            .AddRange(existingSections)
                            .AddRange(newCaseStatements)
                            .AddRange(defaultSection == null ? Enumerable.Empty<SwitchSectionSyntax>() : new [] { defaultSection }))
                        .WithLeadingTrivia(node.GetLeadingTrivia())
                        .WithTrailingTrivia(node.GetTrailingTrivia());
                root = root.ReplaceNode(node, switchStatement);
                return document.WithSyntaxRoot(root);
            }
            return document;
        }

        //--- Methods ---
        public sealed override ImmutableArray<string> FixableDiagnosticIds {
            get { return ImmutableArray.Create(EnumSwitchAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the switch statment identified by the diagnostic.
            var switchStatment = root.FindToken(diagnosticSpan.Start);

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddMissingEnumFields(context.Document, switchStatment, c),
                    equivalenceKey: title),
                diagnostic);
        }
    }
}