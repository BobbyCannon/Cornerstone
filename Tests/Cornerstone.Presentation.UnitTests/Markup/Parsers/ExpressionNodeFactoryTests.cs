#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Data.Core.ExpressionNodes;
using Cornerstone.Presentation.Data.Core.ExpressionNodes.Reflection;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Parsers;

[TestClass]
public class ExpressionNodeFactoryTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldBuildConsecutiveIndexers()
	{
		var result = Parse("Foo[15][16]");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(3, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "15");
		AssertIsIndexer(result[2], "16");
	}

	[PresentationTestMethod]
	public void ShouldBuildDot()
	{
		var result = Parse(".");

		CornerstoneTest.IsNull(result);
	}

	[PresentationTestMethod]
	public void ShouldBuildDoubleNegatedPropertyChain()
	{
		var result = Parse("!!Foo.Bar.Baz");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(5, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsProperty(result[1], "Bar");
		AssertIsProperty(result[2], "Baz");
		CornerstoneTest.IsType<LogicalNotNode>(result[3]);
		CornerstoneTest.IsType<LogicalNotNode>(result[4]);
	}

	[PresentationTestMethod]
	public void ShouldBuildIndexedProperty()
	{
		var result = Parse("Foo[15]");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "15");
		CornerstoneTest.IsType<ReflectionIndexerNode>(result[1]);
	}

	[PresentationTestMethod]
	public void ShouldBuildIndexedPropertyInChain()
	{
		var result = Parse("Foo.Bar[5, 6].Baz");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(4, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsProperty(result[1], "Bar");
		AssertIsIndexer(result[2], "5", "6");
		AssertIsProperty(result[3], "Baz");
	}

	[PresentationTestMethod]
	public void ShouldBuildIndexedPropertyStringIndex()
	{
		var result = Parse("Foo[Key]");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "Key");
		CornerstoneTest.IsType<ReflectionIndexerNode>(result[1]);
	}

	[PresentationTestMethod]
	public void ShouldBuildMultipleIndexedProperty()
	{
		var result = Parse("Foo[15,6]");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "15", "6");
	}

	[PresentationTestMethod]
	public void ShouldBuildMultipleIndexedPropertyWithSpace()
	{
		var result = Parse("Foo[5, 16]");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(2, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsIndexer(result[1], "5", "16");
	}

	[PresentationTestMethod]
	public void ShouldBuildNegatedPropertyChain()
	{
		var result = Parse("!Foo.Bar.Baz");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(4, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsProperty(result[1], "Bar");
		AssertIsProperty(result[2], "Baz");
		CornerstoneTest.IsType<LogicalNotNode>(result[3]);
	}

	[PresentationTestMethod]
	public void ShouldBuildPropertyChain()
	{
		var result = Parse("Foo.Bar.Baz");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(3, result.Count);
		AssertIsProperty(result[0], "Foo");
		AssertIsProperty(result[1], "Bar");
		AssertIsProperty(result[2], "Baz");
	}

	[PresentationTestMethod]
	public void ShouldBuildPropertyWithDigits()
	{
		var result = Parse("F0o");

		CornerstoneTest.IsNotNull(result);
		AssertIsProperty(result[0], "F0o");
	}

	[PresentationTestMethod]
	public void ShouldBuildSingleProperty()
	{
		var result = Parse("Foo");

		CornerstoneTest.IsNotNull(result);
		AssertIsProperty(result[0], "Foo");
	}

	[PresentationTestMethod]
	public void ShouldBuildStreamNode()
	{
		var result = Parse("Foo^");

		CornerstoneTest.IsNotNull(result);
		CornerstoneTest.AreEqual(2, result.Count);
		CornerstoneTest.IsType<DynamicPluginStreamNode>(result[1]);
	}

	[PresentationTestMethod]
	public void ShouldBuildUnderscoredProperty()
	{
		var result = Parse("_Foo");

		CornerstoneTest.IsNotNull(result);
		AssertIsProperty(result[0], "_Foo");
	}

	private static void AssertIsIndexer(ExpressionNode node, params string[] args)
	{
		var e = CornerstoneTest.IsType<ReflectionIndexerNode>(node);
		CornerstoneTest.AreEqual(e.Arguments.Cast<string>().ToArray(), args);
	}

	private static void AssertIsProperty(ExpressionNode node, string name)
	{
		var p = CornerstoneTest.IsType<DynamicPluginPropertyAccessorNode>(node);
		CornerstoneTest.AreEqual(name, p.PropertyName);
	}

	private static List<ExpressionNode> Parse(string path)
	{
		var reader = new CharacterReader(path.AsSpan());
		var (astNodes, sourceMode) = BindingExpressionGrammar.Parse(ref reader);
		return ExpressionNodeFactory.CreateFromAst(astNodes, null, null, out _);
	}

	#endregion
}