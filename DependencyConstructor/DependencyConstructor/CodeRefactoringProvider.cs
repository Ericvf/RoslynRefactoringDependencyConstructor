using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyConstructor
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(DependencyConstructorCodeRefactoringProvider)), Shared]
    public class DependencyConstructorCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            ClassDeclarationSyntax classDeclaration = null;
            switch (node.Kind())
            {
                case SyntaxKind.ConstructorDeclaration:
                    classDeclaration = node.Parent as ClassDeclarationSyntax;
                    break;

                case SyntaxKind.ClassDeclaration:
                    classDeclaration = node as ClassDeclarationSyntax;
                    break;
            }

            if (classDeclaration != null)
            {
                if (DependencyConstructorCodeRefactoring.HasRefactorings(classDeclaration))
                {
                    var action = CodeAction.Create("Resolve unassigned dependencies", c =>
                        GenerateDependencyConstructorAction(context.Document, classDeclaration, c));

                    context.RegisterRefactoring(action);
                }
            }
        }
        
        public async Task<Document> GenerateDependencyConstructorAction(Document document, ClassDeclarationSyntax oldNode, CancellationToken cancellationToken)
        {
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newNode = DependencyConstructorCodeRefactoring.ComputeRefactorings(oldNode);
            var newRoot = oldRoot.ReplaceNode(oldNode, newNode);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}