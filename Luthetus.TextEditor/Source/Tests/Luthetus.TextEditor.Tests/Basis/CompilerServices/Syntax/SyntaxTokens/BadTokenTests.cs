﻿using Xunit;

namespace Luthetus.TextEditor.Tests.Basis.CompilerServices.Syntax.SyntaxTokens;

public sealed record BadTokenTests
{
	[Fact]
	public void BadToken()
	{
		//public BadToken(TextEditorTextSpan textSpan)
		throw new NotImplementedException();
	}

	[Fact]
	public void TextSpan()
	{
		//public TextEditorTextSpan TextSpan { get; }
		throw new NotImplementedException();
	}
	
	[Fact]
	public void SyntaxKind()
	{
		//public SyntaxKind SyntaxKind => SyntaxKind.BadToken;
		throw new NotImplementedException();
	}

	[Fact]
	public void IsFabricated()
	{
		//public bool IsFabricated { get; init; }
		throw new NotImplementedException();
	}
}