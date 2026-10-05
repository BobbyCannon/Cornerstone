#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Text.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class MultiCaretRenderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CurrentLineHighlightStaysOnPrimary()
	{
		var model = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		model.Load("abc\r\ndef");
		model.Lines.Measure(new Size(500, 200), false);
		model.Caret.Move(0);
		model.Carets.AddAt(model.Lines[1].StartOffset);
		model.Carets.UpdateVisualLayouts();

		AreEqual(model.Lines[0], model.Caret.Line);
		AreEqual(model.Lines[1], model.Carets.All[1].Line);
	}

	[TestMethod]
	public void MeasureUpdatesVisualLayoutForEveryCaret()
	{
		var model = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		model.Load("abc\r\ndef");
		model.Lines.Measure(new Size(500, 200), false);
		model.Caret.Move(1);
		var extra = model.Carets.AddAt(model.Lines[1].StartOffset);
		model.Carets.UpdateVisualLayouts();

		AreEqual(new Rect(10, 0, 10, 20), model.Caret.VisualLayout);
		AreEqual(new Rect(0, 20, 10, 20), extra.VisualLayout);
	}

	[TestMethod]
	public void MultiCaretUsesRedPrimaryAndBlueExtras()
	{
		var foreground = Brushes.Black;
		var primary = CaretVisual.GetCaretBrush(true, true, false, foreground);
		var extra = CaretVisual.GetCaretBrush(false, true, false, foreground);
		AreEqual(Colors.Red, ((ISolidColorBrush) primary).Color);
		AreEqual(Colors.Blue, ((ISolidColorBrush) extra).Color);
	}

	[TestMethod]
	public void SelectionRendererCollectsRectsForEveryCaret()
	{
		var model = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		model.Load("abc\r\ndef");
		model.Lines.Measure(new Size(500, 200), false);
		model.Caret.Selection.Update(0, 2);
		var extra = model.Carets.AddAt(model.Lines[1].StartOffset);
		extra.Selection.Update(model.Lines[1].StartOffset, model.Lines[1].StartOffset + 2);

		var rects = SelectionRenderer.CollectDocumentRects(model);
		AreEqual(2, rects.Count);
		AreEqual(new Rect(0, 0, 20, 20), rects[0]);
		AreEqual(new Rect(0, 20, 20, 20), rects[1]);
	}

	[TestMethod]
	public void SingleCaretUsesForeground()
	{
		var foreground = Brushes.Black;
		var brush = CaretVisual.GetCaretBrush(true, false, false, foreground);
		AreEqual(Colors.Black, ((ISolidColorBrush) brush).Color);
	}

	#endregion
}