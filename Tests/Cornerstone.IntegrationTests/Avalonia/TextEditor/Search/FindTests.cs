#region References

using System.Linq;
using Cornerstone.Avalonia.TextEditor.Search;
using Cornerstone.Text.Document;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework.Legacy;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Search;

[TestClass]
public class FindTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ResultAtEnd()
	{
		var strategy = SearchStrategyFactory.Create("me", false, true, SearchMode.Normal);
		var text = new StringTextSource("result           // find me");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		AreEqual(1, results.Length, "One result should be found!");
		AreEqual("result           // find ".Length, results[0].StartIndex);
		AreEqual("me".Length, results[0].Length);
	}

	[TestMethod]
	public void ResultAtStart()
	{
		var strategy = SearchStrategyFactory.Create("result", false, true, SearchMode.Normal);
		var text = new StringTextSource("result           // find me");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		AreEqual(1, results.Length, "One result should be found!");
		AreEqual(0, results[0].StartIndex);
		AreEqual("result".Length, results[0].Length);
	}

	[TestMethod]
	public void SimpleTest()
	{
		var strategy = SearchStrategyFactory.Create("AllTests", false, false, SearchMode.Normal);
		var text = new StringTextSource("name=\"FindAllTests ");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		AreEqual(1, results.Length, "One result should be found!");
		AreEqual("name=\"Find".Length, results[0].StartIndex);
		AreEqual("AllTests".Length, results[0].Length);
	}

	[TestMethod]
	public void SkipWordBorder()
	{
		var strategy = SearchStrategyFactory.Create("AllTests", false, true, SearchMode.Normal);
		var text = new StringTextSource("name=\"{FindAllTests}\"");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		ClassicAssert.IsEmpty(results, "No results should be found!");
	}

	[TestMethod]
	public void SkipWordBorder2()
	{
		var strategy = SearchStrategyFactory.Create("AllTests", false, true, SearchMode.Normal);
		var text = new StringTextSource("name=\"FindAllTests ");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		ClassicAssert.IsEmpty(results, "No results should be found!");
	}

	[TestMethod]
	public void SkipWordBorder3()
	{
		var strategy = SearchStrategyFactory.Create("// find", false, true, SearchMode.Normal);
		var text = new StringTextSource("            // findtest");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		ClassicAssert.IsEmpty(results, "No results should be found!");
	}

	[TestMethod]
	public void SkipWordBorderSimple()
	{
		var strategy = SearchStrategyFactory.Create("All", false, true, SearchMode.Normal);
		var text = new StringTextSource(" FindAllTests ");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		ClassicAssert.IsEmpty(results, "No results should be found!");
	}

	[TestMethod]
	public void TextWithDots()
	{
		var strategy = SearchStrategyFactory.Create("Text", false, true, SearchMode.Normal);
		var text = new StringTextSource(".Text.");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		AreEqual(1, results.Length, "One result should be found!");
		AreEqual(".".Length, results[0].StartIndex);
		AreEqual("Text".Length, results[0].Length);
	}

	[TestMethod]
	public void WordBorderTest()
	{
		var strategy = SearchStrategyFactory.Create("// find", false, true, SearchMode.Normal);
		var text = new StringTextSource("            // find me");
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		AreEqual(1, results.Length, "One result should be found!");
		AreEqual("            ".Length, results[0].StartIndex);
		AreEqual("// find".Length, results[0].Length);
	}

	#endregion
}