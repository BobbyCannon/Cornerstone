#region References

using Cornerstone.VisualStudio.Tests.Manipulator.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests.Manipulator;

[TestClass]
public partial class ManipulatorBasicTests : ManipulatorTestBase
{
	#region Methods

	[TestMethod]
	public void DoesNotInsertWhenIncorrectNesting()
	{
		AssertInsertion("<Alpha$><Foo></Alpha>", "Beta", "<AlphaBeta><Foo></Alpha>");
	}

	[TestMethod]
	public void DoNotCloseTag()
	{
		AssertInsertion("<Alpha$></Alpha>", ">", "<Alpha>></Alpha>");
	}

	[TestMethod]
	public void DoNotInsertToAnotherTag()
	{
		AssertInsertion("<Alpha$><Gamma>", "Beta", "<AlphaBeta><Gamma>");
	}

	[TestMethod]
	public void DoNotInsertToUnclosedTag()
	{
		AssertInsertion("<Alpha$><Alpha>", "Beta", "<AlphaBeta><Alpha>");
	}

	[TestMethod]
	public void DoNotInsertWhitespace()
	{
		AssertInsertion("<Alpha$></Alpha>", "A O", "<AlphaA O></Alpha>");
	}

	[TestMethod]
	public void DoNotRemoveTag()
	{
		AssertReplacement("$<$Alpha></Alpha>", "a", "aAlpha></Alpha>");
		AssertReplacement("<Alpha$>$</Alpha>", "a", "<Alphaa</Alpha>");
	}

	[TestMethod]
	public void InsertsInClosingTagAtEnd()
	{
		AssertInsertion("<Alpha$></Alpha>", "Beta", "<AlphaBeta></AlphaBeta>");
	}

	[TestMethod]
	public void InsertsInClosingTagAtMiddle()
	{
		AssertInsertion("<Alpha$Beta></AlphaBeta>", "Phi", "<AlphaPhiBeta></AlphaPhiBeta>");
	}

	[TestMethod]
	public void InsertsInClosingTagAtStart()
	{
		AssertInsertion("<$Beta></Beta>", "Alpha", "<AlphaBeta></AlphaBeta>");
	}

	[TestMethod]
	[DataRow(".")]
	[DataRow("")]
	[DataRow("-")]
	[DataRow("a")]
	[DataRow("Ą")]
	[DataRow("1")]
	public void InsertsSpecialCharacters(string s)
	{
		AssertInsertion("<Alpha$><Alpha>", s, "<Alpha" + s + "><Alpha>");
	}

	[TestMethod]
	public void InsertsTextAtEndTagWithSubtag()
	{
		AssertInsertion("<Alpha$><Foo></Foo></Alpha>", "Beta", "<AlphaBeta><Foo></Foo></AlphaBeta>");
	}

	[TestMethod]
	public void InsertsTextAtEndTagWithSubtagSelfClosed()
	{
		AssertInsertion("<Alpha$><Foo/></Alpha>", "Beta", "<AlphaBeta><Foo/></AlphaBeta>");
	}

	[TestMethod]
	public void RemovesInClosingTagAtEnd()
	{
		AssertReplacement("<AlphaBeta$Omega$></AlphaBetaOmega>", "", "<AlphaBeta></AlphaBeta>");
	}

	[TestMethod]
	public void RemovesInClosingTagAtMiddle()
	{
		AssertReplacement("<Alpha$Phi$Beta></AlphaPhiBeta>", "", "<AlphaBeta></AlphaBeta>");
	}

	[TestMethod]
	public void RemovesInClosingTagAtStart()
	{
		AssertReplacement("<$Alpha$Beta></AlphaBeta>", "", "<Beta></Beta>");
	}

	[TestMethod]
	public void ReplacesInClosingTagAtEnd()
	{
		AssertReplacement("<AlphaBeta$Omega$></AlphaBetaOmega>", "Gamma", "<AlphaBetaGamma></AlphaBetaGamma>");
	}

	[TestMethod]
	public void ReplacesInClosingTagAtMiddle()
	{
		AssertReplacement("<Alpha$Phi$Beta></AlphaPhiBeta>", "Gamma", "<AlphaGammaBeta></AlphaGammaBeta>");
	}

	[TestMethod]
	public void ReplacesInClosingTagAtStart()
	{
		AssertReplacement("<$Alpha$Beta></AlphaBeta>", "Gamma", "<GammaBeta></GammaBeta>");
	}

	#endregion
}