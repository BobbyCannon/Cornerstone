#region References

using Cornerstone.Text.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Document;

[TestClass]
public class TextUtilitiesTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void TestGetWhitespaceAfter()
	{
		AreEqual(new SimpleSegment(2, 3), TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \tb"), 2));
	}

	[TestMethod]
	public void TestGetWhitespaceAfterDoesNotSkipNewLine()
	{
		AreEqual(new SimpleSegment(2, 3), TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \tb"), 2));
	}

	[TestMethod]
	public void TestGetWhitespaceAfterEmptyResult()
	{
		AreEqual(new SimpleSegment(2, 0), TextUtilities.GetWhitespaceAfter(new StringTextSource("a b"), 2));
	}

	[TestMethod]
	public void TestGetWhitespaceAfterEndOfString()
	{
		AreEqual(new SimpleSegment(2, 0), TextUtilities.GetWhitespaceAfter(new StringTextSource("a "), 2));
	}

	[TestMethod]
	public void TestGetWhitespaceAfterUntilEndOfString()
	{
		AreEqual(new SimpleSegment(2, 3), TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \t"), 2));
	}

	[TestMethod]
	public void TestGetWhitespaceBefore()
	{
		AreEqual(new SimpleSegment(1, 3), TextUtilities.GetWhitespaceBefore(new StringTextSource("a\t \t b"), 4));
	}

	[TestMethod]
	public void TestGetWhitespaceBeforeDoesNotSkipNewLine()
	{
		AreEqual(new SimpleSegment(2, 1), TextUtilities.GetWhitespaceBefore(new StringTextSource("a\n b"), 3));
	}

	[TestMethod]
	public void TestGetWhitespaceBeforeEmptyResult()
	{
		AreEqual(new SimpleSegment(2, 0), TextUtilities.GetWhitespaceBefore(new StringTextSource(" a b"), 2));
	}

	[TestMethod]
	public void TestGetWhitespaceBeforeStartOfString()
	{
		AreEqual(new SimpleSegment(0, 0), TextUtilities.GetWhitespaceBefore(new StringTextSource(" a"), 0));
	}

	[TestMethod]
	public void TestGetWhitespaceBeforeUntilStartOfString()
	{
		AreEqual(new SimpleSegment(0, 2), TextUtilities.GetWhitespaceBefore(new StringTextSource(" \t a"), 2));
	}

	#endregion
}