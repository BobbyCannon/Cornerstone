#region References

using Cornerstone.Avalonia.AvaloniaEdit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Utils;

[TestClass]
public class IndentationStringTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void IndentWith4Spaces()
	{
		var options = new TextEditorSettings { IndentationSize = 4, ConvertTabsToSpaces = true };
		AreEqual("    ", options.IndentationString);
		AreEqual("   ", options.GetIndentationString(2));
		AreEqual("  ", options.GetIndentationString(3));
		AreEqual(" ", options.GetIndentationString(4));
		AreEqual("    ", options.GetIndentationString(5));
		AreEqual("   ", options.GetIndentationString(6));
	}

	[TestMethod]
	public void IndentWithSingleTab()
	{
		var options = new TextEditorSettings { IndentationSize = 4, ConvertTabsToSpaces = false };
		AreEqual("\t", options.IndentationString);
		AreEqual("\t", options.GetIndentationString(2));
		AreEqual("\t", options.GetIndentationString(3));
		AreEqual("\t", options.GetIndentationString(4));
		AreEqual("\t", options.GetIndentationString(5));
		AreEqual("\t", options.GetIndentationString(6));
	}

	#endregion
}