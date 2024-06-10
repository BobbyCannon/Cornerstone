#region References

using System.Linq;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Text.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Document;

[TestClass]
public class HeightTests : CornerstoneUnitTest
{
	#region Fields

	private TextDocument document;
	private HeightTree heightTree;

	#endregion

	#region Methods

	[TestInitialize]
	public void Setup()
	{
		document = new TextDocument();
		document.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
		heightTree = new HeightTree(document, 10);
		foreach (var line in document.Lines)
		{
			heightTree.SetHeight(line, line.LineNumber);
		}
	}

	[TestMethod]
	public void SimpleCheck()
	{
		CheckHeights();
	}

	[TestMethod]
	public void TestHeightChanged()
	{
		heightTree.SetHeight(document.GetLineByNumber(4), 100);
		CheckHeights();
	}

	[TestMethod]
	public void TestLinesInserted()
	{
		document.Insert(0, "x\ny\n");
		heightTree.SetHeight(document.Lines[0], 100);
		heightTree.SetHeight(document.Lines[1], 1000);
		heightTree.SetHeight(document.Lines[2], 10000);
		CheckHeights();
	}

	[TestMethod]
	public void TestLinesRemoved()
	{
		document.Remove(5, 4);
		CheckHeights();
	}

	internal static void CheckHeights(TextDocument document, HeightTree heightTree)
	{
		var heights = document.Lines.Select(l => heightTree.GetIsCollapsed(l.LineNumber) ? 0 : heightTree.GetHeight(l)).ToArray();
		var visualPositions = new double[document.LineCount + 1];
		for (var i = 0; i < heights.Length; i++)
		{
			visualPositions[i + 1] = visualPositions[i] + heights[i];
		}
		foreach (var ls in document.Lines)
		{
			AreEqual(visualPositions[ls.LineNumber - 1], heightTree.GetVisualPosition(ls));
		}
		AreEqual(visualPositions[document.LineCount], heightTree.TotalHeight);
	}

	private void CheckHeights()
	{
		CheckHeights(document, heightTree);
	}

	#endregion
}