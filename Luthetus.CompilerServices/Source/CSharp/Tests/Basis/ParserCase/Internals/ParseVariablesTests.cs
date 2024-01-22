﻿using Luthetus.CompilerServices.Lang.CSharp.LexerCase;
using Luthetus.CompilerServices.Lang.CSharp.ParserCase;
using Luthetus.TextEditor.RazorLib.CompilerServices;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.SyntaxNodes;
using Luthetus.TextEditor.RazorLib.Lexes.Models;

namespace Luthetus.CompilerServices.Lang.CSharp.Tests.Basis.ParserCase.Internals;

public class ParseVariablesTests
{
    /*
     int x;
     Person x;
     var x;
     x = true;
     x = 2;
     x = "Hello World!";
     int x = 2;
     var x = 2;
     */

    [Fact]
    public void VariableDeclaration_WITH_ExplicitType_TypeIsKeyword()
    {
        var resourceUri = new ResourceUri("UnitTests");
        var sourceText = "int x;";
        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topCodeBlock = compilationUnit.RootCodeBlockNode;
        
        var variableDeclarationNode = (VariableDeclarationNode)topCodeBlock.ChildList.Single();

        var typeClauseNode = variableDeclarationNode.TypeClauseNode;
        Assert.Equal("int", typeClauseNode.TypeIdentifierToken.TextSpan.GetText());
        Assert.Equal(typeof(int), typeClauseNode.ValueType);

        var identifierToken = variableDeclarationNode.IdentifierToken;
        Assert.Equal("x", identifierToken.TextSpan.GetText());

        Assert.Empty(compilationUnit.DiagnosticsList);
    }
    
    [Fact]
    public void VariableDeclaration_WITH_ExplicitType_TypeIsIdentifier()
    {
        var resourceUri = new ResourceUri("UnitTests");
        var sourceText = "Person x;";
        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topCodeBlock = compilationUnit.RootCodeBlockNode;

        var variableDeclarationNode = (VariableDeclarationNode)topCodeBlock.ChildList.Single();

        var typeClauseNode = variableDeclarationNode.TypeClauseNode;
        Assert.Equal("Person", typeClauseNode.TypeIdentifierToken.TextSpan.GetText());
        Assert.Null(typeClauseNode.ValueType);

        var identifierToken = variableDeclarationNode.IdentifierToken;
        Assert.Equal("x", identifierToken.TextSpan.GetText());

        Guid idOfExpectedDiagnostic;
        {
            // TODO: Reporting the diagnostic to get the Id like this is silly.
            var fakeDiagnosticBag = new LuthetusDiagnosticBag();
            fakeDiagnosticBag.ReportUndefinedTypeOrNamespace(
                TextEditorTextSpan.FabricateTextSpan(string.Empty),
                string.Empty);
            idOfExpectedDiagnostic = fakeDiagnosticBag.Single().Id;
        }
        
        Assert.Single(compilationUnit.DiagnosticsList);
        Assert.Equal(idOfExpectedDiagnostic, compilationUnit.DiagnosticsList.Single().Id);
    }

    [Fact]
    public void VariableDeclaration_WITH_ImplicitType()
    {
        var resourceUri = new ResourceUri("UnitTests");
        var sourceText = "var x;";
        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topCodeBlock = compilationUnit.RootCodeBlockNode;

        var variableDeclarationNode = (VariableDeclarationNode)topCodeBlock.ChildList.Single();

        var typeClauseNode = variableDeclarationNode.TypeClauseNode;
        Assert.Equal("var", typeClauseNode.TypeIdentifierToken.TextSpan.GetText());
        Assert.Null(typeClauseNode.ValueType);

        var identifierToken = variableDeclarationNode.IdentifierToken;
        Assert.Equal("x", identifierToken.TextSpan.GetText());

        Guid idOfExpectedDiagnostic;
        {
            // TODO: Reporting the diagnostic to get the Id like this is silly.
            var fakeDiagnosticBag = new LuthetusDiagnosticBag();
            fakeDiagnosticBag.ReportImplicitlyTypedVariablesMustBeInitialized(
                TextEditorTextSpan.FabricateTextSpan(string.Empty));
            idOfExpectedDiagnostic = fakeDiagnosticBag.Single().Id;
        }

        Assert.Single(compilationUnit.DiagnosticsList);
        Assert.Equal(idOfExpectedDiagnostic, compilationUnit.DiagnosticsList.Single().Id);
    }

    [Fact]
    public void VariableAssignment_WITH_BoolLiteral()
    {
        var resourceUri = new ResourceUri("UnitTests");
        var sourceText = "x = true;";
        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topCodeBlock = compilationUnit.RootCodeBlockNode;
    }

    [Fact]
    public void VariableAssignment_WITH_IntLiteral()
    {
        var sourceText = "x = 2;";
    }

    [Fact]
    public void VariableAssignment_WITH_StringLiteral()
    {
        var sourceText = "x = \"Hello World!\";";
    }

    [Fact]
    public void COMBINED_VariableDeclaration_AND_VariableAssignment_WITH_ExplicitType()
    {
        var sourceText = "int x = 2;";
    }
    
    [Fact]
    public void COMBINED_VariableDeclaration_AND_VariableAssignment_WITH_ImplicitType()
    {
        var sourceText = "var x = 2;";
    }
}
