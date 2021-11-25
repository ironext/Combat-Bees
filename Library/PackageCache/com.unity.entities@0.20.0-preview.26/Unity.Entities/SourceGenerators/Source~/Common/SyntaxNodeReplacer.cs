using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.Common
{
    public class SyntaxNodeReplacer : CSharpSyntaxRewriter
    {
        readonly Dictionary<SyntaxNode, SyntaxNode> _replacements;

        public SyntaxNodeReplacer(Dictionary<SyntaxNode, SyntaxNode> replacements)
        {
            _replacements = replacements;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node != null && _replacements.TryGetValue(node, out var replacement))
            {
                if (replacement == null)
                    return null;
                return
                    node is StatementSyntax && !(node is BlockSyntax)
                        ? replacement.WithLineTrivia(node)
                        : replacement;
            }

            var newNode = base.Visit(node);

            return
                node is StatementSyntax && !(node is BlockSyntax)
                    ? newNode.WithLineTrivia(node)
                    : newNode;
        }
    }
}
