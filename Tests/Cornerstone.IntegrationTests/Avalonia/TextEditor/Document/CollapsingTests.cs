#region References

using System;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework.Legacy;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Document;

[TestClass]
public class CollapsingTests
{
	#region Fields

	private TextEditorDocument _document;
	private HeightTree _heightTree;

	#endregion

	#region Methods

	[TestMethod]
	public void FullCheck()
	{
		for (var from = 1; from <= 10; from++)
		{
			for (var to = from; to <= 10; to++)
			{
				try
				{
					SimpleCheck(from, to).Uncollapse();
					for (var i = 1; i <= 10; i++)
					{
						Assert.IsFalse(_heightTree.GetIsCollapsed(i));
					}
					CheckHeights();
				}
				catch
				{
					Console.WriteLine("from = " + from + ", to = " + to);
					throw;
				}
			}
		}
	}

	[TestMethod]
	public void InsertInCollapsedSection()
	{
		var sec1 = _heightTree.CollapseText(_document.GetLineByNumber(4), _document.GetLineByNumber(6));
		_document.Insert(_document.GetLineByNumber(5).StartIndex, "a\nb\nc");
		for (var i = 1; i < 4; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		for (var i = 4; i <= 8; i++)
		{
			Assert.IsTrue(_heightTree.GetIsCollapsed(i));
		}
		for (var i = 9; i <= 12; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
	}

	[TestMethod]
	public void RemoveCollapsedSection()
	{
		var sec1 = _heightTree.CollapseText(_document.GetLineByNumber(3), _document.GetLineByNumber(3));
		var line3Offset = _document.GetLineByNumber(3).StartIndex;
		_document.Remove(line3Offset - 1, 1);
		for (var i = 1; i <= 9; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
		ClassicAssert.AreSame(null, sec1.Start);
		ClassicAssert.AreSame(null, sec1.End);
		// section gets uncollapsed when it is removed
		Assert.IsFalse(sec1.IsCollapsed);
	}

	[TestMethod]
	public void RemoveEndOfCollapsedSection()
	{
		var sec1 = _heightTree.CollapseText(_document.GetLineByNumber(3), _document.GetLineByNumber(6));
		var line5Offset = _document.GetLineByNumber(5).StartIndex;
		var line8Offset = _document.GetLineByNumber(8).StartIndex;
		_document.Remove(line5Offset, line8Offset - line5Offset);
		for (var i = 1; i < 3; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		for (var i = 3; i <= 5; i++)
		{
			Assert.IsTrue(_heightTree.GetIsCollapsed(i));
		}
		for (var i = 6; i <= 7; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
	}

	[TestMethod]
	public void RemoveInCollapsedSection()
	{
		var sec1 = _heightTree.CollapseText(_document.GetLineByNumber(3), _document.GetLineByNumber(7));
		var line4Offset = _document.GetLineByNumber(4).StartIndex;
		var line6Offset = _document.GetLineByNumber(6).StartIndex;
		_document.Remove(line4Offset, line6Offset - line4Offset);
		for (var i = 1; i < 3; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		for (var i = 3; i <= 5; i++)
		{
			Assert.IsTrue(_heightTree.GetIsCollapsed(i));
		}
		for (var i = 6; i <= 8; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
	}

	[TestInitialize]
	public void Setup()
	{
		_document = new TextEditorDocument();
		_document.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
		_heightTree = new HeightTree(_document, 10);
		foreach (var line in _document.Lines)
		{
			_heightTree.SetHeight(line, line.LineNumber);
		}
	}

	[TestMethod]
	public void SimpleCheck()
	{
		SimpleCheck(4, 6);
	}

	[TestMethod]
	public void SimpleUncollapse()
	{
		var sec1 = _heightTree.CollapseText(_document.GetLineByNumber(4), _document.GetLineByNumber(6));
		sec1.Uncollapse();
		for (var i = 1; i <= 10; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
	}

	private void CheckHeights()
	{
		HeightTests.CheckHeights(_document, _heightTree);
	}

	private CollapsedLineSection SimpleCheck(int from, int to)
	{
		var sec1 = _heightTree.CollapseText(_document.GetLineByNumber(from), _document.GetLineByNumber(to));
		for (var i = 1; i < from; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		for (var i = from; i <= to; i++)
		{
			Assert.IsTrue(_heightTree.GetIsCollapsed(i));
		}
		for (var i = to + 1; i <= 10; i++)
		{
			Assert.IsFalse(_heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
		return sec1;
	}

	#endregion
}