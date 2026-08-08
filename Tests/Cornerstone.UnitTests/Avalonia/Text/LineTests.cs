#region References

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia;
using Cornerstone.Avalonia.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[TestClass]
public class LineTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void GetNearestOffsetAtVisual()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };

		//             012345678901
		viewModel.Load("Hello World");
		viewModel.Lines.Measure(new Size(50, 200), true);

		// Virtual Wrapped Line
		// 01234| Index | Size   50w 60h
		// Hello|  0-4  | 0,  0, 50, 20
		//  Worl|  5-9  | 0, 20, 50, 20
		// d    | 10-11 | 0, 40, 50, 20
		AreEqual(1, viewModel.Lines.Count);
		AreEqual(new Rect(0, 0, 50, 60), viewModel.Lines[0].VisualLayout);

		AreEqual(5, viewModel.Lines[0].GetNearestOffsetAtVisual(45, 0, false));
		AreEqual(5, viewModel.Lines[0].GetNearestOffsetAtVisual(45, 19, false));
		AreEqual(10, viewModel.Lines[0].GetNearestOffsetAtVisual(45, 20, false));
		AreEqual(11, viewModel.Lines[0].GetNearestOffsetAtVisual(45, 40, false));
		
		AreEqual(0, viewModel.Lines[0].GetNearestOffsetAtVisual(0, 0, false));
		AreEqual(6, viewModel.Lines[0].GetNearestOffsetAtVisual(10, 20, false));
	}
	
	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void GetNearestOffsetAtVisualWithoutWrap()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };

		//             01234567890
		viewModel.Load("Hello World");
		viewModel.Lines.Measure(new Size(50, 200), false);

		// Virtual Wrapped Line
		// 01234567890
		// Hello World
		AreEqual(1, viewModel.Lines.Count);
		AreEqual(new Rect(0, 0, 110, 20), viewModel.Lines[0].VisualLayout);

		AreEqual(0, viewModel.Lines[0].GetNearestOffsetAtVisual(0, 0, false));
		AreEqual(5, viewModel.Lines[0].GetNearestOffsetAtVisual(50, 20, false));

		// Should still work even if the Y is way out of index
		AreEqual(5, viewModel.Lines[0].GetNearestOffsetAtVisual(50, 40, false));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void GetSelectionRectsFollowsSoftWrap()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };

		//             01234567890
		viewModel.Load("Hello World");
		viewModel.Lines.Measure(new Size(50, 200), true);

		var line = viewModel.Lines[0];
		AreEqual(3, line.VisualSubLineCount);

		// Select "Hello World" entirely — one rect per visual subline, not one long horizontal bar.
		var rects = line.GetSelectionRects(0, 11).ToList();
		AreEqual(3, rects.Count);

		// Each rect is a single visual row high and stacked by CharacterHeight.
		AreEqual(0, rects[0].Y);
		AreEqual(20, rects[1].Y);
		AreEqual(40, rects[2].Y);
		foreach (var rect in rects)
		{
			AreEqual(20, rect.Height);
			IsTrue(rect.Width > 0);
			// Soft-wrap rows stay within the measured wrap width (5 cells * 10px).
			IsTrue(rect.Width <= 50.0001);
		}

		// Selection only on the second visual subline should not paint the first row.
		line.GetVisualSubLineRange(1, out var sub1Start, out var sub1End);
		var mid = line.GetSelectionRects(sub1Start, sub1End).ToList();
		AreEqual(1, mid.Count);
		AreEqual(20, mid[0].Y);
	}

	[TestMethod]
	public void GetSelectionRectsWithoutWrapIsSingleRow()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };

		viewModel.Load("Hello World");
		viewModel.Lines.Measure(new Size(500, 200), false);

		var line = viewModel.Lines[0];
		AreEqual(1, line.VisualSubLineCount);

		var rects = line.GetSelectionRects(0, 5).ToList();
		AreEqual(1, rects.Count);
		AreEqual(new Rect(0, 0, 50, 20), rects[0]);
	}

	[TestMethod]
	public void GetSelectionRectsSpansMultipleLogicalLines()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };

		viewModel.Load("abc\r\ndef");
		viewModel.Lines.Measure(new Size(500, 200), false);

		AreEqual(2, viewModel.Lines.Count);

		// "abc\r\ndef" → offsets 0a 1b 2c 3\r 4\n 5d 6e 7f
		// Select from 'b' through 'e' (exclusive end after 'e').
		var first = viewModel.Lines[0].GetSelectionRects(1, 7).ToList();
		var second = viewModel.Lines[1].GetSelectionRects(1, 7).ToList();

		AreEqual(1, first.Count);
		// "bc" at offsets 1..3 (newline has zero advance)
		AreEqual(new Rect(10, 0, 20, 20), first[0]);

		AreEqual(1, second.Count);
		// "de" on second line (offsets 5..7); Y follows measured layout
		AreEqual(viewModel.Lines[1].VisualLayout.Y, second[0].Y);
		AreEqual(0, second[0].X);
		AreEqual(20, second[0].Width);
	}

	#endregion
}