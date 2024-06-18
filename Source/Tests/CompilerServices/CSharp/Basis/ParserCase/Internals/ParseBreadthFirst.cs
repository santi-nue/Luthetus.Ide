using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.Nodes;
using Luthetus.CompilerServices.Lang.CSharp.LexerCase;
using Luthetus.CompilerServices.Lang.CSharp.ParserCase;

namespace Luthetus.CompilerServices.Lang.CSharp.Tests.Basis.ParserCase.Internals;

/*
This file was made after working on 'ParsePrototypes.cs'
and getting the idea that breadth first search is a requirement
(probably other ways to do this like parsing twice or something I'm not sure).

One has to perform a breadth first search per scope?

I should be parse starting with top level statements.

From there any immediate child scopes need to be breadth first searched, do not go
any deeper you need to handle the topmost scopes first?

How would I handle one C# class in some file, referencing a C# class in another file?

If by happenstance I parse the first C# class, prior to parsing the class being referenced,
then the reference would be an error?

Do I therefore need to create a dependency graph?

Would I create a dependency graph prior to parsing?

How would I know the dependencies prior to parsing?

What if there is a circular reference, how do I detect this,
as opposed to a possible infinite loop in the parser?
*/

public class ParseBreadthFirst
{
	/// <summary>
	/// I need to enter the class definition,
	/// then see the 'MyClass' constructor BUT, do not enter the constructor scope.
	/// Instead continue at the class definition scope
	/// Thereby finding the FirstName property,
	/// and somehow "remember" to go back and inside the constructor scope.
	/// </summary>
	[Fact]
	public void ClassDefinition()
	{
		var resourceUri = new ResourceUri("UnitTests");
        var sourceText =
@"public class MyClass
{
	public MyClass(string firstName)
	{
		FirstName = firstName;
	}

	public string FirstName { get; set; }
}";

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topCodeBlock = compilationUnit.RootCodeBlockNode;

		var typeDefinitionNode = (TypeDefinitionNode)topCodeBlock.ChildList.Single();
		Console.WriteLine(typeDefinitionNode);

/*
I want to type out what I would do if someone asked me as a human to manually parse this.

The test data starts with only a class definition at the top level.
But, algorithmically I would start at character index 0,
I see a class definition,

so I skip ahead to its opening brace, then I skip until its ending brace.

I go to the next character and  its the end of the file.

So, now I have finished parsing the top most scope, but how do I go back and
parse the internals of the class definition.

I need to restart from character index 0.
I'm presuming that for any syntax, I would know the parts of it that I
come back to. What I mean is, the class internals are bounded by brace characters.
And, a second example would be a function definition, the arguments I come back to,
and they are bounded by parenthesis, therefore I can do that. And the function code block
is bounded by brace characters so I can come back to that as well.

I can mark the start and stop of each sub-syntax?

A function has arguments.
And yet, what if the function returns a type that was defined in the same file
but at deeper into the document?

Presumably this isn't an issue because again, I just mark where these "sub-syntax"(s)
are.

I'm not looking to make the nodes mutable though. So how do I construct the node initially,
then fill in the rest later, while still being immutable?

The first idea is to have a 'Builder' pattern.

Then, I can parse each scope, breadth first, and afterwards iterate from the top to bottom of that scope
and if the node is a 'Builder' then I know it isn't finished. It needs to be parsed again
but at the scope of the syntax node itself.

Its sort of odd to think about but a generic function definition,
it has the generic argument deeper in the document then the return type,

public T Copy<T>(T item)
{
	
}

With the copy method I wrote here. The
return type is 'T', and the argument list
contains an entry of which the type is 'T'.

Where is the type 'T'? It is after the function identifier.

So, given naive parsing of this, the argument list
could know of the type 'T' since it is deeper in the document.

But, the return type 'T' comes prior to the generic argument,
and so a naive parsing of this would fail to know of the type 'T'
in the return signature.

I suppose what I'm getting at is, there seems to be not just
an order in which one parses "scopes" but also an order
for parsing any syntax at all.

All the syntaxes need to define the order in which to parse their "sub-syntax"?

Regarding the 'Builder' idea. Am I going to create a second class for every node type?
In other words TypeDefinitionNode needs a TypeDefinitionNodeBuilder.
Or do I use one 'Builder' type for all the nodes?

If the child list of nodes is immutable. Then everytime I 'Build' a 'Builder'
I'd need to replace the entire list of nodes?

I guess I can start with "create a second class for every node type",
and only focus on creating a second class for those that are necessary
to solve this unit test specifically.

Then after I solve this unit test, I'll have a better intuition
for how to go about things.

I don't need to add the nodes as I see them?

I can just track the "would be index" alongisde each node.

Then build the list at the end?

I need to see where the class definition parsing code is.
Because I want to identify the point that it goes into the
class's code block.

I'm not sure what I want to do but I just think
its a good start.

- Parse(...)
	  - while (true)
	      - ParseKeywordToken(...)
	            - HandleClassTokenKeyword(...)
		              - HandleStorageModifierTokenKeyword(...)
		                    - model.SyntaxStack.Push(typeDefinitionNode);
				            - return;
		              - return;
	            - return;
	      - continue;

I'm noticing that the class's code block does not track the open and closing braces?

There is a method named 'HandleStorageModifierTokenKeyword(...)'.
This method accepts a parameter 'ISyntaxToken consumedStorageModifierToken'.
Therefore, one would presume if I invoked 'HandleStorageModifierTokenKeyword(...)',
that I would provide a "storage modifier token".

But when parsing the class token keyword, I pass the 'class' token to the
'HandleStorageModifierTokenKeyword(...)'.

And the argument 'ISyntaxToken consumedStorageModifierToken' is actually the
'class' keyword?!?!?

Maybe I wrote the method with the idea that one provides the preceeding keyword,
and I named the method argument incorrectly?

================================================================================

I slept and came back to this file.

I don't know what I talkking about with the
"And the argument 'ISyntaxToken consumedStorageModifierToken' is actually the 'class' keyword?!?!?"
comment.

The storage modifier tokens are:
{
	Struct,
    Class,
    Interface,
    Enum,
    Record
}

So I'm not sure what I was getting at yesterday.

Interestingly, in 'HandleStorageModifierTokenKeyword(...)' I
currently am constructing a 'TypeDefinitionNode' which has not yet
had its code block parsed (among other things).

var typeDefinitionNode = new TypeDefinitionNode(
    accessModifierKind,
    hasPartialModifier,
    storageModifierKind.Value,
    identifierToken,
    valueType: null,
    genericArgumentsListingNode,
    primaryConstructorFunctionArgumentsListingNode: null,
    inheritedTypeClauseNode: null,
    typeBodyCodeBlockNode: null);

So where do I go from here?

I am pushing the incomplete typeDefinitionNode onto a stack.

Seemingly I then follow a sequence of 'return' statements back to the
CSharpParser's top level method with the while true loop over the tokens.

Maybe I can type what I think the stack is at this point.

Stack =
{
	***PUSH_AND_POP_LOCATION is on this side***
	
	typeDefinitionNode,
}

And I can write out the method invocations as they occur,
I'm at the while (true) here.

I'm not sure where the code will go next, so I'm adding
'Console.WriteLine(token.SyntaxKind);' temporarily
so I can see what token we are at.

Somehow, I am going from 'HandleStorageModifierTokenKeyword(...)'
to the OpenBraceToken.

Okay, I'm just being a fool that makes sense.

"public class MyClass { }"

With the knowledge that 'HandleStorageModifierTokenKeyword(...)'
awkwardly consumes the identifier token "MyClass",
then yes an open brace token at this step makes sense.

And, this step seems pivotal to the change I'm trying to make.
I don't want to go into the OpenBraceToken, I want to store
its start position, then come back after I parse everything else
at my current scope.

Considering that I've been looking at the code for
parsing a class definition.

It would perhaps be best that I look at two unit tests
for the moment.

One test having a single class definition,
the other test having two class definitions.

For the single class definition, I need to assert that
the open brace token was not entered, but instead skipped.
And that they just happened to be no further code at that scope,
so therefore go back to the open brace token and begin parsing
the deeper scope.

Got the two class definitions, I neeed to do
the same thing as with the single class definition.
i.e.: assert that the first class definition does not enter the
the open brace token for the class's code block,
but instead skips to the next syntax at the same scope level.
In this case there would be another syntax, its the second class definition,
so start parsing it, but again, do not enter the second class's body.
Now we get to the end of the scope, so go back to the first class's body,
then the second classes body.
And assertions on the order of these things would be very good to include.

If the Stack and the current code block's child list are used,
then the Stack allows for storing the 'changing' nodes that are not finalized.

Then once they are finalized, then move them to the code block's child list.

But, a stack would reverse the order of the child nodes,
when they are moved to the current code block's child list.

I can insert into the current code block's child list at index 0,
but would this mean every insertion needs to shift the other entries?

Is there a performance issue in relation to this shifting
being done every insertion?

If the stack where instead a queue, then I could insert at the end
of the current code block's child list and avoid any shifting.

The stack is nice, because I can see a token,
and in a sense say, "I don't have enough information"
so I push it onto the stack.

Then as I work my way through the tokens, I might
make sense of that token which I didn't have enough information for.

A queue doesn't provide the same functionality, because
the parser already goes left to right,
the stack permits me to reverse parsing direction from right to left
temporarily, as needed.

But, maybe a queue which is dedicated to deferred parsing
would be of use?

The difference here being that it isn't about lack of information
necessarily, but instead that I explicitly want to come back to
that spot later, because it would create a scope,
and I haven't finished parsing the current scope.

Ugh, I have all the code going through that main while loop
though.

It sounds like a pain to store a similar loop,
to be iterated later.

Sort of thinking to myself that I'd have to duplicate
the while loop code, but with token indices to start and end at?

Althought, I have access to the token walker.
The token walker has the current token index...
maybe I can add code to the token walker
such that it handles tracking my 'inner scope while loops' for me.

Because the CSharpParser's while true loop is invoking
'var token = model.TokenWalker.Consume();'.

So, that TokenWalker.Consume(...) method could internally
track "inner while loops".

And it would be entirely abstracted from the CSharpParser's
while true loop.

- Parse(...)
	  - while (true)
	      - ParseKeywordToken(...)
	            - HandleClassTokenKeyword(...)
		              - HandleStorageModifierTokenKeyword(...)
		                    - model.SyntaxStack.Push(typeDefinitionNode);
				            - return;
		              - return;
	            - return;
	      - continue;

*/

		throw new NotImplementedException();
	}

	[Fact]
	public void ClassDefinition()
	{
		var resourceUri = new ResourceUri("UnitTests");
        var sourceText =
@"public class MyClassOne
{
	public MyClass(string firstName)
	{
		FirstName = firstName;
	}

	public string FirstName { get; set; }
}

public class MyClassTwo
{
	public MyClass(string firstName)
	{
		FirstName = firstName;
	}

	public string FirstName { get; set; }
}";

        var lexer = new CSharpLexer(resourceUri, sourceText);
        lexer.Lex();
        var parser = new CSharpParser(lexer);
        var compilationUnit = parser.Parse();
        var topCodeBlock = compilationUnit.RootCodeBlockNode;

		var typeDefinitionNode = (TypeDefinitionNode)topCodeBlock.ChildList.Single();
		Console.WriteLine(typeDefinitionNode);
	}
}
