#nullable enable

#region References

using System;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Parsers;

[TestClass]
public class PropertyParserTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void FailsWithEmptyString()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader(ReadOnlySpan<char>.Empty)));
		CornerstoneTest.AreEqual(0, ex.Column);
		CornerstoneTest.AreEqual("Expected property name.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithInvalidPropertyName()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("123".AsSpan())));
		CornerstoneTest.AreEqual(0, ex.Column);
		CornerstoneTest.AreEqual("Unexpected '1'.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithInvalidPropertyNameAfterOwner()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo.123".AsSpan())));
		CornerstoneTest.AreEqual(4, ex.Column);
		CornerstoneTest.AreEqual("Unexpected '1'.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithLeadingWhitespace()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader(" Foo".AsSpan())));
		CornerstoneTest.AreEqual(0, ex.Column);
		CornerstoneTest.AreEqual("Unexpected ' '.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithMissingCloseParens()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("(Foo.Bar".AsSpan())));
		CornerstoneTest.AreEqual(8, ex.Column);
		CornerstoneTest.AreEqual("Expected ')'.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithOnlyWhitespace()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("  ".AsSpan())));
		CornerstoneTest.AreEqual(0, ex.Column);
		CornerstoneTest.AreEqual("Unexpected ' '.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithParensAndNamespaceButNoOwner()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("(foo:Bar)".AsSpan())));
		CornerstoneTest.AreEqual(1, ex.Column);
		CornerstoneTest.AreEqual("Expected property owner.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithParensButNoOwner()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("(Foo)".AsSpan())));
		CornerstoneTest.AreEqual(1, ex.Column);
		CornerstoneTest.AreEqual("Expected property owner.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithTooManyNamespaces()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("foo:bar:Baz".AsSpan())));
		CornerstoneTest.AreEqual(8, ex.Column);
		CornerstoneTest.AreEqual("Unexpected ':'.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithTooManySegments()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo.Bar.Baz".AsSpan())));
		CornerstoneTest.AreEqual(8, ex.Column);
		CornerstoneTest.AreEqual("Unexpected '.'.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithTrailingJunk()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo%".AsSpan())));
		CornerstoneTest.AreEqual(3, ex.Column);
		CornerstoneTest.AreEqual("Unexpected '%'.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithTrailingWhitespace()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo ".AsSpan())));
		CornerstoneTest.AreEqual(3, ex.Column);
		CornerstoneTest.AreEqual("Unexpected ' '.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithUnexpectedCloseParens()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo.Bar)".AsSpan())));
		CornerstoneTest.AreEqual(7, ex.Column);
		CornerstoneTest.AreEqual("Unexpected ')'.", ex.Message);
	}

	[PresentationTestMethod]
	public void FailsWithWhitespaceBetweenOwnerAndName()
	{
		var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo. Bar".AsSpan())));
		CornerstoneTest.AreEqual(4, ex.Column);
		CornerstoneTest.AreEqual("Unexpected ' '.", ex.Message);
	}

	[PresentationTestMethod]
	public void ParsesName()
	{
		var reader = new CharacterReader("Foo".AsSpan());
		var (ns, owner, name) = PropertyParser.Parse(reader);

		CornerstoneTest.IsNull(ns);
		CornerstoneTest.IsNull(owner);
		CornerstoneTest.AreEqual("Foo", name);
	}

	[PresentationTestMethod]
	public void ParsesNamespaceOwnerAndName()
	{
		var reader = new CharacterReader("foo:Bar.Baz".AsSpan());
		var (ns, owner, name) = PropertyParser.Parse(reader);

		CornerstoneTest.AreEqual("foo", ns);
		CornerstoneTest.AreEqual("Bar", owner);
		CornerstoneTest.AreEqual("Baz", name);
	}

	[PresentationTestMethod]
	public void ParsesNamespaceOwnerAndNameWithParentheses()
	{
		var reader = new CharacterReader("(foo:Bar.Baz)".AsSpan());
		var (ns, owner, name) = PropertyParser.Parse(reader);

		CornerstoneTest.AreEqual("foo", ns);
		CornerstoneTest.AreEqual("Bar", owner);
		CornerstoneTest.AreEqual("Baz", name);
	}

	[PresentationTestMethod]
	public void ParsesOwnerAndName()
	{
		var reader = new CharacterReader("Foo.Bar".AsSpan());
		var (ns, owner, name) = PropertyParser.Parse(reader);

		CornerstoneTest.IsNull(ns);
		CornerstoneTest.AreEqual("Foo", owner);
		CornerstoneTest.AreEqual("Bar", name);
	}

	[PresentationTestMethod]
	public void ParsesOwnerAndNameWithParentheses()
	{
		var reader = new CharacterReader("(Foo.Bar)".AsSpan());
		var (ns, owner, name) = PropertyParser.Parse(reader);

		CornerstoneTest.IsNull(ns);
		CornerstoneTest.AreEqual("Foo", owner);
		CornerstoneTest.AreEqual("Bar", name);
	}

	#endregion
}