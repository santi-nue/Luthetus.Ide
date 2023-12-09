﻿using System.Collections.Immutable;

namespace Luthetus.TextEditor.Tests.Basis.CompilerServices.Syntax.SyntaxNodes;

public sealed record UnaryOperatorNodeTests
{
    public UnaryOperatorNode(
        TypeClauseNode operandTypeClauseNode,
        ISyntaxToken operatorToken,
        TypeClauseNode resultTypeClauseNode)
    {
        OperandTypeClauseNode = operandTypeClauseNode;
        OperatorToken = operatorToken;
        ResultTypeClauseNode = resultTypeClauseNode;

        ChildBag = new ISyntax[]
        {
            OperandTypeClauseNode,
            OperatorToken,
            ResultTypeClauseNode,
        }
        .ToImmutableArray();
    }

    public TypeClauseNode OperandTypeClauseNode { get; }
    public ISyntaxToken OperatorToken { get; }
    public TypeClauseNode ResultTypeClauseNode { get; }

    public ImmutableArray<ISyntax> ChildBag { get; }

    public bool IsFabricated { get; init; }
    public SyntaxKind SyntaxKind => SyntaxKind.UnaryOperatorNode;
}