﻿using Luthetus.CompilerServices.Lang.CSharp.BinderCase;
using Luthetus.CompilerServices.Lang.CSharp.LexerCase;
using Luthetus.CompilerServices.Lang.CSharp.ParserCase;
using Luthetus.TextEditor.RazorLib.CompilerServices;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.SyntaxNodes;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.SyntaxTokens;
using Luthetus.TextEditor.RazorLib.Lexes.Models;

namespace Luthetus.CompilerServices.Lang.CSharp.Tests.Basics;

public partial class ParserTests
{
    [Fact]
    public void SHOULD_PARSE_VARIABLE_DECLARATION_STATEMENT()
    {
        var variableTypeClauseIdentifier = "int";
        var variableIdentifier = "x";
        var sourceText = @$"{variableTypeClauseIdentifier} {variableIdentifier};".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var variableDeclarationStatementNode = (VariableDeclarationStatementNode)codeBlockNode.ChildBag.Single();

        Assert.Equal(SyntaxKind.VariableDeclarationStatementNode, variableDeclarationStatementNode.SyntaxKind);
        Assert.Equal(2, variableDeclarationStatementNode.ChildBag.Length);
        
        var typeClauseNode = (TypeClauseNode)variableDeclarationStatementNode.ChildBag[0];
        Assert.Equal(SyntaxKind.TypeClauseNode, typeClauseNode.SyntaxKind);
        Assert.Equal(variableTypeClauseIdentifier, typeClauseNode.TypeIdentifier.TextSpan.GetText());

        var identifierToken = (IdentifierToken)variableDeclarationStatementNode.ChildBag[1];
        Assert.Equal(SyntaxKind.IdentifierToken, identifierToken.SyntaxKind);
        Assert.Equal(variableIdentifier, identifierToken.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_VARIABLE_DECLARATION_STATEMENT_THEN_VARIABLE_ASSIGNMENT_STATEMENT()
    {
        string sourceText = @"int x; x = 42;".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        Assert.Equal(2, compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag.Length);

        var variableDeclarationStatementNode = compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag[0];
        Assert.Equal(SyntaxKind.VariableDeclarationStatementNode, variableDeclarationStatementNode.SyntaxKind);

        var variableAssignmentExpressionNode = (VariableAssignmentExpressionNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag[1];
        Assert.Equal(SyntaxKind.VariableAssignmentExpressionNode, variableAssignmentExpressionNode.SyntaxKind);
    }

    [Fact]
    public void SHOULD_PARSE_COMPOUND_VARIABLE_DECLARATION_AND_ASSIGNMENT_STATEMENT()
    {
        string sourceText = @"int x = 42;".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        Assert.Equal(2, compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag.Length);

        var variableDeclarationStatementNode = (VariableDeclarationStatementNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag[0];
        Assert.Equal(SyntaxKind.VariableDeclarationStatementNode, variableDeclarationStatementNode.SyntaxKind);

        var variableAssignmentExpressionNode = (VariableAssignmentExpressionNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag[1];
        Assert.Equal(SyntaxKind.VariableAssignmentExpressionNode, variableAssignmentExpressionNode.SyntaxKind);
    }

    [Fact]
    public void SHOULD_PARSE_CONDITIONAL_VAR_KEYWORD()
    {
        var varString = "var";
        var otherVariableIdentifier = "x";
        var sourceText = $@"var var = 2; var {otherVariableIdentifier} = var * 2;".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var modelParser = new CSharpParser(lexer);
        var compilationUnit = modelParser.Parse();

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var variableDeclarationWithIdentifierVar = (VariableDeclarationStatementNode)topLevelStatementsCodeBlockNode.ChildBag[0];
        Assert.Equal(varString, variableDeclarationWithIdentifierVar.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(varString, variableDeclarationWithIdentifierVar.IdentifierToken.TextSpan.GetText());

        var variableAssignmentWithIdentifierVar = (VariableAssignmentExpressionNode)topLevelStatementsCodeBlockNode.ChildBag[1];
        Assert.NotNull(variableAssignmentWithIdentifierVar);

        var variableDeclarationWithIdentifierX = (VariableDeclarationStatementNode)topLevelStatementsCodeBlockNode.ChildBag[2];
        Assert.Equal(varString, variableDeclarationWithIdentifierX.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(otherVariableIdentifier, variableDeclarationWithIdentifierX.IdentifierToken.TextSpan.GetText());

        var variableAssignmentWithIdentifierX = (VariableAssignmentExpressionNode)topLevelStatementsCodeBlockNode.ChildBag[3];
        Assert.NotNull(variableAssignmentWithIdentifierX);
    }

    [Fact]
    public void SHOULD_PARSE_VARIABLE_REFERENCE()
    {
        var sourceText = @"private int _count; private void IncrementCountOnClick() { _count++; }".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var modelParser = new CSharpParser(lexer);
        var compilationUnit = modelParser.Parse();

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;
        Assert.NotNull(codeBlockNode);

        var functionDefinitionNode = (FunctionDefinitionNode)compilationUnit.ChildBag[1];
        Assert.NotNull(functionDefinitionNode);

        var boundIdentifierReferenceNode = (IdentifierReferenceNode)compilationUnit.ChildBag[2];
        Assert.NotNull(boundIdentifierReferenceNode);
    }

    /// <summary>GOAL: Add "HelloWorld" key to NamespaceDictionary with a single CompilationUnit child which has a CompilationUnit without any children.</summary>
    [Fact]
    public void SHOULD_PARSE_NAMESPACE_DEFINITION_EMPTY()
    {
        string sourceText = @"namespace HelloWorld {}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        
        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var boundNamespaceStatementNode = (NamespaceStatementNode)topLevelStatementsCodeBlockNode.ChildBag.Single();
        Assert.Equal(SyntaxKind.NamespaceStatementNode, boundNamespaceStatementNode.SyntaxKind);

        var boundNamespaceEntryNode = boundNamespaceStatementNode.NamespaceEntryNodeBag.Single();
        Assert.Equal(SyntaxKind.NamespaceEntryNode, boundNamespaceEntryNode.SyntaxKind);
        Assert.Empty(boundNamespaceEntryNode.CodeBlockNode.ChildBag);
    }

    [Fact]
    public void SHOULD_PARSE_NAMESPACE_BLOCK_SCOPED()
    {
        var classIdentifier = "PersonModel";
        var sourceText = @$"namespace PersonCase {{ public class {classIdentifier} {{ }} }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var boundNamespaceStatementNode = (NamespaceStatementNode)topLevelStatementsCodeBlockNode.ChildBag.Single();
        var boundNamespaceEntryNode = boundNamespaceStatementNode.NamespaceEntryNodeBag.Single();
        var typeDefinitionNode = (TypeDefinitionNode)boundNamespaceEntryNode.CodeBlockNode.ChildBag.Single();

        var namespaceScope = parser.Binder.BoundScopes[1];

        var personModel = namespaceScope.TypeDefinitionMap.Single();
        Assert.Equal(classIdentifier, personModel.Key);
    }

    [Fact]
    public void SHOULD_PARSE_NAMESPACE_FILE_SCOPED()
    {
        var classIdentifier = "PersonModel";
        var sourceText = @$"namespace PersonCase; public class {classIdentifier} {{ }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var boundNamespaceStatementNode = (NamespaceStatementNode)topLevelStatementsCodeBlockNode.ChildBag.Single();
        var boundNamespaceEntryNode = boundNamespaceStatementNode.NamespaceEntryNodeBag.Single();
        var typeDefinitionNode = (TypeDefinitionNode)boundNamespaceEntryNode.CodeBlockNode.ChildBag.Single();

        var namespaceScope = parser.Binder.BoundScopes[1];

        var personModelKeyValuePair = namespaceScope.TypeDefinitionMap.Single();
        Assert.Equal(classIdentifier, personModelKeyValuePair.Key);
    }

    /// <summary>A file scope namespace results in the file not being allowed to have any block namespaces. So this test should result in the proper Diagnostics being reported.</summary>
    [Fact]
    public void SHOULD_PARSE_NAMESPACE_MIXED_FILE_SCOPE_THEN_BLOCK_SCOPE()
    {
        var classIdentifier = "PersonModel";
        var sourceText = @$"namespace PersonCase; public class {classIdentifier} {{ }} namespace Pages {{ public class {classIdentifier} {{ }} }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    /// <summary>A file scope namespace results in the file not being allowed to have any block namespaces. So this test should result in the proper Diagnostics being reported.</summary>
    [Fact]
    public void SHOULD_PARSE_NAMESPACE_MIXED_BLOCK_SCOPE_THEN_FILE_SCOPE()
    {
        var classIdentifier = "PersonModel";
        var sourceText = @$"namespace Pages {{ public class {classIdentifier} {{ }} }} namespace PersonCase; public class {classIdentifier} {{ }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    /// <summary>GOAL: Add "PersonCase" key to NamespaceDictionary with two CompilationUnit children: 'PersonModel.cs', and 'PersonDisplay.razor.cs'.<br/><br/>Afterwards convert the Namespace to a BoundScope which would contain the two classes: 'PersonModel', and 'PersonDisplay'</summary>
    [Fact]
    public void SHOULD_PARSE_TWO_NAMESPACE_DECLARATIONS_WITH_THE_SAME_IDENTIFIER_INTO_A_SINGLE_SCOPE()
    {
        string personModelClassIdentifier = "PersonModel", personDisplayClassIdentifier = "PersonDisplay";

        var personModelResourceUri = new ResourceUri("PersonModel.cs");
        var personModelContent = @$"namespace PersonCase {{ public class {personModelClassIdentifier} {{ }} }}".ReplaceLineEndings("\n");

        var personDisplayResourceUri = new ResourceUri("PersonDisplay.razor.cs");
        var personDisplayContent = @$"namespace PersonCase {{ public partial class {personDisplayClassIdentifier} : ComponentBase {{ }} }}".ReplaceLineEndings("\n");

        CompilationUnit modelCompilationUnit;
        CompilationUnit displayCompilationUnit;

        var modelLexer = new CSharpLexer(personModelResourceUri, personModelContent);
        modelLexer.Lex();
        var modelParser = new CSharpParser(modelLexer);
        modelCompilationUnit = modelParser.Parse();

        var displayLexer = new CSharpLexer(personDisplayResourceUri, personDisplayContent);
        displayLexer.Lex();
        var displayParser = new CSharpParser(displayLexer);
        displayCompilationUnit = displayParser.Parse(modelParser.Binder, personDisplayResourceUri);

        var displayTopLevelStatementsCodeBlockNode = displayCompilationUnit.TopLevelStatementsCodeBlockNode;

        var boundNamespaceStatementNode = (NamespaceStatementNode)displayTopLevelStatementsCodeBlockNode.ChildBag.Single();
        Assert.Equal(SyntaxKind.NamespaceStatementNode, boundNamespaceStatementNode.SyntaxKind);
        Assert.Equal(2, boundNamespaceStatementNode.NamespaceEntryNodeBag.Length);

        // TODO: (2023-05-28) The way a reference to namespaceScope is obtained is hacky and perhaps should be changed. The BoundScopes[2] is at index 2 specifically. Index 0 is global scope. Index 1 is the first time the namespace is declared. Index 2 is the second time the namespace is declared.
        var namespaceScope = displayParser.Binder.BoundScopes[2];

        Assert.True(namespaceScope.TypeDefinitionMap.ContainsKey(personModelClassIdentifier));
        Assert.True(namespaceScope.TypeDefinitionMap.ContainsKey(personDisplayClassIdentifier));
    }

    /// <summary>GOAL: Add "PersonCase" key to NamespaceDictionary with two CompilationUnit children: PersonModel.cs, and PersonDisplay.razor.cs. Afterwards evaluate the Namespace as a BoundScope which would contain the two classes: PersonModel, and PersonDisplay. Afterwards add "Pages" key to NamespaceDictionary with one CompilationUnit child: PersonPage.razor. Have PersonPage.razor.cs include a using statement that includes the "PersonCase" namespace.</summary>
    [Fact]
    public void SHOULD_PARSE_USING_STATEMENT()
    {
        var personModelResourceUri = new ResourceUri("PersonModel.cs");
        var personModelClassIdentifier = "PersonModel";
        var personModelContent = $@"namespace PersonCase {{ public class {personModelClassIdentifier} {{ }} }}".ReplaceLineEndings("\n");

        var personDisplayResourceUri = new ResourceUri("PersonDisplay.razor.cs");
        var personDisplayClassIdentifier = "PersonDisplay";
        var personDisplayContent = $@"namespace PersonCase {{ public partial class {personDisplayClassIdentifier} : ComponentBase {{ }} }}".ReplaceLineEndings("\n");

        var personPageResourceUri = new ResourceUri("PersonPage.razor.cs");
        var personPageContent = @"using PersonCase; namespace Pages { public partial class PersonPage : ComponentBase { } }".ReplaceLineEndings("\n");

        CompilationUnit modelCompilationUnit;
        CompilationUnit displayCompilationUnit;
        CompilationUnit pageCompilationUnit;

        var modelLexer = new CSharpLexer(personModelResourceUri, personModelContent);
        modelLexer.Lex();
        var modelParser = new CSharpParser(modelLexer);
        modelCompilationUnit = modelParser.Parse();

        var displayLexer = new CSharpLexer(personDisplayResourceUri, personDisplayContent);
        displayLexer.Lex();
        var displayParser = new CSharpParser(displayLexer);
        displayCompilationUnit = displayParser.Parse(modelParser.Binder, personDisplayResourceUri);

        var pageLexer = new CSharpLexer(personPageResourceUri, personPageContent);
        pageLexer.Lex();
        var pageParser = new CSharpParser(pageLexer);
        pageCompilationUnit = pageParser.Parse(displayParser.Binder, personPageResourceUri);

        var globalScope = pageParser.Binder.BoundScopes.First();

        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(personModelClassIdentifier));
        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(personDisplayClassIdentifier));
    }

    [Fact]
    public void SHOULD_PARSE_USING_STATEMENT_CONTAINING_MEMBER_ACCESS_TOKEN()
    {
        var namespaceIdentifier = "Microsoft.AspNetCore.Components";
        var sourceResourceUri = new ResourceUri("PersonPage.razor.cs");
        var sourceContent = @$"using {namespaceIdentifier};".ReplaceLineEndings("\n");

        var lexer = new CSharpLexer(sourceResourceUri, sourceContent);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var boundUsingStatementNode = (UsingStatementNode)codeBlockNode.ChildBag.Single();
        Assert.Equal(namespaceIdentifier, boundUsingStatementNode.NamespaceIdentifier.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_TOP_LEVEL_STATEMENTS()
    {
        throw new NotImplementedException("(2023-05-30) I am not sure how I want to test this yet.");
    }

    [Fact]
    public void SHOULD_PARSE_NAMESPACE_IDENTIFIER_CONTAINING_MEMBER_ACCESS_TOKEN()
    {
        var namespaceIdentifier = "BlazorWasmApp.PersonCase";
        var sourceText = @$"namespace {namespaceIdentifier} {{}}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var namespaceStatementNode = (NamespaceStatementNode)codeBlockNode.ChildBag.Single();
        Assert.Equal(SyntaxKind.NamespaceStatementNode, namespaceStatementNode.SyntaxKind);
        Assert.Equal(namespaceIdentifier, namespaceStatementNode.IdentifierToken.TextSpan.GetText());

        var namespaceEntryNode = namespaceStatementNode.NamespaceEntryNodeBag.Single();
        Assert.Equal(SyntaxKind.NamespaceEntryNode, namespaceEntryNode.SyntaxKind);
        Assert.Empty(namespaceEntryNode.CodeBlockNode.ChildBag);
    }

    [Fact]
    public void SHOULD_PARSE_NUMERIC_LITERAL_EXPRESSION()
    {
        string sourceText = "3".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;
        Assert.Single(topLevelStatementsCodeBlockNode.ChildBag);

        var literalExpressionNode = (LiteralExpressionNode)topLevelStatementsCodeBlockNode.ChildBag[0];
        Assert.Equal(typeof(int), literalExpressionNode.TypeClauseNode?.ValueType);
        Assert.Equal(3, int.Parse(literalExpressionNode.LiteralSyntaxToken.TextSpan.GetText()));
    }

    [Fact]
    public void SHOULD_PARSE_STRING_LITERAL_EXPRESSION()
    {
        string sourceText = $"\"123abc\"".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        Assert.Single(compilationUnit.ChildBag);
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var literalExpressionNode = (LiteralExpressionNode)codeBlockNode.ChildBag.Single();
        Assert.Equal(typeof(string), literalExpressionNode.TypeClauseNode?.ValueType);
        Assert.Equal(sourceText, literalExpressionNode.LiteralSyntaxToken.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_INTERFACE_DEFINITION_WHICH_CONTAINS_A_FUNCTION_DEFINITION()
    {
        var interfaceIdentifierString = "IPersonRepository";
        var methodIdentifierString = "AddPerson";
        var methodReturnTypeIdentifierString = "void";
        var methodArgumentTypeString = "IPersonModel";
        var methodArgumentIdentifierString = "personModel";
        var sourceText = @$"public interface {interfaceIdentifierString} {{ public {methodReturnTypeIdentifierString} {methodIdentifierString}({methodArgumentTypeString} {methodArgumentIdentifierString}); }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;
        Assert.Single(topLevelStatementsCodeBlockNode.ChildBag);
        
        var typeDefinitionNode = (TypeDefinitionNode)topLevelStatementsCodeBlockNode.ChildBag.Single();
        Assert.Equal(interfaceIdentifierString, typeDefinitionNode.TypeIdentifier.TextSpan.GetText());

        var cSharpBinder = (CSharpBinder)compilationUnit.Binder;
        Assert.True(cSharpBinder.BoundScopes.First().TypeDefinitionMap.ContainsKey(interfaceIdentifierString));

        var typeDefinition = cSharpBinder.BoundScopes.First().TypeDefinitionMap[interfaceIdentifierString];
        var functionDefinition = typeDefinition.GetFunctionDefinitionNodes().Single(x => x.FunctionIdentifier.TextSpan.GetText() == methodIdentifierString);

        Assert.Equal(methodReturnTypeIdentifierString, functionDefinition.ReturnTypeClauseNode.TypeIdentifier.TextSpan.GetText());
        
        var functionArgumentEntryNode = functionDefinition.FunctionArgumentsListingNode.FunctionArgumentEntryNodeBag.Single();

        Assert.Equal(methodArgumentTypeString, functionArgumentEntryNode.VariableDeclarationStatementNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(methodArgumentIdentifierString, functionArgumentEntryNode.VariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_DEFINITION_STATEMENT_WITH_KEYWORD_RETURN_TYPE_NO_ARGUMENTS()
    {
        string functionIdentifier = "WriteHelloWorldToConsole";
        string sourceText = @$"void {functionIdentifier}(){{}}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        Assert.Single(compilationUnit.ChildBag);
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var functionDefinitionNode = (FunctionDefinitionNode)codeBlockNode.ChildBag[0];
        Assert.Equal(SyntaxKind.FunctionDefinitionNode, functionDefinitionNode.SyntaxKind);
        Assert.Equal("void", functionDefinitionNode.ReturnTypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(functionIdentifier, functionDefinitionNode.FunctionIdentifier.TextSpan.GetText());
        Assert.NotNull(functionDefinitionNode.FunctionBodyCodeBlockNode);
        Assert.Empty(functionDefinitionNode.FunctionBodyCodeBlockNode!.ChildBag);
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_DEFINITION_STATEMENT_WITH_UNDEFINED_RETURN_TYPE()
    {
        var returnTypeIdentifier = "IPerson";
        var functionIdentifier = "WriteHelloWorldToConsole";
        var sourceText = @$"{returnTypeIdentifier} {functionIdentifier}(){{}}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        Assert.Single(compilationUnit.DiagnosticsBag);
        var undefinedTypeDiagnostic = compilationUnit.DiagnosticsBag[0];

        var expectedDiagnosticBag = new LuthetusDiagnosticBag();
        expectedDiagnosticBag.ReportUndefinedTypeOrNamespace(new(0, 0, 0, new(""), ""), "");
        Assert.Equal(expectedDiagnosticBag.ElementAt(0).Id, undefinedTypeDiagnostic.Id);

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;
        var functionDefinitionNode = (FunctionDefinitionNode)topLevelStatementsCodeBlockNode.ChildBag[0];
        Assert.Equal(SyntaxKind.FunctionDefinitionNode, functionDefinitionNode.SyntaxKind);
        Assert.Equal(returnTypeIdentifier, functionDefinitionNode.ReturnTypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(functionIdentifier, functionDefinitionNode.FunctionIdentifier.TextSpan.GetText());
        Assert.NotNull(functionDefinitionNode.FunctionBodyCodeBlockNode);
        Assert.Empty(functionDefinitionNode.FunctionBodyCodeBlockNode.ChildBag);
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_DEFINITION_STATEMENT_WITH_UNDEFINED_GENERIC_RETURN_TYPE()
    {
        var returnTypeIdentifier = "IBox";
        var returnTypeGenericArgument = "string";
        var functionIdentifier = "WriteHelloWorldToConsole";
        var sourceText = @$"{returnTypeIdentifier}<{returnTypeGenericArgument}> {functionIdentifier}(){{}}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        throw new NotImplementedException("Add assertions");
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_DEFINITION_STATEMENT_WITH_ONE_ARGUMENT()
    {
        var functionReturnTypeClauseText = "void";
        var functionIdentifierText = "WriteHelloWorldToConsole";
        var argumentTypeClauseText = "int";
        var argumentIdentifierText = "times";
        var sourceText = @$"{functionReturnTypeClauseText} {functionIdentifierText}({argumentTypeClauseText} {argumentIdentifierText}) {{}}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var functionDefinitionNode = (FunctionDefinitionNode)topLevelStatementsCodeBlockNode.ChildBag.Single();
        Assert.Equal(SyntaxKind.FunctionDefinitionNode, functionDefinitionNode.SyntaxKind);
        Assert.Equal(functionReturnTypeClauseText, functionDefinitionNode.ReturnTypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(functionIdentifierText, functionDefinitionNode.FunctionIdentifier.TextSpan.GetText());
        Assert.NotNull(functionDefinitionNode.FunctionBodyCodeBlockNode);
        Assert.Empty(functionDefinitionNode.FunctionBodyCodeBlockNode.ChildBag);

        var functionArgumentsListingNode = functionDefinitionNode.FunctionArgumentsListingNode;

        var singularArgument = functionArgumentsListingNode.FunctionArgumentEntryNodeBag.Single();
        Assert.Equal(argumentTypeClauseText, singularArgument.VariableDeclarationStatementNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(argumentIdentifierText, singularArgument.VariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_DEFINITION_STATEMENT_WITH_TWO_ARGUMENTS()
    {
        var functionReturnTypeClauseText = "void";
        var functionIdentifierText = "WriteHelloWorldToConsole";
        var firstArgumentTypeClauseText = "int";
        var firstArgumentIdentifierText = "times";
        var secondArgumentTypeClauseText = "bool";
        var secondArgumentIdentifierText = "usePurpleText";
        var sourceText = @$"{functionReturnTypeClauseText} {functionIdentifierText}({firstArgumentTypeClauseText} {firstArgumentIdentifierText}, {secondArgumentTypeClauseText} {secondArgumentIdentifierText}){{}}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        Assert.Single(compilationUnit.ChildBag);

        // TODO: The logic for the Asserts, below this comment, are nonsensical. (2023-08-06)
        //
        //var boundFunctionDefinitionNode = (FunctionDefinitionNode)compilationUnit.Children.Single();
        //Assert.Equal(SyntaxKind.FunctionDefinitionNode, boundFunctionDefinitionNode.SyntaxKind);
        //// Return Type
        //Assert.Equal(functionReturnTypeClauseText, boundFunctionDefinitionNode.ReturnTypeClauseNode.TypeClauseToken.TextSpan.GetText());
        //Assert.Equal(typeof(void), boundFunctionDefinitionNode.ReturnTypeClauseNode.Type);
        //// Function Identifier
        //Assert.Equal(functionIdentifierText, boundFunctionDefinitionNode.FunctionIdentifier.TextSpan.GetText());
        //// First Argument
        //{
        //    // Argument Type
        //    var boundClassReferenceNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[0];
        //    Assert.Equal(typeof(int), boundClassReferenceNode.Type);
        //    Assert.Equal(firstArgumentTypeClauseText, boundClassReferenceNode.Identifier.TextSpan.GetText());

        //    // Argument Identifier
        //    var boundVariableDeclarationStatementNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[1];
        //    Assert.Equal(firstArgumentIdentifierText, boundVariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());
        //}
        //// Second Argument
        //{
        //    // Argument Type
        //    var boundClassReferenceNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[2];
        //    Assert.Equal(typeof(bool), boundClassReferenceNode.Type);
        //    Assert.Equal(secondArgumentTypeClauseText, boundClassReferenceNode.Identifier.TextSpan.GetText());

        //    // Argument Identifier
        //    var boundVariableDeclarationStatementNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[3];
        //    Assert.Equal(secondArgumentIdentifierText, boundVariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());
        //}
        //// Body CompilationUnit
        //Assert.Empty(boundFunctionDefinitionNode.FunctionBodyCodeBlockNode!.Children);
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_DEFINITION_STATEMENT_WITH_THREE_ARGUMENTS()
    {
        string functionReturnTypeClauseText = "void";
        string functionIdentifierText = "WriteHelloWorldToConsole";
        string firstArgumentTypeClauseText = "int";
        string firstArgumentIdentifierText = "times";
        string secondArgumentTypeClauseText = "bool";
        string secondArgumentIdentifierText = "usePurpleText";
        string thirdArgumentTypeClauseText = "string";
        string thirdArgumentIdentifierText = "additionalText";
        string sourceText = @$"{functionReturnTypeClauseText} {functionIdentifierText}({firstArgumentTypeClauseText} {firstArgumentIdentifierText}, {secondArgumentTypeClauseText} {secondArgumentIdentifierText}, {thirdArgumentTypeClauseText} {thirdArgumentIdentifierText}){{}}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        Assert.Single(compilationUnit.ChildBag);

        // TODO: The logic for the Asserts, below this comment, are nonsensical. (2023-08-06)
        //
        //var boundFunctionDefinitionNode = (FunctionDefinitionNode)compilationUnit.Children.Single();
        //Assert.Equal(SyntaxKind.FunctionDefinitionNode, boundFunctionDefinitionNode.SyntaxKind);
        //// Return Type
        //Assert.Equal(functionReturnTypeClauseText, boundFunctionDefinitionNode.ReturnTypeClauseNode.TypeIdentifier.TextSpan.GetText());
        //Assert.Equal(typeof(void), boundFunctionDefinitionNode.ReturnTypeClauseNode.Type);
        //// Function Identifier
        //Assert.Equal(functionIdentifierText, boundFunctionDefinitionNode.FunctionIdentifier.TextSpan.GetText());
        //// First Argument
        //{
        //    // Argument Type
        //    var boundClassReferenceNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[0];
        //    Assert.Equal(typeof(int), boundClassReferenceNode.Type);
        //    Assert.Equal(firstArgumentTypeClauseText, boundClassReferenceNode.Identifier.TextSpan.GetText());

        //    // Argument Identifier
        //    var boundVariableDeclarationStatementNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[1];
        //    Assert.Equal(firstArgumentIdentifierText, boundVariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());
        //}
        //// Second Argument
        //{
        //    // Argument Type
        //    var boundClassReferenceNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[2];
        //    Assert.Equal(typeof(bool), boundClassReferenceNode.Type);
        //    Assert.Equal(secondArgumentTypeClauseText, boundClassReferenceNode.Identifier.TextSpan.GetText());

        //    // Argument Identifier
        //    var boundVariableDeclarationStatementNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[3];
        //    Assert.Equal(secondArgumentIdentifierText, boundVariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());
        //}
        //// Third Argument
        //{
        //    // Argument Type
        //    var boundClassReferenceNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[4];
        //    Assert.Equal(typeof(string), boundClassReferenceNode.Type);
        //    Assert.Equal(thirdArgumentTypeClauseText, boundClassReferenceNode.Identifier.TextSpan.GetText());

        //    // Argument Identifier
        //    var boundVariableDeclarationStatementNode = boundFunctionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodes[5];
        //    Assert.Equal(thirdArgumentIdentifierText, boundVariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());
        //}
        //// Body CompilationUnit
        //Assert.Empty(boundFunctionDefinitionNode.FunctionBodyCodeBlockNode!.Children);
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_DEFINITION_STATEMENT_WITH_OPTIONAL_ARGUMENT()
    {
        var functionReturnTypeIdentifier = "void";
        var functionIdentifier = "WriteHelloWorldToConsole";
        var optionalParameterTypeIdentifier = "int";
        var optionalParameterVariableIdentifier = "times";
        var optionalParameterCompileTimeConstantValue = "1";
        string sourceText = @$"{functionReturnTypeIdentifier} {functionIdentifier}({optionalParameterTypeIdentifier} {optionalParameterVariableIdentifier} = {optionalParameterCompileTimeConstantValue}){{}}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        Assert.Empty(compilationUnit.Binder.DiagnosticsBag);
        Assert.Single(codeBlockNode.ChildBag);

        var functionDefinitionNode = (FunctionDefinitionNode)codeBlockNode.ChildBag.Single();
        Assert.Equal(functionReturnTypeIdentifier, functionDefinitionNode.ReturnTypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(functionIdentifier, functionDefinitionNode.FunctionIdentifier.TextSpan.GetText());
        Assert.Single(functionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodeBag);

        var optionalArgumentListingNode = functionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodeBag.Single();
        Assert.Equal(optionalParameterTypeIdentifier, optionalArgumentListingNode.VariableDeclarationStatementNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(optionalParameterVariableIdentifier, optionalArgumentListingNode.VariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());

        // TODO: Assert the 'optionalParameterCompileTimeConstantValue'
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_NO_ARGUMENT()
    {
        string functionInvocationIdentifier = "WriteToConsole";
        string sourceText = @$"{functionInvocationIdentifier}();".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var functionInvocationNode = (FunctionInvocationNode)topLevelStatementsCodeBlockNode.ChildBag.Single();

        Assert.Empty(functionInvocationNode.FunctionParametersListingNode.FunctionParameterEntryNodeBag);
        Assert.Equal(functionInvocationIdentifier, functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_WITH_STRING_LITERAL_ARGUMENT()
    {
        string sourceText = @"void WriteToConsole(string input){} WriteToConsole(""Aaa"");".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        Assert.Equal(2, topLevelStatementsCodeBlockNode.ChildBag.Length);

        var functionDefinitionNode = (FunctionDefinitionNode)topLevelStatementsCodeBlockNode.ChildBag[0];
        var functionInvocationNode = (FunctionInvocationNode)topLevelStatementsCodeBlockNode.ChildBag[1];

        Assert.Equal(SyntaxKind.FunctionDefinitionNode, functionDefinitionNode.SyntaxKind);
        Assert.Equal(SyntaxKind.FunctionInvocationNode, functionInvocationNode.SyntaxKind);
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_WITH_NUMERIC_LITERAL_ARGUMENT()
    {
        string sourceText = @"void WriteToConsole(int input){} WriteToConsole(31);".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_WITH_EXPRESSION_ARGUMENT()
    {
        string sourceText = @"void WriteToConsole(string input){} WriteToConsole(""a"" + ""b"");".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_WITH_VARIABLE_ARGUMENT()
    {
        string sourceText = @"int x = 2; void WriteToConsole(int input){} WriteToConsole(x);".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_HAVING_OUT_VARIABLE_ARGUMENT_REFERENCE()
    {
        string sourceText = @"int x = 2; void WriteToConsole(out int input){} WriteToConsole(out x);".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        Assert.Equal(4, codeBlockNode.ChildBag.Length);

        var variableDeclarationStatementNode = (VariableDeclarationStatementNode)codeBlockNode.ChildBag[0];
        var variableAssignmentExpressionNode = (VariableAssignmentExpressionNode)codeBlockNode.ChildBag[1];
        var functionDefinitionNode = (FunctionDefinitionNode)codeBlockNode.ChildBag[2];
        var functionInvocationNode = (FunctionInvocationNode)codeBlockNode.ChildBag[3];

        Assert.NotNull(variableDeclarationStatementNode);
        Assert.NotNull(variableAssignmentExpressionNode);
        Assert.NotNull(functionDefinitionNode);
        Assert.NotNull(functionInvocationNode);
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_HAVING_OUT_VARIABLE_ARGUMENT_DECLARATION()
    {
        string sourceText = @"void WriteToConsole(out int input){} WriteToConsole(out int x);".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_HAVING_REF_VARIABLE_ARGUMENT_REFERENCE()
    {
        string sourceText = @"int x = 2; void WriteToConsole(ref int input){} WriteToConsole(ref x);".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        Assert.Equal(4, codeBlockNode.ChildBag.Length);

        var variableDeclarationStatementNode = (VariableDeclarationStatementNode)codeBlockNode.ChildBag[0];
        var variableAssignmentExpressionNode = (VariableAssignmentExpressionNode)codeBlockNode.ChildBag[1];
        var functionDefinitionNode = (FunctionDefinitionNode)codeBlockNode.ChildBag[2];
        var functionInvocationNode = (FunctionInvocationNode)codeBlockNode.ChildBag[3];

        Assert.NotNull(variableDeclarationStatementNode);
        Assert.NotNull(variableAssignmentExpressionNode);
        Assert.NotNull(functionDefinitionNode);
        Assert.NotNull(functionInvocationNode);
    }

    [Fact]
    public void SHOULD_PARSE_FUNCTION_INVOCATION_STATEMENT_WITH_DIAGNOSTIC_FOR_UNDEFINED_FUNCTION()
    {
        string sourceText = @"printf();".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        Assert.Single(compilationUnit.DiagnosticsBag);

        var errorDiagnostic = compilationUnit.DiagnosticsBag.Single();
        Assert.Equal(TextEditorDiagnosticLevel.Error, errorDiagnostic.DiagnosticLevel);

        var functionInvocationNode = (FunctionInvocationNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag.Single();
        Assert.Equal(SyntaxKind.FunctionInvocationNode, functionInvocationNode.SyntaxKind);
    }

    [Fact]
    public void SHOULD_PARSE_METHOD_INVOCATION_ON_CLASS_INSTANCE()
    {
        var sourceText = @"TODO".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_METHOD_INVOCATION_ON_STATIC_CLASS_WITH_USING_STATEMENT()
    {
        var sourceText = @"using System; Console.WriteLine(""Hello World!"");".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_METHOD_INVOCATION_ON_STATIC_CLASS_WITH_EXPLICIT_NAMESPACE_QUALIFICATION()
    {
        var sourceText = @"System.Console.WriteLine(""Hello World!"");".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_METHOD_DEFINITION_WITH_GENERIC_ARGUMENT()
    {
        var genericArgumentIdentifier = "T";
        var functionIdentifier = "Clone";
        var functionArgumentIdentifier = "item";
        var sourceText = @$"public {genericArgumentIdentifier} {functionIdentifier}<{genericArgumentIdentifier}>({genericArgumentIdentifier} {functionArgumentIdentifier}) {{ return {functionArgumentIdentifier}; }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var functionDefinitionNode = (FunctionDefinitionNode)topLevelStatementsCodeBlockNode.ChildBag.Single();
        Assert.Equal(SyntaxKind.FunctionDefinitionNode, functionDefinitionNode.SyntaxKind);
        Assert.Equal(genericArgumentIdentifier, functionDefinitionNode.ReturnTypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(functionIdentifier, functionDefinitionNode.FunctionIdentifier.TextSpan.GetText());
        Assert.NotNull(functionDefinitionNode.FunctionBodyCodeBlockNode);
        Assert.NotNull(functionDefinitionNode.GenericArgumentsListingNode);

        var genericArgument = functionDefinitionNode.GenericArgumentsListingNode.GenericArgumentEntryNodeBag.Single();
        Assert.Equal(genericArgumentIdentifier, genericArgument.TypeClauseNode.TypeIdentifier.TextSpan.GetText());

        Assert.NotNull(functionDefinitionNode.FunctionArgumentsListingNode);

        var functionArgument = functionDefinitionNode.FunctionArgumentsListingNode.FunctionArgumentEntryNodeBag.Single();
        Assert.Equal(genericArgumentIdentifier, functionArgument.VariableDeclarationStatementNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
        Assert.Equal(functionArgumentIdentifier, functionArgument.VariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_METHOD_DEFINITION_WITH_GENERIC_ARGUMENT_CLAUSE()
    {
        var sourceText = @"public T Clone<T>(T item) where T : class { return item; }".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_METHOD_INVOCATION_WITH_GENERIC_ARGUMENT()
    {
        var sourceText = @"Clone<int>(3){}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_NUMERIC_BINARY_EXPRESSION()
    {
        string sourceText = "3 + 3".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;
        Assert.Single(codeBlockNode.ChildBag);

        var boundBinaryExpressionNode = (BinaryExpressionNode)codeBlockNode.ChildBag[0];
        Assert.Equal(typeof(int), boundBinaryExpressionNode.LeftExpressionNode.TypeClauseNode?.ValueType);
        Assert.Equal(typeof(int), boundBinaryExpressionNode.BinaryOperatorNode.TypeClauseNode.ValueType);
        Assert.Equal(typeof(int), boundBinaryExpressionNode.RightExpressionNode.TypeClauseNode?.ValueType);
    }

    [Fact]
    public void SHOULD_PARSE_STRING_INTERPOLATION_EXPRESSION()
    {
        string sourceText = "$\"DisplayName: {FirstName} {LastName}\"".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException();
    }

    [Fact]
    public void SHOULD_PARSE_IF_STATEMENT()
    {
        var sourceText = @"if (true) { }".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var codeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var ifStatementNode = (IfStatementNode)codeBlockNode.ChildBag.Single();
        Assert.NotNull(ifStatementNode.KeywordToken);
        Assert.NotNull(ifStatementNode.ExpressionNode);
        Assert.NotNull(ifStatementNode.IfStatementBodyCodeBlockNode);
    }

    [Fact]
    public void SHOULD_NOT_PARSE_COMMENT_SINGLE_LINE_STATEMENT()
    {
        string sourceText = @"// C:\Users\hunte\Repos\Aaa\".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;
        Assert.Empty(topLevelStatementsCodeBlockNode.ChildBag);
    }

    [Fact]
    public void SHOULD_PARSE_CLASS_DEFINITION_EMPTY()
    {
        string classIdentifier = "PersonModel";
        string sourceText = @$"public class {classIdentifier} {{ }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var globalScope = parser.Binder.BoundScopes.First();
        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(classIdentifier));

        var typeDefinitionNode = (TypeDefinitionNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag.Single();

        if (typeDefinitionNode.TypeBodyCodeBlockNode is null)
            throw new ApplicationException("ClassBodyCompilationUnit should not be null here.");

        Assert.Equal(classIdentifier, typeDefinitionNode.TypeIdentifier.TextSpan.GetText());
        Assert.Empty(typeDefinitionNode.TypeBodyCodeBlockNode.ChildBag);
    }

    [Fact]
    public void SHOULD_PARSE_CLASS_DEFINITION_WHICH_CONTAINS_A_METHOD()
    {
        string classIdentifier = "PersonModel";
        string methodIdentifier = "Walk";
        string sourceText = @$"public class {classIdentifier} {{ public void {methodIdentifier}() {{ }} }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        
        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var globalScope = parser.Binder.BoundScopes.First();

        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(classIdentifier));

        var typeDefinitionNode = (TypeDefinitionNode)topLevelStatementsCodeBlockNode.ChildBag.Single();

        if (typeDefinitionNode.TypeBodyCodeBlockNode is null)
            throw new ApplicationException("ClassBodyCompilationUnit should not be null here.");

        Assert.Equal(classIdentifier, typeDefinitionNode.TypeIdentifier.TextSpan.GetText());

        var functionDefinitionNode = (FunctionDefinitionNode)typeDefinitionNode.TypeBodyCodeBlockNode.ChildBag.Single();
        Assert.Equal(SyntaxKind.FunctionDefinitionNode, functionDefinitionNode.SyntaxKind);
        Assert.Equal(methodIdentifier, functionDefinitionNode.FunctionIdentifier.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_CLASS_DEFINITION_WHICH_HAS_PARTIAL_MODIFIER()
    {
        string classIdentifier = "PersonModel";
        string sourceText = @$"public partial class {classIdentifier} {{ }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var globalScope = parser.Binder.BoundScopes.First();
        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(classIdentifier));

        var topLevelStatementsCodeBlockNode = compilationUnit.TopLevelStatementsCodeBlockNode;

        var typeDefinitionNode = (TypeDefinitionNode)topLevelStatementsCodeBlockNode.ChildBag.Single();

        if (typeDefinitionNode.TypeBodyCodeBlockNode is null)
            throw new ApplicationException($"{nameof(typeDefinitionNode.TypeBodyCodeBlockNode)} should not be null here.");

        Assert.Equal(classIdentifier, typeDefinitionNode.TypeIdentifier.TextSpan.GetText());
        Assert.Empty(typeDefinitionNode.TypeBodyCodeBlockNode.ChildBag);
    }

    [Fact]
    public void SHOULD_PARSE_CLASS_DEFINITION_WHICH_IS_INHERITING()
    {
        string parentClassIdentifier = "IPerson";
        string derivingClassIdentifier = "Person";
        string sourceText = @$"public interface {parentClassIdentifier} {{ }} public class {derivingClassIdentifier} : {parentClassIdentifier} {{ }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var globalScope = parser.Binder.BoundScopes.First();

        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(parentClassIdentifier));
        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(derivingClassIdentifier));

        var parentTypeDefinition = (TypeDefinitionNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag[0];
        var derivingTypeDefinition = (TypeDefinitionNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag[1];

        Assert.NotNull(derivingTypeDefinition.InheritedTypeClauseNode);
        Assert.Equal(parentClassIdentifier, derivingTypeDefinition.InheritedTypeClauseNode.TypeIdentifier.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_CLASS_DEFINITION_WHICH_HAS_ONE_GENERIC_ARGUMENT()
    {
        string classIdentifier = "Box", genericArgumentIdentifier = "T";
        string sourceText = @$"public class {classIdentifier}<{genericArgumentIdentifier}> {{ }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var globalScope = parser.Binder.BoundScopes.First();
        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(classIdentifier));

        var typeDefinitionNode = (TypeDefinitionNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag.Single();

        if (typeDefinitionNode.TypeBodyCodeBlockNode is null)
            throw new ApplicationException("ClassBodyCompilationUnit should not be null here.");

        Assert.Equal(classIdentifier, typeDefinitionNode.TypeIdentifier.TextSpan.GetText());
        Assert.Empty(typeDefinitionNode.TypeBodyCodeBlockNode.ChildBag);

        if (typeDefinitionNode.GenericArgumentsListingNode is null)
            throw new ApplicationException("ClassBodyCompilationUnit should not be null here.");

        Assert.Single(typeDefinitionNode.GenericArgumentsListingNode.GenericArgumentEntryNodeBag);

        var genericArgumentEntryNode = typeDefinitionNode.GenericArgumentsListingNode.GenericArgumentEntryNodeBag.Single();
        Assert.Equal(genericArgumentIdentifier, genericArgumentEntryNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_CLASS_DEFINITION_WHICH_HAS_TWO_GENERIC_ARGUMENTS()
    {
        string classIdentifier = "Box", genericArgOne = "TItem", genericArgTwo = "TPackager";
        string sourceText = @$"public class {classIdentifier}<{genericArgOne}, {genericArgTwo}> {{ }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var globalScope = parser.Binder.BoundScopes.First();
        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(classIdentifier));

        var typeDefinitionNode = (TypeDefinitionNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag.Single();

        if (typeDefinitionNode.TypeBodyCodeBlockNode is null)
            throw new ApplicationException("ClassBodyCompilationUnit should not be null here.");

        Assert.Equal(classIdentifier, typeDefinitionNode.TypeIdentifier.TextSpan.GetText());
        Assert.Empty(typeDefinitionNode.TypeBodyCodeBlockNode.ChildBag);

        if (typeDefinitionNode.GenericArgumentsListingNode is null)
            throw new ApplicationException("ClassBodyCompilationUnit should not be null here.");

        var firstGenericArgumentEntryNode = typeDefinitionNode.GenericArgumentsListingNode.GenericArgumentEntryNodeBag[0];
        Assert.Equal(genericArgOne, firstGenericArgumentEntryNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());

        var secondGenericArgumentEntryNode = typeDefinitionNode.GenericArgumentsListingNode.GenericArgumentEntryNodeBag[1];
        Assert.Equal(genericArgTwo, secondGenericArgumentEntryNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_CLASS_DEFINITION_WHICH_HAS_THREE_GENERIC_ARGUMENTS()
    {
        string classIdentifier = "Box", genericArgOne = "TItem", genericArgTwo = "TPackager", genericArgThree = "TDeliverer";
        string sourceText = @$"public class {classIdentifier}<{genericArgOne}, {genericArgTwo}, {genericArgThree}> : ComponentBase {{ }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        var globalScope = parser.Binder.BoundScopes.First();
        Assert.True(globalScope.TypeDefinitionMap.ContainsKey(classIdentifier));

        var typeDefinitionNode = (TypeDefinitionNode)compilationUnit.TopLevelStatementsCodeBlockNode.ChildBag.Single();

        if (typeDefinitionNode.TypeBodyCodeBlockNode is null)
            throw new ApplicationException("ClassBodyCompilationUnit should not be null here.");

        Assert.Equal(classIdentifier, typeDefinitionNode.TypeIdentifier.TextSpan.GetText());
        Assert.Empty(typeDefinitionNode.TypeBodyCodeBlockNode.ChildBag);

        if (typeDefinitionNode.GenericArgumentsListingNode is null)
            throw new ApplicationException("ClassBodyCompilationUnit should not be null here.");

        var firstGenericArgumentEntryNode = typeDefinitionNode.GenericArgumentsListingNode.GenericArgumentEntryNodeBag[0];
        Assert.Equal(genericArgOne, firstGenericArgumentEntryNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());

        var secondGenericArgumentEntryNode = typeDefinitionNode.GenericArgumentsListingNode.GenericArgumentEntryNodeBag[1];
        Assert.Equal(genericArgTwo, secondGenericArgumentEntryNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
        
        var thirdGenericArgumentEntryNode = typeDefinitionNode.GenericArgumentsListingNode.GenericArgumentEntryNodeBag[1];
        Assert.Equal(genericArgTwo, thirdGenericArgumentEntryNode.TypeClauseNode.TypeIdentifier.TextSpan.GetText());
    }

    [Fact]
    public void SHOULD_PARSE_PROPERTY_ATTRIBUTE()
    {
        var attributeIdentifier = "Parameter";
        string sourceText = @$"public partial class PersonDisplay : ComponentBase {{ [{attributeIdentifier}] public IPersonModel PersonModel {{ get; set; }} }}".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException("Need to add Assertions");
    }

    [Fact]
    public void SHOULD_PARSE_PROPERTY_WITH_TYPE_NOT_FOUND()
    {
        string sourceText = @"public class Aaa { public IPersonModel MyProperty { get; set; } }".ReplaceLineEndings("\n");
        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();

        throw new NotImplementedException("Need to add Assertions");
    }
}