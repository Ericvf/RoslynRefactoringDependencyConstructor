using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DependencyConstructor
{
    public class DependencyConstructorCodeRefactoring
    {
        internal struct DependencyMemberDeclaration
        {
            internal TypeSyntax fieldType;
            internal string fieldName;
            internal string parameterName;
        }

        public static ClassDeclarationSyntax ComputeRefactorings(ClassDeclarationSyntax currentClassNode)
        {
            // Get the current constructor, if it has one
            var currentConstructorNode = currentClassNode.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault();

            // Create list of current constructor argument names
            var currentConstructorArguments = currentConstructorNode?
                .ParameterList.DescendantNodes()
                .OfType<ParameterSyntax>()
                .Select(p => p.Identifier.Text);

            // Create a list of all the readonly members
            var readonlyMembers = GetReadonlyMembers(currentClassNode);

            // Create a list of the new constructor arguments, exclude those already present
            var newParameterSyntaxList = (
                from vars in readonlyMembers
                where currentConstructorArguments == null || !currentConstructorArguments.Contains(vars.parameterName)
                select Parameter(Identifier(vars.parameterName))
                    .WithType(vars.fieldType));

            IEnumerable<string> currentConstructorAssignedMembers = new List<string>();
            if (currentConstructorNode != null)
            {
                var readonlyMemberNames = readonlyMembers.Select(c => c.fieldName);

                // Create a list of all members names that are already assigned
                currentConstructorAssignedMembers = GetMemberAssignments(currentConstructorNode, readonlyMemberNames);
            }

            // Create a list of the new constructor statements, exclude those already assigned
            var newStatementSyntaxList =
                from vars in readonlyMembers
                where !currentConstructorAssignedMembers.Contains(vars.fieldName)
                let leftMember = IdentifierName(vars.fieldName)
                let rightMember = IdentifierName(vars.parameterName)
                let expression = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, leftMember, rightMember)
                select ExpressionStatement(expression);

            // Check if we need to create a new constructor
            if (currentConstructorNode == null)
            {
                // Create new constructor
                var newConstructorNode = ConstructorDeclaration(currentClassNode.Identifier.ValueText)
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(ParameterList(SeparatedList(newParameterSyntaxList)))
                    .WithBody(Block(newStatementSyntaxList)
                        .WithTrailingTrivia(CarriageReturnLineFeed));

                // Add it to the class 
                currentClassNode = currentClassNode.AddMembers(newConstructorNode);
            }
            else
            {
                var currentConstructorTrivia = currentConstructorNode.Body.GetTrailingTrivia();

                // Add the constructor arguments and statements
                var newConstructorNode = currentConstructorNode.AddParameterListParameters(newParameterSyntaxList.ToArray());
                newConstructorNode = newConstructorNode.AddBodyStatements(newStatementSyntaxList.ToArray());

                // Replace the constructor in the class
                currentClassNode = currentClassNode.ReplaceNode(currentConstructorNode, newConstructorNode);
            }
            
            return currentClassNode;
        }

        public static bool HasRefactorings(ClassDeclarationSyntax currentClassNode)
        {
            // Create a list of all the readonly members
            var readonlyMembers = GetReadonlyMembers(currentClassNode);

            // If no readonly members are found, return
            if (readonlyMembers.Count() == 0)
                return false;

            // Find current constructor
            var currentConstructorNode = currentClassNode.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault();

            // If no constructor is found, return
            if (currentConstructorNode == null)
                return true;

            // Find constructor arguments, if any
            var currentConstructorArguments = currentConstructorNode?
                .ParameterList.DescendantNodes()
                .OfType<ParameterSyntax>()
                .Select(p => p.Identifier.Text);

            // Determine members not part of the constructor arguments
            var membersWithoutConstructorArguments =
                from vars in readonlyMembers
                where currentConstructorArguments == null || !currentConstructorArguments.Contains(vars.parameterName)
                select vars.parameterName;

            // If unmapped members are found, return
            if (membersWithoutConstructorArguments.Count() > 0)
                return true;

            // Determine members not assigned in constructor
            var readonlyMemberNames = readonlyMembers.Select(c => c.fieldName);
            var currentConstructorAssignedMembers = GetMemberAssignments(currentConstructorNode, readonlyMemberNames);

            var membersWithoutConstructorAssignments =
               from vars in readonlyMembers
               where !currentConstructorAssignedMembers.Contains(vars.fieldName)
               select vars.fieldName;

            // If unassigned members are found, return
            return membersWithoutConstructorAssignments.Count() > 0;
        }

        private static IEnumerable<string> GetMemberAssignments(ConstructorDeclarationSyntax currentConstructorNode, IEnumerable<string> dependencyMemberNames)
        {
            return from statement in currentConstructorNode.Body.Statements
                   let expression = statement as ExpressionStatementSyntax
                   let assignment = expression?.Expression as AssignmentExpressionSyntax
                   let leftMember = assignment?.Left as IdentifierNameSyntax
                   where leftMember != null && dependencyMemberNames.Contains(leftMember.Identifier.ValueText)
                   select leftMember.Identifier.ValueText;
        }

        private static IEnumerable<DependencyMemberDeclaration> GetReadonlyMembers(ClassDeclarationSyntax currentClassNode)
        {
            return from memberField in currentClassNode.DescendantNodes().OfType<FieldDeclarationSyntax>()
                   where memberField.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword))
                   let fieldType = memberField.Declaration.Type
                   from variable in memberField.Declaration.Variables
                   let fieldName = variable.Identifier.ValueText
                   let parameterName = fieldName.StartsWith("_")
                        ? fieldName.Substring(1)
                        : "_" + fieldName
                   select new DependencyMemberDeclaration
                   {
                       fieldType = fieldType,
                       fieldName = fieldName,
                       parameterName = parameterName
                   };
        }
    }
}