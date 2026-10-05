#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core.Parsers;

[TestClass]
public partial class BindingExpressionGrammarTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldParseConsecutiveIndexers()
	{
		var result = Parse("Foo[15][16]");

		CornerstoneTest.AreEqual(3, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "15");
		AssertIsIndexer(result[2], "16");
	}

	[PresentationTestMethod]
	public void ShouldParseDot()
	{
		var result = Parse(".");
		var node = CornerstoneTest.Single(result);

		CornerstoneTest.IsType<BindingExpressionGrammar.EmptyExpressionNode>(node);
	}

	[PresentationTestMethod]
	public void ShouldParseDoubleNegatedPropertyChain()
	{
		var result = Parse("!!Foo.Bar.Baz");

		CornerstoneTest.AreEqual(5, result.Count);
		CornerstoneTest.IsType<BindingExpressionGrammar.NotNode>(result[0]);
		CornerstoneTest.IsType<BindingExpressionGrammar.NotNode>(result[1]);
		AssertIsProperty(result[2], "Foo");
		AssertIsProperty(result[3], "Bar");
		AssertIsProperty(result[4], "Baz");
	}

	[PresentationTestMethod]
	public void ShouldParseIndexedProperty()
	{
		var result = Parse("Foo[15]");

		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "15");
	}

	[PresentationTestMethod]
	public void ShouldParseIndexedPropertyInChain()
	{
		var result = Parse("Foo.Bar[5, 6].Baz");

		CornerstoneTest.AreEqual(4, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsProperty(result[1], "Bar");
		AssertIsIndexer(result[2], "5", "6");
		AssertIsProperty(result[3], "Baz");
	}

	[PresentationTestMethod]
	public void ShouldParseIndexedPropertyStringIndex()
	{
		var result = Parse("Foo[Key]");

		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "Key");
	}

	[PresentationTestMethod]
	public void ShouldParseMultipleIndexedProperty()
	{
		var result = Parse("Foo[15,6]");

		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "15", "6");
	}

	[PresentationTestMethod]
	public void ShouldParseMultipleIndexedPropertyWithSpace()
	{
		var result = Parse("Foo[5, 16]");

		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "5", "16");
	}

	[PresentationTestMethod]
	public void ShouldParseNegatedPropertyChain()
	{
		var result = Parse("!Foo.Bar.Baz");

		CornerstoneTest.AreEqual(4, result.Count);
		CornerstoneTest.IsType<BindingExpressionGrammar.NotNode>(result[0]);
		AssertIsProperty(result[1], "Foo");
		AssertIsProperty(result[2], "Bar");
		AssertIsProperty(result[3], "Baz");
	}

	[PresentationTestMethod]
	public void ShouldParseNullConditionalInPropertyChain1()
	{
		var result = Parse("Foo?.Bar.Baz");

		CornerstoneTest.AreEqual(3, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsProperty(result[1], "Bar", true);
		AssertIsProperty(result[2], "Baz");
	}

	[PresentationTestMethod]
	public void ShouldParseNullConditionalInPropertyChain2()
	{
		var result = Parse("Foo.Bar?.Baz");

		CornerstoneTest.AreEqual(3, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsProperty(result[1], "Bar");
		AssertIsProperty(result[2], "Baz", true);
	}

	[PresentationTestMethod]
	public void ShouldParseNullConditionalInPropertyChain3()
	{
		var result = Parse("Foo?.(Bar.Baz)");

		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsAttachedProperty(result[1], "Bar", "Baz", true);
	}

	[PresentationTestMethod]
	public void ShouldParsePropertyChain()
	{
		var result = Parse("Foo.Bar.Baz");

		CornerstoneTest.AreEqual(3, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsProperty(result[1], "Bar");
		AssertIsProperty(result[2], "Baz");
	}

	[PresentationTestMethod]
	public void ShouldParsePropertyChainWithAttachedProperty1()
	{
		var result = Parse("(Foo.Bar).Baz");

		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsAttachedProperty(result[0], "Foo", "Bar");
		AssertIsProperty(result[1], "Baz");
	}

	[PresentationTestMethod]
	public void ShouldParsePropertyChainWithAttachedProperty2()
	{
		var result = Parse("Foo.(Bar.Baz)");

		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsAttachedProperty(result[1], "Bar", "Baz");
	}

	[PresentationTestMethod]
	public void ShouldParsePropertyChainWithAttachedProperty3()
	{
		var result = Parse("Foo.(Bar.Baz).Last");

		CornerstoneTest.AreEqual(3, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsAttachedProperty(result[1], "Bar", "Baz");
		AssertIsProperty(result[2], "Last");
	}

	[PresentationTestMethod]
	public void ShouldParsePropertyWithDigits()
	{
		var result = Parse("F0o");
		var node = CornerstoneTest.Single(result);

		AssertIsProperty(node, "F0o");
	}

	[PresentationTestMethod]
	public void ShouldParseSingleAttachedProperty()
	{
		var result = Parse("(Foo.Bar)");
		var node = CornerstoneTest.Single(result);

		AssertIsAttachedProperty(node, "Foo", "Bar");
	}

	[PresentationTestMethod]
	public void ShouldParseSingleProperty()
	{
		var result = Parse("Foo");
		var node = CornerstoneTest.Single(result);

		AssertIsProperty(node, "Foo");
	}

	[PresentationTestMethod]
	public void ShouldParseStreamNode()
	{
		var result = Parse("Foo^");

		CornerstoneTest.AreEqual(2, result.Count);
		CornerstoneTest.IsType<BindingExpressionGrammar.StreamNode>(result[1]);
	}

	[PresentationTestMethod]
	public void ShouldParseStreamNodeAfterDot()
	{
		var result = Parse(".^");

		CornerstoneTest.AreEqual(2, result.Count);
		CornerstoneTest.IsType<BindingExpressionGrammar.EmptyExpressionNode>(result[0]);
		CornerstoneTest.IsType<BindingExpressionGrammar.StreamNode>(result[1]);
	}

	[PresentationTestMethod]
	public void ShouldParseStreamNodeOnEmptyExpression()
	{
		var result = Parse("^");

		CornerstoneTest.AreEqual(2, result.Count);
		CornerstoneTest.IsType<BindingExpressionGrammar.EmptyExpressionNode>(result[0]);
		CornerstoneTest.IsType<BindingExpressionGrammar.StreamNode>(result[1]);
	}

	[PresentationTestMethod]
	public void ShouldParseUnderscoredProperty()
	{
		var result = Parse("_Foo");
		var node = CornerstoneTest.Single(result);

		AssertIsProperty(node, "_Foo");
	}

	private static void AssertIsAttachedProperty(
		BindingExpressionGrammar.INode node,
		string typeName,
		string name,
		bool acceptsNull = false)
	{
		var p = CornerstoneTest.IsType<BindingExpressionGrammar.AttachedPropertyNameNode>(node);
		CornerstoneTest.AreEqual(typeName, p.TypeName);
		CornerstoneTest.AreEqual(name, p.PropertyName);
		CornerstoneTest.AreEqual(acceptsNull, p.AcceptsNull);
	}

	private static void AssertIsIndexer(BindingExpressionGrammar.INode node, params string[] args)
	{
		var e = CornerstoneTest.IsType<BindingExpressionGrammar.IndexerNode>(node);
		CornerstoneTest.AreEqual(e.Arguments, args);
	}

	private static void AssertIsProperty(
		BindingExpressionGrammar.INode node,
		string name,
		bool acceptsNull = false)
	{
		var p = CornerstoneTest.IsType<BindingExpressionGrammar.PropertyNameNode>(node);
		CornerstoneTest.AreEqual(name, p.PropertyName);
		CornerstoneTest.AreEqual(acceptsNull, p.AcceptsNull);
	}

	private static List<BindingExpressionGrammar.INode> Parse(string s)
	{
		var r = new CharacterReader(s);
		return BindingExpressionGrammar.Parse(ref r).Nodes;
	}

	#endregion
}