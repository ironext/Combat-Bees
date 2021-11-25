using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen
{
    public class JobEntitySyntaxReceiver : ISyntaxReceiver
    {
        public Dictionary<SyntaxTree, List<StructDeclarationSyntax>> JobCandidatesBySyntaxTree = new Dictionary<SyntaxTree, List<StructDeclarationSyntax>>();
        public List<StructDeclarationSyntax> NonPartialJobCandidates = new List<StructDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case StructDeclarationSyntax structDeclarationSyntax:
                {
                    if (structDeclarationSyntax.BaseList == null)
                        break;

                    bool isIJobEntity = false;
                    foreach (var baseType in structDeclarationSyntax.BaseList.Types)
                    {
                        if (baseType.Type is IdentifierNameSyntax identifierNameSyntax && identifierNameSyntax.Identifier.ValueText == "IJobEntity")
                        {
                            isIJobEntity = true;
                            break;
                        }
                    }

                    if (!isIJobEntity)
                    {
                        break;
                    }

                    bool isPartial = structDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

                    if (!isPartial)
                    {
                        NonPartialJobCandidates.Add(structDeclarationSyntax);
                        break;
                    }

                    var syntaxTree = structDeclarationSyntax.SyntaxTree;
                    JobCandidatesBySyntaxTree.Add(syntaxTree, structDeclarationSyntax);
                    break;
                }
            }
        }
    }
}
