#region References

using Cornerstone.VisualStudio.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class LineJoinDeleteTests
{
	#region Methods

	[TestMethod]
	public void DeleteAtAttributeJoinsWithOneSpace()
	{
		var indent = new string(' ', 20);
		var next = indent + "Text=\"\" />";
		var joined = Join("<TextBlock Classes=\"InputValue\"", "\r\n", next);
		Assert.AreEqual("<TextBlock Classes=\"InputValue\" Text=\"\" />", joined);
	}

	[TestMethod]
	public void TabIndentJoinsWithOneSpace()
	{
		var joined = Join("<TextBlock Classes=\"InputValue\"", "\n", "\t\tText=\"\" />");
		Assert.AreEqual("<TextBlock Classes=\"InputValue\" Text=\"\" />", joined);
	}

	[TestMethod]
	public void ChildElementJoinsWithOneSpace()
	{
		var joined = Join("<StackPanel>", "\n", "    <TextBlock />");
		Assert.AreEqual("<StackPanel> <TextBlock />", joined);
	}

	[TestMethod]
	public void ClosingTagJoinsWithOneSpace()
	{
		var joined = Join("</Grid", "\n", "    </StackPanel>");
		Assert.AreEqual("</Grid </StackPanel>", joined);
	}

	[TestMethod]
	public void PlainTextJoinsWithOneSpace()
	{
		var joined = Join("Hello", "\r\n", "        world");
		Assert.AreEqual("Hello world", joined);
	}

	[TestMethod]
	public void NextLineWithoutIndentStaysWithTheEditor()
	{
		Assert.IsFalse(LineJoinDelete.TryGetReplaceLength(2, "Text=\"\" />", out _));
	}

	[TestMethod]
	public void MissingLineBreakDoesNotJoin()
	{
		Assert.IsFalse(LineJoinDelete.TryGetReplaceLength(0, "    Text=\"\"", out _));
	}

	private static string Join(string textBeforeCaret, string lineBreak, string nextLine)
	{
		var found = LineJoinDelete.TryGetReplaceLength(lineBreak.Length, nextLine, out var length);
		Assert.IsTrue(found);
		var indent = length - lineBreak.Length;
		Assert.IsTrue(indent > 0);
		return textBeforeCaret + LineJoinDelete.Replacement + nextLine.Substring(indent);
	}

	#endregion
}
