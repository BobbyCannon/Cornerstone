#region References

using System.Collections.Generic;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Generators;
using Cornerstone.Text.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Document;

/// <summary>
/// A randomized test for the line manager.
/// </summary>
[TestClass]
public class RandomizedLineManagerTest : CornerstoneUnitTest
{
	#region Fields

	private TextDocument _document;

	#endregion

	static RandomizedLineManagerTest()
	{
		TestLoopCount = 500;
	}

	public static int TestLoopCount { get; }

	#region Methods

	[TestMethod]
	public void CollapsingTest()
	{
		char[] chars = ['a', 'b', '\r', '\n'];
		var buffer = new char[20];
		var heightTree = new HeightTree(_document, 10);
		List<CollapsedLineSection> collapsedSections = [];
		for (var i = 0; i < TestLoopCount; i++)
		{
			switch (RandomGenerator.NextInteger(0, 10))
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
					var offset = RandomGenerator.NextInteger(0, _document.TextLength);
					var length = RandomGenerator.NextInteger(0, _document.TextLength - offset);
					var newTextLength = RandomGenerator.NextInteger(0, 20);
					for (var j = 0; j < newTextLength; j++)
					{
						buffer[j] = chars[RandomGenerator.NextInteger(0, chars.Length)];
					}

					_document.Replace(offset, length, new string(buffer, 0, newTextLength));
					break;
				case 6:
				case 7:
					var startLine = RandomGenerator.NextInteger(1, _document.LineCount + 1);
					var endLine = RandomGenerator.NextInteger(startLine, _document.LineCount + 1);
					collapsedSections.Add(heightTree.CollapseText(_document.GetLineByNumber(startLine), _document.GetLineByNumber(endLine)));
					break;
				case 8:
					if (collapsedSections.Count > 0)
					{
						var cs = collapsedSections[RandomGenerator.NextInteger(0, collapsedSections.Count)];
						// unless the text section containing the CollapsedSection was deleted:
						if (cs.Start != null)
						{
							cs.Uncollapse();
						}
						collapsedSections.Remove(cs);
					}
					break;
				case 9:
					foreach (var ls in _document.Lines)
					{
						heightTree.SetHeight(ls, ls.LineNumber);
					}
					break;
			}
			var treeSections = new HashSet<CollapsedLineSection>(heightTree.GetAllCollapsedSections());
			var expectedCount = 0;
			foreach (var cs in collapsedSections)
			{
				if (cs.Start != null)
				{
					expectedCount++;
					Assert.IsTrue(treeSections.Contains(cs));
				}
			}
			AreEqual(expectedCount, treeSections.Count);
			CheckLines();
			HeightTests.CheckHeights(_document, heightTree);
		}
	}

	[TestMethod]
	public void LargeReplacements()
	{
		char[] chars = ['a', 'b', 'c', 'd', 'e', 'f', 'g', '\r', '\n'];
		var buffer = new char[1000];
		for (var i = 0; i < 20; i++)
		{
			var offset = RandomGenerator.NextInteger(0, _document.TextLength);
			var length = RandomGenerator.NextInteger(0, (_document.TextLength - offset) / 4);
			var newTextLength = RandomGenerator.NextInteger(0, 1000);
			for (var j = 0; j < newTextLength; j++)
			{
				buffer[j] = chars[RandomGenerator.NextInteger(0, chars.Length)];
			}

			var newText = new string(buffer, 0, newTextLength);
			var expectedText = _document.Text.Remove(offset, length).Insert(offset, newText);
			_document.Replace(offset, length, newText);
			AreEqual(expectedText, _document.Text);
			CheckLines();
		}
	}

	[TestInitialize]
	public void Setup()
	{
		_document = new TextDocument();
	}

	[TestMethod]
	public void ShortReplacements()
	{
		char[] chars = ['a', 'b', '\r', '\n'];
		var buffer = new char[20];
		for (var i = 0; i < TestLoopCount; i++)
		{
			var offset = RandomGenerator.NextInteger(0, _document.TextLength);
			var length = RandomGenerator.NextInteger(0, _document.TextLength - offset);
			var newTextLength = RandomGenerator.NextInteger(0, 20);
			for (var j = 0; j < newTextLength; j++)
			{
				buffer[j] = chars[RandomGenerator.NextInteger(0, chars.Length)];
			}

			_document.Replace(offset, length, new string(buffer, 0, newTextLength));
			CheckLines();
		}
	}

	private void CheckLines()
	{
		var text = _document.Text;
		var lineNumber = 1;
		var lineStart = 0;
		for (var i = 0; i < text.Length; i++)
		{
			var c = text[i];
			if ((c == '\r') && ((i + 1) < text.Length) && (text[i + 1] == '\n'))
			{
				var line = _document.GetLineByNumber(lineNumber);
				AreEqual(lineNumber, line.LineNumber);
				AreEqual(2, line.DelimiterLength);
				AreEqual(lineStart, line.Offset);
				AreEqual(i - lineStart, line.Length);
				i++; // consume \n
				lineNumber++;
				lineStart = i + 1;
			}
			else if ((c == '\r') || (c == '\n'))
			{
				var line = _document.GetLineByNumber(lineNumber);
				AreEqual(lineNumber, line.LineNumber);
				AreEqual(1, line.DelimiterLength);
				AreEqual(lineStart, line.Offset);
				AreEqual(i - lineStart, line.Length);
				lineNumber++;
				lineStart = i + 1;
			}
		}
		AreEqual(lineNumber, _document.LineCount);
	}

	#endregion
}