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
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindTouch.RoslynAnalyzers.MindTouchMaterializedEnumerableAnalyzer {

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MindTouchMaterializeCollectionsAnalyzerCodeFixProvider)), Shared]
    public class MindTouchMaterializeCollectionsAnalyzerCodeFixProvider : CodeFixProvider {

        //--- Constants ---
        private const string title = "Materialize using ToArray()";

        //--- Class Methods ---
        private static async Task<Document> MakeArray(Document document, SyntaxToken typeDecl, CancellationToken cancellationToken) {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            // check to see if this is an argument to a method call
            SyntaxNode node = null;
            if(typeDecl.Parent.Ancestors().Any(x => x.Kind() == SyntaxKind.Argument)) {
                node = typeDecl.Parent.Ancestors()
                 .Where(x => x.Kind() == SyntaxKind.Argument)
                 .Select(x => x.DescendantNodes().FirstOrDefault())
                 .FirstOrDefault();

                // check to see if we are dealing with a return statement
            } else if(typeDecl.Parent.Ancestors().Any(x => x.Kind() == SyntaxKind.ReturnStatement)) {
                node = typeDecl.Parent.Ancestors().Where(x => x.Kind() == SyntaxKind.ReturnStatement).Select(x => x.DescendantNodes().FirstOrDefault()).FirstOrDefault();
            }
            if(node == null) {

                // we did not recognize this node, do nothing
                return document;
            }

            var typeInfo = semanticModel.GetTypeInfo(node);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            var root = (CompilationUnitSyntax)tree.GetRoot(cancellationToken);
            if(MaterializedCollectionsUtils.IsCollection(typeInfo.ConvertedType) && typeInfo.Type.IsAbstract) {

                // generate a .ToArray() call around the argument
                var newNode = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        (ExpressionSyntax)node,
                        SyntaxFactory.IdentifierName(@"ToArray")
                    ),
                    SyntaxFactory.ArgumentList()
                );
                var newRoot = root.ReplaceNode(node, newNode);
                return document.WithSyntaxRoot(newRoot);
            }
            return document;
        }

        //--- Methods ---
        public sealed override ImmutableArray<string> FixableDiagnosticIds {
            get { return ImmutableArray.Create(MindTouchMaterializedEnumerableAnalyzer.DIAGNOSTIC_ID); }
        }

        public sealed override FixAllProvider GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // find all argument identified by the diagnostic
            var argument = root.FindToken(diagnosticSpan.Start);

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => MakeArray(context.Document, argument, c),
                    equivalenceKey: title),
                diagnostic);
        }
    }
}