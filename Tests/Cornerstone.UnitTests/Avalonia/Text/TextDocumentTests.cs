#region References

using System;
using System.Linq;
using Avalonia;
using Cornerstone.Avalonia.Text;
using Cornerstone.Avalonia.Text.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[TestClass]
public class TextDocumentTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DeleteBackwards()
	{
		var viewModel = new TextEditorViewModel();

		//             012345 6 78901
		viewModel.Load("Hello\r\nWorld");
		AreEqual(2, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 7, LineNumber = 1, LineEndingLength = 2 }, viewModel.Lines[0]);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 7, EndOffset = 12, LineNumber = 2, LineEndingLength = 0 }, viewModel.Lines[1]);

		// should delete the "\r\n" completely
		viewModel.Delete(7, false);

		AreEqual(1, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 10, LineNumber = 1, LineEndingLength = 0 }, viewModel.Lines[0]);
	}

	[TestMethod]
	public void DeleteForward()
	{
		var viewModel = new TextEditorViewModel();

		//             012345 6 78901
		viewModel.Load("Hello\r\nWorld");
		AreEqual(2, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 7, LineNumber = 1, LineEndingLength = 2 }, viewModel.Lines[0]);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 7, EndOffset = 12, LineNumber = 2, LineEndingLength = 0 }, viewModel.Lines[1]);

		// should delete the "\r\n" completely
		viewModel.Delete(5, true);

		AreEqual(1, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 10, LineNumber = 1, LineEndingLength = 0 }, viewModel.Lines[0]);
	}

	[TestMethod]
	public void EditLineShouldSplit()
	{
		var viewModel = new TextEditorViewModel();

		//          012345678 9 012345678 9 0123456789
		viewModel.Load("line one\r\nline two\r\nline three");
		AreEqual(3, viewModel.Lines.Count);

		var expected = new LineManager(viewModel)
		{
			new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 10, LineNumber = 1, LineEndingLength = 2 },
			new Line(viewModel.Lines) { StartOffset = 10, EndOffset = 20, LineNumber = 2, LineEndingLength = 2 },
			new Line(viewModel.Lines) { StartOffset = 20, EndOffset = 30, LineNumber = 3, LineEndingLength = 0 }
		};

		AreEqual(expected.ToArray(), viewModel.Lines.ToArray());

		viewModel.Caret.Move(13);
		viewModel.Insert(Environment.NewLine);

		expected = new LineManager(viewModel)
		{
			new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 10, LineNumber = 1, LineEndingLength = 2 },
			new Line(viewModel.Lines) { StartOffset = 10, EndOffset = 15, LineNumber = 2, LineEndingLength = 2 },
			new Line(viewModel.Lines) { StartOffset = 15, EndOffset = 22, LineNumber = 3, LineEndingLength = 2 },
			new Line(viewModel.Lines) { StartOffset = 22, EndOffset = 32, LineNumber = 4, LineEndingLength = 0 }
		};

		AreEqual(expected.ToArray(), viewModel.Lines.ToArray());
	}

	[TestMethod]
	public void EmptyViewModel()
	{
		var viewModel = new TextEditorViewModel();
		AreEqual(1, viewModel.Lines.Count);
		AreEqual(0, viewModel.DocumentLength);
	}

	[TestMethod]
	public void EnterOnEmptyDocument()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		var caret = new Caret(viewModel);
		AreEqual(1, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 0, LineNumber = 1, LineEndingLength = 0 }, viewModel.Lines[0]);

		viewModel.Insert("\r\n");
		viewModel.Lines.Measure(new Size(800, 400), true);
		caret.Move(caret.Offset + 2);

		AreEqual(2, viewModel.Lines.Count);
		AreEqual(new Rect(0, 0, 0, 20), viewModel.Lines[0].VisualLayout);
		AreEqual(new Rect(0, 20, 0, 20), viewModel.Lines[1].VisualLayout);
	}

	[TestMethod]
	public void Load()
	{
		var viewModel = new TextEditorViewModel();

		//          012345678 9 012345678 9 0123456789
		viewModel.Load("line one\r\nline two\r\nline three");

		AreEqual(3, viewModel.Lines.Count);
		AreEqual("line one\r\n", viewModel.Lines[0].ToString());
		AreEqual(0, viewModel.Lines[0].StartOffset);
		AreEqual(10, viewModel.Lines[0].EndOffset);
		AreEqual(10, viewModel.Lines[0].Length);

		AreEqual("line two\r\n", viewModel.Lines[1].ToString());
		AreEqual(10, viewModel.Lines[1].StartOffset);
		AreEqual(20, viewModel.Lines[1].EndOffset);
		AreEqual(10, viewModel.Lines[1].Length);

		AreEqual("line three", viewModel.Lines[2].ToString());
		AreEqual(20, viewModel.Lines[2].StartOffset);
		AreEqual(30, viewModel.Lines[2].EndOffset);
		AreEqual(10, viewModel.Lines[2].Length);
	}

	[TestMethod]
	public void SingleLines()
	{
		var viewModel = new TextEditorViewModel();
		viewModel.Load("Hello World\r\n");
		AreEqual(2, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 13, LineNumber = 1, LineEndingLength = 2 }, viewModel.Lines[0]);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 13, EndOffset = 13, LineNumber = 2, LineEndingLength = 0 }, viewModel.Lines[1]);

		viewModel.Load("Hello World\r");
		AreEqual(2, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 12, LineNumber = 1, LineEndingLength = 1 }, viewModel.Lines[0]);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 12, EndOffset = 12, LineNumber = 2, LineEndingLength = 0 }, viewModel.Lines[1]);

		viewModel.Load("Hello World\n");
		AreEqual(2, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 12, LineNumber = 1, LineEndingLength = 1 }, viewModel.Lines[0]);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 12, EndOffset = 12, LineNumber = 2, LineEndingLength = 0 }, viewModel.Lines[1]);

		viewModel.Load("Hello World");
		AreEqual(1, viewModel.Lines.Count);
		AreEqual(new Line(viewModel.Lines) { StartOffset = 0, EndOffset = 11, LineNumber = 1, LineEndingLength = 0 }, viewModel.Lines[0]);
	}

	[TestMethod]
	public void WordWrapLines()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };

		//             012345678901 2 34567890 1
		viewModel.Load("Hello World\r\nFoo Bar\r\n");

		//             0       110    0    70
		viewModel.Lines.Measure(new Size(120, 200), true);

		AreEqual(new Rect(0, 0, 110, 20), viewModel.Lines[0].VisualLayout);
		AreEqual(new Rect(0, 20, 70, 20), viewModel.Lines[1].VisualLayout);
		AreEqual(new Rect(0, 40, 0, 20), viewModel.Lines[2].VisualLayout);

		viewModel.Lines.Measure(new Size(200, 200), false);

		AreEqual(new Rect(0, 0, 110, 20), viewModel.Lines[0].VisualLayout);
		AreEqual(new Rect(0, 20, 70, 20), viewModel.Lines[1].VisualLayout);
		AreEqual(new Rect(0, 40, 0, 20), viewModel.Lines[2].VisualLayout);
	}

	#endregion
}