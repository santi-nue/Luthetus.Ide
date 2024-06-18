using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.Tokens;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.Nodes;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.Nodes.Interfaces;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.Nodes.Enums;
using System.Collections.Immutable;

namespace Luthetus.CompilerServices.Lang.CSharp.ParserCase.Internals;

public static class ParseTokens
{
    public static void ParseNumericLiteralToken(
        NumericLiteralToken consumedNumericLiteralToken,
        ParserModel model)
    {
        // The handle expression won't see this token unless backtracked.
        model.TokenWalker.Backtrack();
        ParseOthers.HandleExpression(
            null,
            null,
            null,
            null,
            null,
            null,
            model);

        model.CurrentCodeBlockBuilder.ChildList.Add(
            (IExpressionNode)model.SyntaxStack.Pop());
    }

    public static void ParseStringLiteralToken(
        StringLiteralToken consumedStringLiteralToken,
        ParserModel model)
    {
        // The handle expression won't see this token unless backtracked.
        model.TokenWalker.Backtrack();
        ParseOthers.HandleExpression(
            null,
            null,
            null,
            null,
            null,
            null,
            model);

        model.CurrentCodeBlockBuilder.ChildList.Add(
            (IExpressionNode)model.SyntaxStack.Pop());
    }

    public static void ParsePreprocessorDirectiveToken(
        PreprocessorDirectiveToken consumedPreprocessorDirectiveToken,
        ParserModel model)
    {
        var consumedToken = model.TokenWalker.Consume();

        if (consumedToken.SyntaxKind == SyntaxKind.LibraryReferenceToken)
        {
            var preprocessorLibraryReferenceStatement = new PreprocessorLibraryReferenceStatementNode(
                consumedPreprocessorDirectiveToken,
                consumedToken);

            model.CurrentCodeBlockBuilder.ChildList.Add(preprocessorLibraryReferenceStatement);
            return;
        }
        else
        {
            model.DiagnosticBag.ReportTodoException(
                consumedToken.TextSpan,
                $"Implement {nameof(ParsePreprocessorDirectiveToken)}");
        }
    }

    public static void ParseIdentifierToken(
        IdentifierToken consumedIdentifierToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) && syntax is AmbiguousIdentifierNode)
            ResolveAmbiguousIdentifier((AmbiguousIdentifierNode)model.SyntaxStack.Pop(), model);

        if (TryParseTypedIdentifier(consumedIdentifierToken, model))
            return;

        if (TryParseConstructorDefinition(consumedIdentifierToken, model))
            return;

        if (TryParseVariableAssignment(consumedIdentifierToken, model))
            return;

        if (TryParseGenericTypeOrFunctionInvocation(consumedIdentifierToken, model))
            return;

        if (TryParseReference(consumedIdentifierToken, model))
            return;

        return;
    }

    private static bool TryParseGenericArguments(ParserModel model)
    {
        if (model.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenAngleBracketToken)
        {
            ParseTypes.HandleGenericArguments(
                (OpenAngleBracketToken)model.TokenWalker.Consume(),
                model);

            return true;
        }
        else
        {
            return false;
        }
    }

    private static bool TryParseGenericParameters(
        ParserModel model,
        out GenericParametersListingNode? genericParametersListingNode)
    {
        if (SyntaxKind.OpenAngleBracketToken == model.TokenWalker.Current.SyntaxKind)
        {
            ParseTypes.HandleGenericParameters(
                (OpenAngleBracketToken)model.TokenWalker.Consume(),
                model);

            genericParametersListingNode = (GenericParametersListingNode?)model.SyntaxStack.Pop();
            return true;
        }
        else
        {
            genericParametersListingNode = null;
            return false;
        }
    }

    private static bool TryParseConstructorDefinition(
        IdentifierToken consumedIdentifierToken,
        ParserModel model)
    {
        if (model.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenParenthesisToken &&
            model.CurrentCodeBlockBuilder.CodeBlockOwner is not null &&
            model.CurrentCodeBlockBuilder.CodeBlockOwner.SyntaxKind == SyntaxKind.TypeDefinitionNode)
        {
            ParseFunctions.HandleConstructorDefinition(consumedIdentifierToken, model);
            return true;
        }

        return false;
    }

    private static bool TryParseTypedIdentifier(
        IdentifierToken consumedIdentifierToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) && syntax is TypeClauseNode typeClauseNode)
        {
            // The variable 'genericArgumentsListingNode' is here for
            // when the syntax is determined to be a function definition.
            // In this case, the typeClauseNode would be the function's return type.
            // Yet, there may still be generic arguments to the function.
            var genericArgumentsListingNode = (GenericArgumentsListingNode?)null;

            if (TryParseGenericArguments(model) &&
                model.SyntaxStack.Peek().SyntaxKind == SyntaxKind.GenericArgumentsListingNode)
            {
                genericArgumentsListingNode = (GenericArgumentsListingNode)model.SyntaxStack.Pop();
            }

            if (TryParseFunctionDefinition(
                consumedIdentifierToken,
                typeClauseNode,
                genericArgumentsListingNode,
                model))
            {
                return true;
            }

            if (TryParseVariableDeclaration(
                typeClauseNode,
                consumedIdentifierToken,
                model))
            {
                return true;
            }
        }
        
        return false;
    }

    private static bool TryParseReference(
        IdentifierToken consumedIdentifierToken,
        ParserModel model)
    {
        var text = consumedIdentifierToken.TextSpan.GetText();

        if (model.Binder.NamespaceGroupNodes.TryGetValue(text, out var namespaceGroupNode) &&
            namespaceGroupNode is not null)
        {
            ParseOthers.HandleNamespaceReference(consumedIdentifierToken, namespaceGroupNode, model);
            return true;
        }
        else
        {
            if (model.Binder.TryGetVariableDeclarationHierarchically(
                    text,
                    model.BinderSession.CurrentScope,
                    out var variableDeclarationStatementNode) &&
                variableDeclarationStatementNode is not null)
            {
                ParseVariables.HandleVariableReference(consumedIdentifierToken, model);
                return true;
            }
            else
            {
                // 'static class identifier' OR 'undeclared-variable reference'
                if (model.Binder.TryGetTypeDefinitionHierarchically(
                        text,
                        model.BinderSession.CurrentScope,
                        out var typeDefinitionNode) &&
                    typeDefinitionNode is not null)
                {
                    ParseTypes.HandleStaticClassIdentifier(consumedIdentifierToken, model);
                    return true;
                }
                else
                {
                    ParseTypes.HandleUndefinedTypeOrNamespaceReference(consumedIdentifierToken, model);
                    return true;
                }
            }
        }
    }

    private static bool TryParseVariableAssignment(
        IdentifierToken consumedIdentifierToken,
        ParserModel model)
    {
        if (model.TokenWalker.Current.SyntaxKind == SyntaxKind.EqualsToken)
        {
            ParseVariables.HandleVariableAssignment(
                consumedIdentifierToken,
                (EqualsToken)model.TokenWalker.Consume(),
                model);
            return true;
        }

        return false;
    }

    private static bool TryParseGenericTypeOrFunctionInvocation(
        IdentifierToken consumedIdentifierToken,
        ParserModel model)
    {
        if (model.TokenWalker.Current.SyntaxKind != SyntaxKind.OpenParenthesisToken &&
            model.TokenWalker.Current.SyntaxKind != SyntaxKind.OpenAngleBracketToken)
        {
            return false;
        }

        if (TryParseGenericParameters(model, out var genericParametersListingNode))
        {
            if (model.TokenWalker.Current.SyntaxKind != SyntaxKind.OpenParenthesisToken)
            {
                // Generic type
                var typeClauseNode = new TypeClauseNode(
                    consumedIdentifierToken,
                    null,
                    genericParametersListingNode);

                model.Binder.BindTypeClauseNode(typeClauseNode, model);
                model.SyntaxStack.Push(typeClauseNode);
                return true;
            }
        }

        // Function invocation
        ParseFunctions.HandleFunctionInvocation(
            consumedIdentifierToken,
            genericParametersListingNode,
            model);

        return true;
    }

    private static bool TryParseFunctionDefinition(
        IdentifierToken consumedIdentifierToken,
        TypeClauseNode consumedTypeClauseNode,
        GenericArgumentsListingNode? consumedGenericArgumentsListingNode,
        ParserModel model)
    {
        if (model.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenParenthesisToken)
        {
            ParseFunctions.HandleFunctionDefinition(
                consumedIdentifierToken,
                consumedTypeClauseNode,
                consumedGenericArgumentsListingNode,
                model);

            return true;
        }

        return false;
    }

    private static bool TryParseVariableDeclaration(
        TypeClauseNode consumedTypeClauseNode,
        IdentifierToken consumedIdentifierToken,
        ParserModel model)
    {
        var isLocalOrField = model.TokenWalker.Current.SyntaxKind == SyntaxKind.StatementDelimiterToken ||
                             model.TokenWalker.Current.SyntaxKind == SyntaxKind.EqualsToken;

        var isLambda = model.TokenWalker.Next.SyntaxKind == SyntaxKind.CloseAngleBracketToken;

        var variableKind = (VariableKind?)null;

        if (isLocalOrField && !isLambda)
        {
            if (model.CurrentCodeBlockBuilder.CodeBlockOwner is TypeDefinitionNode)
                variableKind = VariableKind.Field;
            else
                variableKind = VariableKind.Local;
        }
        else if (isLambda)
        {
            // Property (expression bound)
            variableKind = VariableKind.Property;
        }
        else if (model.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenBraceToken)
        {
            // Property
            variableKind = VariableKind.Property;
        }

        if (variableKind is not null)
        {
            ParseVariables.HandleVariableDeclaration(
                consumedTypeClauseNode,
                consumedIdentifierToken,
                variableKind.Value,
                model);

            return true;
        }
        else
        {
            return false;
        }
    }

    public static void ResolveAmbiguousIdentifier(
        AmbiguousIdentifierNode consumedAmbiguousIdentifierNode,
        ParserModel model)
    {
        var expectingTypeClause = false;

        if (model.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenAngleBracketToken)
            expectingTypeClause = true;

        if (model.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenParenthesisToken)
            expectingTypeClause = true;

        if (model.TokenWalker.Current.SyntaxKind == SyntaxKind.EqualsToken ||
            model.TokenWalker.Current.SyntaxKind == SyntaxKind.StatementDelimiterToken)
        {
            expectingTypeClause = true;
        }

        if (expectingTypeClause)
        {
            if (!model.Binder.TryGetTypeDefinitionHierarchically(
                    consumedAmbiguousIdentifierNode.IdentifierToken.TextSpan.GetText(),
                    model.BinderSession.CurrentScope,
                    out var typeDefinitionNode)
                || typeDefinitionNode is null)
            {
                var fabricateTypeDefinition = new TypeDefinitionNode(
                    AccessModifierKind.Public,
                    false,
                    StorageModifierKind.Class,
                    consumedAmbiguousIdentifierNode.IdentifierToken,
                    null,
                    null,
                    null,
                    null,
                    null)
                {
                    IsFabricated = true
                };

                model.Binder.BindTypeDefinitionNode(fabricateTypeDefinition, model);
                typeDefinitionNode = fabricateTypeDefinition;
            }

            model.SyntaxStack.Push(typeDefinitionNode.ToTypeClause());
        }
    }

    public static void ParsePlusToken(
        PlusToken consumedPlusToken,
        ParserModel model)
    {
        // The handle expression won't see this token unless backtracked.
        model.TokenWalker.Backtrack();
        ParseOthers.HandleExpression(
            null,
            null,
            null,
            null,
            null,
            null,
            model);

        model.CurrentCodeBlockBuilder.ChildList.Add(
            (IExpressionNode)model.SyntaxStack.Pop());
    }

    public static void ParsePlusPlusToken(
        PlusPlusToken consumedPlusPlusToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) && syntax.SyntaxKind == SyntaxKind.VariableReferenceNode)
        {
            var variableReferenceNode = (VariableReferenceNode)model.SyntaxStack.Pop();

            var unaryOperatorNode = new UnaryOperatorNode(
                variableReferenceNode.ResultTypeClauseNode,
                consumedPlusPlusToken,
                variableReferenceNode.ResultTypeClauseNode);

            var unaryExpressionNode = new UnaryExpressionNode(
                variableReferenceNode,
                unaryOperatorNode);

            model.CurrentCodeBlockBuilder.ChildList.Add(unaryExpressionNode);
        }
    }

    public static void ParseMinusToken(
        MinusToken consumedMinusToken,
        ParserModel model)
    {
        // The handle expression won't see this token unless backtracked.
        model.TokenWalker.Backtrack();
        ParseOthers.HandleExpression(
            null,
            null,
            null,
            null,
            null,
            null,
            model);

        model.CurrentCodeBlockBuilder.ChildList.Add(
            (IExpressionNode)model.SyntaxStack.Pop());
    }

    public static void ParseStarToken(
        StarToken consumedStarToken,
        ParserModel model)
    {
        // The handle expression won't see this token unless backtracked.
        model.TokenWalker.Backtrack();
        ParseOthers.HandleExpression(
            null,
            null,
            null,
            null,
            null,
            null,
            model);

        model.CurrentCodeBlockBuilder.ChildList.Add(
            (IExpressionNode)model.SyntaxStack.Pop());
    }

    public static void ParseDollarSignToken(
        DollarSignToken consumedDollarSignToken,
        ParserModel model)
    {
        // The handle expression won't see this token unless backtracked.
        model.TokenWalker.Backtrack();
        ParseOthers.HandleExpression(
            null,
            null,
            null,
            null,
            null,
            null,
            model);

        model.CurrentCodeBlockBuilder.ChildList.Add(
            (IExpressionNode)model.SyntaxStack.Pop());
    }

    public static void ParseColonToken(
        ColonToken consumedColonToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) && syntax.SyntaxKind == SyntaxKind.TypeDefinitionNode)
        {
            var typeDefinitionNode = (TypeDefinitionNode)model.SyntaxStack.Pop();
            var inheritedTypeClauseNode = model.TokenWalker.MatchTypeClauseNode(model);

            model.Binder.BindTypeClauseNode(inheritedTypeClauseNode, model);

            model.SyntaxStack.Push(new TypeDefinitionNode(
                typeDefinitionNode.AccessModifierKind,
                typeDefinitionNode.HasPartialModifier,
                typeDefinitionNode.StorageModifierKind,
                typeDefinitionNode.TypeIdentifierToken,
                typeDefinitionNode.ValueType,
                typeDefinitionNode.GenericArgumentsListingNode,
                typeDefinitionNode.PrimaryConstructorFunctionArgumentsListingNode,
                inheritedTypeClauseNode,
                typeDefinitionNode.TypeBodyCodeBlockNode));
        }
        else
        {
            model.DiagnosticBag.ReportTodoException(consumedColonToken.TextSpan, "Colon is in unexpected place.");
        }
    }

    public static void ParseOpenBraceToken(
        OpenBraceToken consumedOpenBraceToken,
        ParserModel model)
    {
        var closureCurrentCodeBlockBuilder = model.CurrentCodeBlockBuilder;
        ISyntaxNode? nextCodeBlockOwner = null;
        TypeClauseNode? scopeReturnTypeClauseNode = null;

        if (model.SyntaxStack.TryPeek(out var syntax) && syntax.SyntaxKind == SyntaxKind.NamespaceStatementNode)
        {
            var namespaceStatementNode = (NamespaceStatementNode)model.SyntaxStack.Pop();
            nextCodeBlockOwner = namespaceStatementNode;

            model.FinalizeCodeBlockNodeActionStack.Push(codeBlockNode =>
            {
                namespaceStatementNode = new NamespaceStatementNode(
                    namespaceStatementNode.KeywordToken,
                    namespaceStatementNode.IdentifierToken,
                    codeBlockNode);

                closureCurrentCodeBlockBuilder.ChildList.Add(namespaceStatementNode);
                model.Binder.BindNamespaceStatementNode(namespaceStatementNode, model);
            });

            model.SyntaxStack.Push(namespaceStatementNode);
        }
        else if (model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.TypeDefinitionNode)
        {
            var typeDefinitionNode = (TypeDefinitionNode)model.SyntaxStack.Pop();
            nextCodeBlockOwner = typeDefinitionNode;

            model.FinalizeCodeBlockNodeActionStack.Push(codeBlockNode =>
            {
                typeDefinitionNode = new TypeDefinitionNode(
                    typeDefinitionNode.AccessModifierKind,
                    typeDefinitionNode.HasPartialModifier,
                    typeDefinitionNode.StorageModifierKind,
                    typeDefinitionNode.TypeIdentifierToken,
                    typeDefinitionNode.ValueType,
                    typeDefinitionNode.GenericArgumentsListingNode,
                    typeDefinitionNode.PrimaryConstructorFunctionArgumentsListingNode,
                    typeDefinitionNode.InheritedTypeClauseNode,
                    codeBlockNode);

                model.Binder.BindTypeDefinitionNode(typeDefinitionNode, model, true);
                closureCurrentCodeBlockBuilder.ChildList.Add(typeDefinitionNode);
            });

            model.SyntaxStack.Push(typeDefinitionNode);
        }
        else if (model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.FunctionDefinitionNode)
        {
            var functionDefinitionNode = (FunctionDefinitionNode)model.SyntaxStack.Pop();
            nextCodeBlockOwner = functionDefinitionNode;
            scopeReturnTypeClauseNode = functionDefinitionNode.ReturnTypeClauseNode;

            model.FinalizeCodeBlockNodeActionStack.Push(codeBlockNode =>
            {
                functionDefinitionNode = new FunctionDefinitionNode(
                    AccessModifierKind.Public,
                    functionDefinitionNode.ReturnTypeClauseNode,
                    functionDefinitionNode.FunctionIdentifierToken,
                    functionDefinitionNode.GenericArgumentsListingNode,
                    functionDefinitionNode.FunctionArgumentsListingNode,
                    codeBlockNode,
                    functionDefinitionNode.ConstraintNode);

                closureCurrentCodeBlockBuilder.ChildList.Add(functionDefinitionNode);
            });

            model.SyntaxStack.Push(functionDefinitionNode);
        }
        else if (model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.ConstructorDefinitionNode)
        {
            var constructorDefinitionNode = (ConstructorDefinitionNode)model.SyntaxStack.Pop();
            nextCodeBlockOwner = constructorDefinitionNode;
            scopeReturnTypeClauseNode = constructorDefinitionNode.ReturnTypeClauseNode;

            model.FinalizeCodeBlockNodeActionStack.Push(codeBlockNode =>
            {
                constructorDefinitionNode = new ConstructorDefinitionNode(
                    constructorDefinitionNode.ReturnTypeClauseNode,
                    constructorDefinitionNode.FunctionIdentifier,
                    constructorDefinitionNode.GenericArgumentsListingNode,
                    constructorDefinitionNode.FunctionArgumentsListingNode,
                    codeBlockNode,
                    constructorDefinitionNode.ConstraintNode);

                closureCurrentCodeBlockBuilder.ChildList.Add(constructorDefinitionNode);
            });

            model.SyntaxStack.Push(constructorDefinitionNode);
        }
        else if (model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.IfStatementNode)
        {
            var ifStatementNode = (IfStatementNode)model.SyntaxStack.Pop();
            nextCodeBlockOwner = ifStatementNode;

            model.FinalizeCodeBlockNodeActionStack.Push(codeBlockNode =>
            {
                ifStatementNode = new IfStatementNode(
                    ifStatementNode.KeywordToken,
                    ifStatementNode.ExpressionNode,
                    codeBlockNode);

                closureCurrentCodeBlockBuilder.ChildList.Add(ifStatementNode);
            });

            model.SyntaxStack.Push(ifStatementNode);
        }
        else
        {
            nextCodeBlockOwner = closureCurrentCodeBlockBuilder.CodeBlockOwner;

            model.FinalizeCodeBlockNodeActionStack.Push(codeBlockNode =>
            {
                closureCurrentCodeBlockBuilder.ChildList.Add(codeBlockNode);
            });
        }

        model.Binder.RegisterBoundScope(scopeReturnTypeClauseNode, consumedOpenBraceToken.TextSpan, model);

        if (model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.NamespaceStatementNode)
        {
            var namespaceStatementNode = (NamespaceStatementNode)model.SyntaxStack.Pop();

            var namespaceString = namespaceStatementNode
                .IdentifierToken
                .TextSpan
                .GetText();

            model.Binder.AddNamespaceToCurrentScope(namespaceString, model);
            model.SyntaxStack.Push(namespaceStatementNode);
        }

        model.CurrentCodeBlockBuilder = new(model.CurrentCodeBlockBuilder, nextCodeBlockOwner);
    }

    public static void ParseCloseBraceToken(
        CloseBraceToken consumedCloseBraceToken,
        ParserModel model)
    {
        model.Binder.DisposeBoundScope(consumedCloseBraceToken.TextSpan, model);

        if (model.CurrentCodeBlockBuilder.Parent is not null && model.FinalizeCodeBlockNodeActionStack.Any())
        {
            model.FinalizeCodeBlockNodeActionStack
                .Pop()
                .Invoke(model.CurrentCodeBlockBuilder.Build());

            model.CurrentCodeBlockBuilder = model.CurrentCodeBlockBuilder.Parent;
        }
    }

    public static void ParseOpenParenthesisToken(
        OpenParenthesisToken consumedOpenParenthesisToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) &&
            syntax is TypeDefinitionNode typeDefinitionNode)
        {
            if (typeDefinitionNode.StorageModifierKind == StorageModifierKind.Record)
            {
                _ = model.SyntaxStack.Pop();

                ParseTypes.HandlePrimaryConstructorDefinition(
                    typeDefinitionNode,
                    consumedOpenParenthesisToken,
                    model);

                return;
            }
        }

        // The handle expression won't see this token unless backtracked.
        model.TokenWalker.Backtrack();
        ParseOthers.HandleExpression(
            null,
            null,
            null,
            null,
            null,
            null,
            model);

        var parenthesizedExpression = (IExpressionNode)model.SyntaxStack.Pop();

        // Example: (3 + 4) * 3
        //
        // Complete expression would be binary multiplication.
        ParseOthers.HandleExpression(
            parenthesizedExpression,
            parenthesizedExpression,
            null,
            null,
            null,
            null,
            model);

        model.CurrentCodeBlockBuilder.ChildList.Add(
            (IExpressionNode)model.SyntaxStack.Pop());
    }

    public static void ParseCloseParenthesisToken(
        CloseParenthesisToken consumedCloseParenthesisToken,
        ParserModel model)
    {
    }

    public static void ParseOpenAngleBracketToken(
        OpenAngleBracketToken consumedOpenAngleBracketToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) && syntax.SyntaxKind == SyntaxKind.LiteralExpressionNode ||
            model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.LiteralExpressionNode ||
            model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.BinaryExpressionNode ||
            /* Prefer the enum comparison. Will short circuit. This "is" cast is for fallback in case someone in the future adds for expression syntax kinds but does not update this if statement TODO: Check if node ends with "ExpressionNode"? */
            model.SyntaxStack.TryPeek(out syntax) && syntax is IExpressionNode)
        {
            // Mathematical angle bracket
            model.DiagnosticBag.ReportTodoException(
                consumedOpenAngleBracketToken.TextSpan,
                $"Implement mathematical angle bracket");
        }
        else
        {
            // Generic Arguments
            ParseTypes.HandleGenericArguments(consumedOpenAngleBracketToken, model);

            if (model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.TypeDefinitionNode)
            {
                var typeDefinitionNode = (TypeDefinitionNode)model.SyntaxStack.Pop();

                // TODO: Fix boundClassDefinitionNode, it broke on (2023-07-26)
                //
                // _cSharpParser._nodeRecent = boundClassDefinitionNode with
                // {
                //     BoundGenericArgumentsNode = boundGenericArguments
                // };
            }
        }
    }

    public static void ParseCloseAngleBracketToken(
        CloseAngleBracketToken consumedCloseAngleBracketToken,
        ParserModel model)
    {
        model.DiagnosticBag.ReportTodoException(
            consumedCloseAngleBracketToken.TextSpan,
            $"Implement {nameof(ParseCloseAngleBracketToken)}");
    }

    public static void ParseOpenSquareBracketToken(
        OpenSquareBracketToken consumedOpenSquareBracketToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) && syntax.SyntaxKind == SyntaxKind.LiteralExpressionNode ||
            model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.LiteralExpressionNode ||
            model.SyntaxStack.TryPeek(out syntax) && syntax.SyntaxKind == SyntaxKind.BinaryExpressionNode ||
            /* Prefer the enum comparison. Will short circuit. This "is" cast is for fallback in case someone in the future adds for expression syntax kinds but does not update this if statement TODO: Check if node ends with "ExpressionNode"? */
            model.SyntaxStack.TryPeek(out syntax) && syntax is IExpressionNode)
        {
            // Mathematical square bracket
            model.DiagnosticBag.ReportTodoException(
                consumedOpenSquareBracketToken.TextSpan,
                $"Implement mathematical square bracket");
        }
        else
        {
            // Attribute
            model.SyntaxStack.Push(consumedOpenSquareBracketToken);
            ParseTypes.HandleAttribute(consumedOpenSquareBracketToken, model);
        }
    }

    public static void ParseCloseSquareBracketToken(
        CloseSquareBracketToken consumedCloseSquareBracketToken,
        ParserModel model)
    {
    }

    public static void ParseEqualsToken(
        EqualsToken consumedEqualsToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) &&
            syntax is FunctionDefinitionNode functionDefinitionNode)
        {
            if (functionDefinitionNode.FunctionBodyCodeBlockNode is null &&
                model.TokenWalker.Current.SyntaxKind == SyntaxKind.CloseAngleBracketToken)
            {
                var closeAngleBracketToken = model.TokenWalker.Consume();

                ParseOthers.HandleExpression(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    model);
                
                var expression = model.SyntaxStack.Pop();
                var codeBlockNode = new CodeBlockNode(new ISyntax[] 
                {
                    expression
                }.ToImmutableArray());

                functionDefinitionNode = (FunctionDefinitionNode)model.SyntaxStack.Pop();
                functionDefinitionNode = new FunctionDefinitionNode(
                    AccessModifierKind.Public,
                    functionDefinitionNode.ReturnTypeClauseNode,
                    functionDefinitionNode.FunctionIdentifierToken,
                    functionDefinitionNode.GenericArgumentsListingNode,
                    functionDefinitionNode.FunctionArgumentsListingNode,
                    codeBlockNode,
                    functionDefinitionNode.ConstraintNode);

                model.CurrentCodeBlockBuilder.ChildList.Add(functionDefinitionNode);
            }
        }
    }

    public static void ParseMemberAccessToken(
        MemberAccessToken consumedMemberAccessToken,
        ParserModel model)
    {
        model.SyntaxStack.Push(consumedMemberAccessToken);

        var isValidMemberAccessToken = true;

        if (model.SyntaxStack.TryPeek(out var syntax) && syntax is not null)
        {
            switch (model.SyntaxStack.Peek().SyntaxKind)
            {
                case SyntaxKind.VariableReferenceNode:
                    var variableReferenceNode = (VariableReferenceNode)model.SyntaxStack.Pop();

                    if (variableReferenceNode.VariableDeclarationNode.IsFabricated)
                    {
                        // Undeclared variable, so the Type is unknown.
                    }

                    break;
            }
        }
        else
        {
            isValidMemberAccessToken = false;
        }

        if (!isValidMemberAccessToken)
            model.DiagnosticBag.ReportTodoException(consumedMemberAccessToken.TextSpan, "MemberAccessToken needs further implementation.");
    }

    public static void ParseStatementDelimiterToken(
        StatementDelimiterToken consumedStatementDelimiterToken,
        ParserModel model)
    {
        if (model.SyntaxStack.TryPeek(out var syntax) && syntax.SyntaxKind == SyntaxKind.NamespaceStatementNode)
        {
            var closureCurrentCompilationUnitBuilder = model.CurrentCodeBlockBuilder;
            ISyntaxNode? nextCodeBlockOwner = null;
            TypeClauseNode? scopeReturnTypeClauseNode = null;

            var namespaceStatementNode = (NamespaceStatementNode)model.SyntaxStack.Pop();
            nextCodeBlockOwner = namespaceStatementNode;

            model.FinalizeNamespaceFileScopeCodeBlockNodeAction = codeBlockNode =>
                {
                    namespaceStatementNode = new NamespaceStatementNode(
                        namespaceStatementNode.KeywordToken,
                        namespaceStatementNode.IdentifierToken,
                        codeBlockNode);

                    closureCurrentCompilationUnitBuilder.ChildList.Add(namespaceStatementNode);
                    model.Binder.BindNamespaceStatementNode(namespaceStatementNode, model);
                };

            model.Binder.RegisterBoundScope(
                scopeReturnTypeClauseNode,
                consumedStatementDelimiterToken.TextSpan,
                model);

            model.Binder.AddNamespaceToCurrentScope(
                namespaceStatementNode.IdentifierToken.TextSpan.GetText(),
                model);

            model.CurrentCodeBlockBuilder = new(model.CurrentCodeBlockBuilder, nextCodeBlockOwner);
        }
    }

    public static void ParseKeywordToken(
        KeywordToken consumedKeywordToken,
        ParserModel model)
    {
        // 'return', 'if', 'get', etc...
        switch (consumedKeywordToken.SyntaxKind)
        {
            case SyntaxKind.AsTokenKeyword:
                ParseDefaultKeywords.HandleAsTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.BaseTokenKeyword:
                ParseDefaultKeywords.HandleBaseTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.BoolTokenKeyword:
                ParseDefaultKeywords.HandleBoolTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.BreakTokenKeyword:
                ParseDefaultKeywords.HandleBreakTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ByteTokenKeyword:
                ParseDefaultKeywords.HandleByteTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.CaseTokenKeyword:
                ParseDefaultKeywords.HandleCaseTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.CatchTokenKeyword:
                ParseDefaultKeywords.HandleCatchTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.CharTokenKeyword:
                ParseDefaultKeywords.HandleCharTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.CheckedTokenKeyword:
                ParseDefaultKeywords.HandleCheckedTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ConstTokenKeyword:
                ParseDefaultKeywords.HandleConstTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ContinueTokenKeyword:
                ParseDefaultKeywords.HandleContinueTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.DecimalTokenKeyword:
                ParseDefaultKeywords.HandleDecimalTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.DefaultTokenKeyword:
                ParseDefaultKeywords.HandleDefaultTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.DelegateTokenKeyword:
                ParseDefaultKeywords.HandleDelegateTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.DoTokenKeyword:
                ParseDefaultKeywords.HandleDoTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.DoubleTokenKeyword:
                ParseDefaultKeywords.HandleDoubleTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ElseTokenKeyword:
                ParseDefaultKeywords.HandleElseTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.EnumTokenKeyword:
                ParseDefaultKeywords.HandleEnumTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.EventTokenKeyword:
                ParseDefaultKeywords.HandleEventTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ExplicitTokenKeyword:
                ParseDefaultKeywords.HandleExplicitTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ExternTokenKeyword:
                ParseDefaultKeywords.HandleExternTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.FalseTokenKeyword:
                ParseDefaultKeywords.HandleFalseTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.FinallyTokenKeyword:
                ParseDefaultKeywords.HandleFinallyTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.FixedTokenKeyword:
                ParseDefaultKeywords.HandleFixedTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.FloatTokenKeyword:
                ParseDefaultKeywords.HandleFloatTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ForTokenKeyword:
                ParseDefaultKeywords.HandleForTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ForeachTokenKeyword:
                ParseDefaultKeywords.HandleForeachTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.GotoTokenKeyword:
                ParseDefaultKeywords.HandleGotoTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ImplicitTokenKeyword:
                ParseDefaultKeywords.HandleImplicitTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.InTokenKeyword:
                ParseDefaultKeywords.HandleInTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.IntTokenKeyword:
                ParseDefaultKeywords.HandleIntTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.IsTokenKeyword:
                ParseDefaultKeywords.HandleIsTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.LockTokenKeyword:
                ParseDefaultKeywords.HandleLockTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.LongTokenKeyword:
                ParseDefaultKeywords.HandleLongTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.NullTokenKeyword:
                ParseDefaultKeywords.HandleNullTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ObjectTokenKeyword:
                ParseDefaultKeywords.HandleObjectTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.OperatorTokenKeyword:
                ParseDefaultKeywords.HandleOperatorTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.OutTokenKeyword:
                ParseDefaultKeywords.HandleOutTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ParamsTokenKeyword:
                ParseDefaultKeywords.HandleParamsTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ProtectedTokenKeyword:
                ParseDefaultKeywords.HandleProtectedTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ReadonlyTokenKeyword:
                ParseDefaultKeywords.HandleReadonlyTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.RefTokenKeyword:
                ParseDefaultKeywords.HandleRefTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.SbyteTokenKeyword:
                ParseDefaultKeywords.HandleSbyteTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ShortTokenKeyword:
                ParseDefaultKeywords.HandleShortTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.SizeofTokenKeyword:
                ParseDefaultKeywords.HandleSizeofTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.StackallocTokenKeyword:
                ParseDefaultKeywords.HandleStackallocTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.StringTokenKeyword:
                ParseDefaultKeywords.HandleStringTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.StructTokenKeyword:
                ParseDefaultKeywords.HandleStructTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.SwitchTokenKeyword:
                ParseDefaultKeywords.HandleSwitchTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ThisTokenKeyword:
                ParseDefaultKeywords.HandleThisTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ThrowTokenKeyword:
                ParseDefaultKeywords.HandleThrowTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.TrueTokenKeyword:
                ParseDefaultKeywords.HandleTrueTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.TryTokenKeyword:
                ParseDefaultKeywords.HandleTryTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.TypeofTokenKeyword:
                ParseDefaultKeywords.HandleTypeofTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.UintTokenKeyword:
                ParseDefaultKeywords.HandleUintTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.UlongTokenKeyword:
                ParseDefaultKeywords.HandleUlongTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.UncheckedTokenKeyword:
                ParseDefaultKeywords.HandleUncheckedTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.UnsafeTokenKeyword:
                ParseDefaultKeywords.HandleUnsafeTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.UshortTokenKeyword:
                ParseDefaultKeywords.HandleUshortTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.VoidTokenKeyword:
                ParseDefaultKeywords.HandleVoidTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.VolatileTokenKeyword:
                ParseDefaultKeywords.HandleVolatileTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.WhileTokenKeyword:
                ParseDefaultKeywords.HandleWhileTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.UnrecognizedTokenKeyword:
                ParseDefaultKeywords.HandleUnrecognizedTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ReturnTokenKeyword:
                ParseDefaultKeywords.HandleReturnTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.NamespaceTokenKeyword:
                ParseDefaultKeywords.HandleNamespaceTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.ClassTokenKeyword:
                ParseDefaultKeywords.HandleClassTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.InterfaceTokenKeyword:
                ParseDefaultKeywords.HandleInterfaceTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.UsingTokenKeyword:
                ParseDefaultKeywords.HandleUsingTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.PublicTokenKeyword:
                ParseDefaultKeywords.HandlePublicTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.InternalTokenKeyword:
                ParseDefaultKeywords.HandleInternalTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.PrivateTokenKeyword:
                ParseDefaultKeywords.HandlePrivateTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.StaticTokenKeyword:
                ParseDefaultKeywords.HandleStaticTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.OverrideTokenKeyword:
                ParseDefaultKeywords.HandleOverrideTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.VirtualTokenKeyword:
                ParseDefaultKeywords.HandleVirtualTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.AbstractTokenKeyword:
                ParseDefaultKeywords.HandleAbstractTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.SealedTokenKeyword:
                ParseDefaultKeywords.HandleSealedTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.IfTokenKeyword:
                ParseDefaultKeywords.HandleIfTokenKeyword(consumedKeywordToken, model);
                break;
            case SyntaxKind.NewTokenKeyword:
                ParseDefaultKeywords.HandleNewTokenKeyword(consumedKeywordToken, model);
                break;
            default:
                ParseDefaultKeywords.HandleDefault(consumedKeywordToken, model);
                break;
        }
    }

    public static void ParseKeywordContextualToken(
        KeywordContextualToken consumedKeywordContextualToken,
        ParserModel model)
    {
        switch (consumedKeywordContextualToken.SyntaxKind)
        {
            case SyntaxKind.VarTokenContextualKeyword:
                ParseContextualKeywords.HandleVarTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.PartialTokenContextualKeyword:
                ParseContextualKeywords.HandlePartialTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.AddTokenContextualKeyword:
                ParseContextualKeywords.HandleAddTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.AndTokenContextualKeyword:
                ParseContextualKeywords.HandleAndTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.AliasTokenContextualKeyword:
                ParseContextualKeywords.HandleAliasTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.AscendingTokenContextualKeyword:
                ParseContextualKeywords.HandleAscendingTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.ArgsTokenContextualKeyword:
                ParseContextualKeywords.HandleArgsTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.AsyncTokenContextualKeyword:
                ParseContextualKeywords.HandleAsyncTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.AwaitTokenContextualKeyword:
                ParseContextualKeywords.HandleAwaitTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.ByTokenContextualKeyword:
                ParseContextualKeywords.HandleByTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.DescendingTokenContextualKeyword:
                ParseContextualKeywords.HandleDescendingTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.DynamicTokenContextualKeyword:
                ParseContextualKeywords.HandleDynamicTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.EqualsTokenContextualKeyword:
                ParseContextualKeywords.HandleEqualsTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.FileTokenContextualKeyword:
                ParseContextualKeywords.HandleFileTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.FromTokenContextualKeyword:
                ParseContextualKeywords.HandleFromTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.GetTokenContextualKeyword:
                ParseContextualKeywords.HandleGetTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.GlobalTokenContextualKeyword:
                ParseContextualKeywords.HandleGlobalTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.GroupTokenContextualKeyword:
                ParseContextualKeywords.HandleGroupTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.InitTokenContextualKeyword:
                ParseContextualKeywords.HandleInitTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.IntoTokenContextualKeyword:
                ParseContextualKeywords.HandleIntoTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.JoinTokenContextualKeyword:
                ParseContextualKeywords.HandleJoinTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.LetTokenContextualKeyword:
                ParseContextualKeywords.HandleLetTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.ManagedTokenContextualKeyword:
                ParseContextualKeywords.HandleManagedTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.NameofTokenContextualKeyword:
                ParseContextualKeywords.HandleNameofTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.NintTokenContextualKeyword:
                ParseContextualKeywords.HandleNintTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.NotTokenContextualKeyword:
                ParseContextualKeywords.HandleNotTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.NotnullTokenContextualKeyword:
                ParseContextualKeywords.HandleNotnullTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.NuintTokenContextualKeyword:
                ParseContextualKeywords.HandleNuintTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.OnTokenContextualKeyword:
                ParseContextualKeywords.HandleOnTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.OrTokenContextualKeyword:
                ParseContextualKeywords.HandleOrTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.OrderbyTokenContextualKeyword:
                ParseContextualKeywords.HandleOrderbyTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.RecordTokenContextualKeyword:
                ParseContextualKeywords.HandleRecordTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.RemoveTokenContextualKeyword:
                ParseContextualKeywords.HandleRemoveTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.RequiredTokenContextualKeyword:
                ParseContextualKeywords.HandleRequiredTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.ScopedTokenContextualKeyword:
                ParseContextualKeywords.HandleScopedTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.SelectTokenContextualKeyword:
                ParseContextualKeywords.HandleSelectTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.SetTokenContextualKeyword:
                ParseContextualKeywords.HandleSetTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.UnmanagedTokenContextualKeyword:
                ParseContextualKeywords.HandleUnmanagedTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.ValueTokenContextualKeyword:
                ParseContextualKeywords.HandleValueTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.WhenTokenContextualKeyword:
                ParseContextualKeywords.HandleWhenTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.WhereTokenContextualKeyword:
                ParseContextualKeywords.HandleWhereTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.WithTokenContextualKeyword:
                ParseContextualKeywords.HandleWithTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.YieldTokenContextualKeyword:
                ParseContextualKeywords.HandleYieldTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            case SyntaxKind.UnrecognizedTokenContextualKeyword:
                ParseContextualKeywords.HandleUnrecognizedTokenContextualKeyword(consumedKeywordContextualToken, model);
                break;
            default:
                throw new NotImplementedException($"Implement the {consumedKeywordContextualToken.SyntaxKind.ToString()} contextual keyword.");
        }
    }
}