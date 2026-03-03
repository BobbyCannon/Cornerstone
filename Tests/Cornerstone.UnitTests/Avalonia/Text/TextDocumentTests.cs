#region References

using System;
using System.Linq;
using Avalonia;
using Cornerstone.Avalonia.Text;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Avalonia.Text.Rendering;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

public class TextDocumentTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void DeleteBackwards()
	{
		var document = new TextDocument();

		//             012345 6 78901
		document.Load("Hello\r\nWorld");
		AreEqual(2, document.Lines.Count);
		AreEqual(new Line(document.Lines) { StartOffset = 0, EndOffset = 7, LineNumber = 1, LineEndingLength = 2 }, document.Lines[0]);
		AreEqual(new Line(document.Lines) { StartOffset = 7, EndOffset = 12, LineNumber = 2, LineEndingLength = 0 }, document.Lines[1]);

		// should delete the "\r\n" completely
		document.Delete(7, false);

		AreEqual(1, document.Lines.Count);
		AreEqual(new Line(document.Lines) { StartOffset = 0, EndOffset = 10, LineNumber = 1, LineEndingLength = 0 }, document.Lines[0]);
	}

	[Test]
	public void DeleteForward()
	{
		var document = new TextDocument();

		//             012345 6 78901
		document.Load("Hello\r\nWorld");
		AreEqual(2, document.Lines.Count);
		AreEqual(new Line(document.Lines) { StartOffset = 0, EndOffset = 7, LineNumber = 1, LineEndingLength = 2 }, document.Lines[0]);
		AreEqual(new Line(document.Lines) { StartOffset = 7, EndOffset = 12, LineNumber = 2, LineEndingLength = 0 }, document.Lines[1]);

		// should delete the "\r\n" completely
		document.Delete(5, true);

		AreEqual(1, document.Lines.Count);
		AreEqual(new Line(document.Lines) { StartOffset = 0, EndOffset = 10, LineNumber = 1, LineEndingLength = 0 }, document.Lines[0]);
	}

	[Test]
	public void DocumentWidth()
	{
		var document = new TextDocument();

		//                   10        20        30        40    47 (45 characters + \r\n)
		//          01234567890123456789012345678901234567890123456
		document.Load("""
					this is a really long line for the first line
					shorter second line
					third
					""");

		AreEqual(3, document.Lines.Count);
		AreEqual(48, document.DocumentWidth);
		AreEqual("this is a really long line for the first line\r\n", document.Lines[0].ToString());
		AreEqual("shorter second line\r\n", document.Lines[1].ToString());
		AreEqual("third", document.Lines[2].ToString());

		//                   10        20        30        40     48 (46 characters + \r\n)
		//          012345678901234567890123456789012345678901234567
		document.Load("""
					first
					this is a really long line for the second line
					last
					""");

		AreEqual(3, document.Lines.Count);
		AreEqual(49, document.DocumentWidth);
		AreEqual("first\r\n", document.Lines[0].ToString());
		AreEqual("this is a really long line for the second line\r\n", document.Lines[1].ToString());
		AreEqual("last", document.Lines[2].ToString());
	}

	[Test]
	public void EditLineShouldSplit()
	{
		var document = new TextDocument();

		//          012345678 9 012345678 9 0123456789
		document.Load("line one\r\nline two\r\nline three");

		var expected = new LineManager(document)
		{
			new Line(document.Lines) { StartOffset = 0, EndOffset = 10, LineNumber = 1, LineEndingLength = 2 },
			new Line(document.Lines) { StartOffset = 10, EndOffset = 20, LineNumber = 2, LineEndingLength = 2 },
			new Line(document.Lines) { StartOffset = 20, EndOffset = 30, LineNumber = 3, LineEndingLength = 0 }
		};

		AreEqual(expected.ToArray(), document.Lines.ToArray());

		document.Insert(13, Environment.NewLine);

		expected = new LineManager(document)
		{
			new Line(document.Lines) { StartOffset = 0, EndOffset = 10, LineNumber = 1, LineEndingLength = 2 },
			new Line(document.Lines) { StartOffset = 10, EndOffset = 15, LineNumber = 2, LineEndingLength = 2 },
			new Line(document.Lines) { StartOffset = 15, EndOffset = 22, LineNumber = 3, LineEndingLength = 2 },
			new Line(document.Lines) { StartOffset = 22, EndOffset = 32, LineNumber = 4, LineEndingLength = 0 }
		};

		AreEqual(expected.ToArray(), document.Lines.ToArray());
	}

	[Test]
	public void EmptyViewModel()
	{
		var document = new TextDocument();
		AreEqual(1, document.Lines.Count);
		AreEqual(1, document.DocumentWidth);
	}

	[Test]
	public void Load()
	{
		var document = new TextDocument();

		//          012345678 9 012345678 9 0123456789
		document.Load("line one\r\nline two\r\nline three");

		AreEqual(3, document.Lines.Count);
		AreEqual("line one\r\n", document.Lines[0].ToString());
		AreEqual(0, document.Lines[0].StartOffset);
		AreEqual(10, document.Lines[0].EndOffset);
		AreEqual(10, document.Lines[0].Length);

		AreEqual("line two\r\n", document.Lines[1].ToString());
		AreEqual(10, document.Lines[1].StartOffset);
		AreEqual(20, document.Lines[1].EndOffset);
		AreEqual(10, document.Lines[1].Length);

		AreEqual("line three", document.Lines[2].ToString());
		AreEqual(20, document.Lines[2].StartOffset);
		AreEqual(30, document.Lines[2].EndOffset);
		AreEqual(10, document.Lines[2].Length);
	}

	[Test]
	public void SingleLines()
	{
		var document = new TextDocument();
		document.Load("Hello World\r\n");
		AreEqual(2, document.Lines.Count);
		AreEqual(new Line(document.Lines) { StartOffset = 0, EndOffset = 13, LineNumber = 1, LineEndingLength = 2 }, document.Lines[0]);
		AreEqual(new Line(document.Lines) { StartOffset = 13, EndOffset = 13, LineNumber = 2, LineEndingLength = 0 }, document.Lines[1]);

		document.Load("Hello World\r");
		AreEqual(2, document.Lines.Count);
		AreEqual(new Line(document.Lines) { StartOffset = 0, EndOffset = 12, LineNumber = 1, LineEndingLength = 1 }, document.Lines[0]);
		AreEqual(new Line(document.Lines) { StartOffset = 12, EndOffset = 12, LineNumber = 2, LineEndingLength = 0 }, document.Lines[1]);

		document.Load("Hello World\n");
		AreEqual(2, document.Lines.Count);
		AreEqual(new Line(document.Lines) { StartOffset = 0, EndOffset = 12, LineNumber = 1, LineEndingLength = 1 }, document.Lines[0]);
		AreEqual(new Line(document.Lines) { StartOffset = 12, EndOffset = 12, LineNumber = 2, LineEndingLength = 0 }, document.Lines[1]);

		document.Load("Hello World");
		AreEqual(1, document.Lines.Count);
		AreEqual(new Line(document.Lines) { StartOffset = 0, EndOffset = 11, LineNumber = 1, LineEndingLength = 0 }, document.Lines[0]);
	}

	[Test]
	public void WordWrapLines()
	{
		var textMetrics = new TextMetrics { CharacterHeight = 20, CharacterWidth = 10 };
		var document = new TextDocument();

		//             012345678901 2 34567890 1
		document.Load("Hello World\r\nFoo Bar\r\n");
		//             0       110    0    70
		document.Lines.Measure(new Size(120, 200), true, textMetrics);

		AreEqual(new Rect(0, 0, 110, 20), document.Lines[0].VisualLayout);
		AreEqual(new Rect(0, 20, 70, 20), document.Lines[1].VisualLayout);
		AreEqual(new Rect(0, 40, 0, 20), document.Lines[2].VisualLayout);
		
		document.Lines.Measure(new Size(200, 200), false, textMetrics);

		AreEqual(new Rect(0, 0, 110, 20), document.Lines[0].VisualLayout);
		AreEqual(new Rect(0, 20, 70, 20), document.Lines[1].VisualLayout);
		AreEqual(new Rect(0, 40, 0, 20), document.Lines[2].VisualLayout);
	}

	#endregion
}