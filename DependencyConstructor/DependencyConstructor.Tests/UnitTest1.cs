using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DependencyConstructor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DependencyConstructorTests
{
    [TestClass]
    public class UnitTest1
    {
        public async Task<ClassDeclarationSyntax> Execute(SyntaxTree syntaxTree)
        {
            var root = await syntaxTree.GetRootAsync();
            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Single();

            var newClassDeclaration = GenerateDependencyConstructorHelpers.GenerateDependencyConstructor(classDeclaration);
            Debug.WriteLine(newClassDeclaration.ToString());
            return newClassDeclaration;
        }

        [TestMethod]
        public async Task GenerateNew()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
class ProgramTests
{
    private readonly A _a1, _a2, a3, a4;
    public ProgramTests()
    {

    }
}");

            //[76..88)
            var span = TextSpan.FromBounds(76, 88);
            var newClass = await Execute(syntaxTree);
            var constructor = newClass.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Single();

            Assert.IsTrue(constructor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

            var parameterCount = constructor.ParameterList.Parameters.Count;
            Assert.IsTrue(parameterCount == 4);
        }

        [TestMethod]
        public async Task AddParameters()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
            class ProgramTests
            {
                private readonly A _a1, _a2, _a3, a4;
                public ProgramTests(A a1, A a2, string x)
                {
                    _a2 = a2;
                    _a1 = a1;
                }
            }
            ");

            var newClass = await Execute(syntaxTree);
            var constructor = newClass.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Single();

            var parameterCount = constructor.ParameterList.Parameters.Count;
            Assert.IsTrue(parameterCount == 5);
        }

        [TestMethod]
        public async Task AddStatements()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
            class ProgramTests
            {
                private readonly A _a1, _a2, _a3, a4;

                // this is a comment
                public ProgramTests(A a1, A a2)
                {
                    // this is a comment
                    _a2 = a2;
                    _a1 = a1;
                    string x = ""x"";
                }
                
                public void Test(){
                }
            }
            ");

            var newClass = await Execute(syntaxTree);
            var constructor = newClass.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Single();

            var parameterCount = constructor.ParameterList.Parameters.Count;
            Assert.IsTrue(parameterCount == 4);

            var statementCount = constructor.Body.Statements.Count;
            Assert.IsTrue(statementCount == 5);
        }


        [TestMethod]
        public async Task TestSyncMethod()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            class ProgramTests
            {
                public async Task Test()
                {
                    string x = null;
                }

                public string anotherMethod()
                {
                }
            }
            ");
            var span = TextSpan.FromBounds(119, 123);
            var root = await syntaxTree.GetRootAsync();
            var methodDeclaration = root.FindNode(span);

            if (methodDeclaration is MethodDeclarationSyntax)
            {
                var newMethod = TaskHelpers.GenerateSync(methodDeclaration as MethodDeclarationSyntax);
                var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
                Debug.WriteLine(newRoot.ToString());
            }
        }

        [TestMethod]
        public async Task TestGenericSyncMethod()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            class ProgramTests
            {
                public async Task<string> Test()
                {
                    string x = null;
                }

                public string anotherMethod()
                {
                }
            }
            ");
            var span = TextSpan.FromBounds(119, 123);
            var root = await syntaxTree.GetRootAsync();
            var methodDeclaration = root.FindNode(span);

            if (methodDeclaration is MethodDeclarationSyntax)
            {
                var newMethod = TaskHelpers.GenerateSync(methodDeclaration as MethodDeclarationSyntax);
                var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
                Debug.WriteLine(newRoot.ToString());
            }
        }

        [TestMethod]
        public async Task TestGeneric2SyncMethod()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            class ProgramTests
            {
                public async Task<Task<string>> Test()
                {
                    string x = null;
                }

                public string anotherMethod()
                {
                }
            }
            ");
            var span = TextSpan.FromBounds(119, 123);
            var root = await syntaxTree.GetRootAsync();
            var methodDeclaration = root.FindNode(span);

            if (methodDeclaration is MethodDeclarationSyntax)
            {
                var newMethod = TaskHelpers.GenerateSync(methodDeclaration as MethodDeclarationSyntax);
                var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
                Debug.WriteLine(newRoot.ToString());
            }
        }

        [TestMethod]
        public async Task TestASyncMethod()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            class ProgramTests
            {
                public void Test()
                {
                    string x = null;
                }

                public string anotherMethod()
                {
                }
            }
            ");
            var span = TextSpan.FromBounds(113, 119);
            var root = await syntaxTree.GetRootAsync();
            var methodDeclaration = root.FindNode(span);

            if (methodDeclaration is MethodDeclarationSyntax)
            {
                var newMethod = TaskHelpers.GenerateAsync(methodDeclaration as MethodDeclarationSyntax);
                var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
                Debug.WriteLine(newRoot.ToString());
            }
        }

        [TestMethod]
        public async Task TestGenericASyncMethod()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            class ProgramTests
            {
                public string Test()
                {
                    string x = null;
                }

                public string anotherMethod()
                {
                }
            }
            ");
            var span = TextSpan.FromBounds(113, 119);
            var root = await syntaxTree.GetRootAsync();
            var methodDeclaration = root.FindNode(span);

            if (methodDeclaration is MethodDeclarationSyntax)
            {
                var newMethod = TaskHelpers.GenerateAsync(methodDeclaration as MethodDeclarationSyntax);
                var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
                Debug.WriteLine(newRoot.ToString());
            }
        }
    }
}
