#region References

using System;
using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Text.Parsing.Markdown;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class TextRendererHitTestTests : CornerstoneCornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CaretHitTestAtEndOfBoldRunIncludesLastCharacterForCopy()
	{
		RunOnUi(() =>
		{
			var renderer = new TextRenderer { FontSize = 16 };
			using (var em = renderer.GetTextLayout("X", 999999, false, Brushes.Black))
			{
				renderer.ViewModel.ViewMetrics.CharacterHeight = Math.Max(1, em.Height);
				renderer.ViewModel.ViewMetrics.CharacterWidth = Math.Max(1, em.WidthIncludingTrailingWhitespace);
			}

			const string word = "bold";
			renderer.ViewModel.Load(word);
			var tokenizer = new MarkdownViewTokenizer();
			renderer.ViewModel.TokenManager.Initialize(tokenizer);
			renderer.ViewModel.TokenManager.Add(
				tokenizer.CreateOrUpdateSection(MarkdownTokenizer.TokenTypeBold, 0, word.Length, bold: true));
			renderer.ViewModel.Lines.Measure(new Size(400, 80), false);

			using var boldLayout = renderer.GetTextLayout(word, 999999, false, Brushes.Black, true);

			// Just inside the painted right edge of the bold word.
			var sampleX = boldLayout.WidthIncludingTrailingWhitespace - 0.5;
			IsTrue(renderer.TryGetDocumentOffsetAtPoint(new Point(sampleX, 2), true, out var caretOffset));
			IsTrue(caretOffset >= word.Length);

			renderer.ViewModel.Caret.Selection.Reset(0);
			renderer.ViewModel.Caret.Selection.Update(0, caretOffset);
			AreEqual(word, renderer.ViewModel.Clipboard.GetCopyText());
		});
	}

	[TestMethod]
	public void PaintedSelectionRectMatchesBoldRunWidth()
	{
		RunOnUi(() =>
		{
			var renderer = new TextRenderer { FontSize = 16 };
			using (var em = renderer.GetTextLayout("X", 999999, false, Brushes.Black))
			{
				renderer.ViewModel.ViewMetrics.CharacterHeight = Math.Max(1, em.Height);
				renderer.ViewModel.ViewMetrics.CharacterWidth = Math.Max(1, em.WidthIncludingTrailingWhitespace);
			}

			const string prefix = "ab";
			const string word = "bold";
			var full = prefix + word;
			renderer.ViewModel.Load(full);
			var tokenizer = new MarkdownViewTokenizer();
			renderer.ViewModel.TokenManager.Initialize(tokenizer);
			renderer.ViewModel.TokenManager.Add(
				tokenizer.CreateOrUpdateSection(
					MarkdownTokenizer.TokenTypeBold, prefix.Length, full.Length, bold: true));
			renderer.ViewModel.Lines.Measure(new Size(800, 80), false);

			using var prefixLayout = renderer.GetTextLayout(prefix, 999999, false, Brushes.Black);
			using var boldLayout = renderer.GetTextLayout(word, 999999, false, Brushes.Black, true);
			var line = renderer.ViewModel.Lines[0];
			var rects = renderer.GetPaintedSelectionRects(line, prefix.Length, full.Length).ToList();
			AreEqual(1, rects.Count);
			IsTrue(Math.Abs(prefixLayout.WidthIncludingTrailingWhitespace - rects[0].X) < 0.5);
			IsTrue(Math.Abs(boldLayout.WidthIncludingTrailingWhitespace - rects[0].Width) < 0.5);
		});
	}

	[TestMethod]
	public void PaintedWidthReusesTextLayoutsOnSecondMeasure()
	{
		RunOnUi(() =>
		{
			var renderer = new TextRenderer { FontSize = 16 };
			renderer.ViewModel.Load("hello world");
			renderer.ViewModel.Lines.Measure(new Size(800, 80), false);

			var length = renderer.ViewModel.DocumentLength;
			renderer.GetPaintedWidth(0, length);
			var created = renderer.TextLayoutCreateCount;

			renderer.GetPaintedWidth(0, length);
			AreEqual(created, renderer.TextLayoutCreateCount);
		});
	}

	[TestMethod]
	public void TryGetDocumentOffsetAtPointAccountsForBoldRunsBeforeLink()
	{
		RunOnUi(() =>
		{
			var renderer = new TextRenderer { FontSize = 16 };
			using (var em = renderer.GetTextLayout("X", 999999, false, Brushes.Black))
			{
				renderer.ViewModel.ViewMetrics.CharacterHeight = Math.Max(1, em.Height);
				renderer.ViewModel.ViewMetrics.CharacterWidth = Math.Max(1, em.WidthIncludingTrailingWhitespace);
			}

			// "does not move ... See Keystone.md" with bold on "not"
			const string before = "does ";
			const string bold = "not";
			const string mid = " move. See ";
			const string linkText = "Keystone.md";
			var full = before + bold + mid + linkText;
			var boldStart = before.Length;
			var boldEnd = boldStart + bold.Length;
			var linkStart = before.Length + bold.Length + mid.Length;
			var linkEnd = full.Length;

			renderer.ViewModel.Load(full);
			var tokenizer = new MarkdownViewTokenizer();
			renderer.ViewModel.TokenManager.Initialize(tokenizer);
			renderer.ViewModel.TokenManager.Add(
				tokenizer.CreateOrUpdateSection(MarkdownTokenizer.TokenTypeBold, boldStart, boldEnd, bold: true));
			renderer.ViewModel.TokenManager.Add(
				tokenizer.CreateOrUpdateSection(MarkdownTokenizer.TokenTypeLink, linkStart, linkEnd));
			renderer.ViewModel.Lines.Measure(new Size(1200, 400), false);

			using var beforeLayout = renderer.GetTextLayout(before, 999999, false, Brushes.Black);
			using var boldLayout = renderer.GetTextLayout(bold, 999999, false, Brushes.Black, true);
			using var midLayout = renderer.GetTextLayout(mid, 999999, false, Brushes.Black);
			using var firstLinkChar = renderer.GetTextLayout("K", 999999, false, Brushes.Black);
			var sampleX = beforeLayout.WidthIncludingTrailingWhitespace
				+ boldLayout.WidthIncludingTrailingWhitespace
				+ midLayout.WidthIncludingTrailingWhitespace
				+ (firstLinkChar.WidthIncludingTrailingWhitespace / 2.0);

			IsTrue(renderer.TryGetDocumentOffsetAtPoint(new Point(sampleX, 2), out var offset));
			IsTrue(offset >= linkStart);
			IsTrue(offset < linkEnd);
		});
	}

	[TestMethod]
	public void TryGetDocumentOffsetAtPointMatchesPaintWidthsForNarrowPrefix()
	{
		RunOnUi(() =>
		{
			// Repro shape for Documentation reader: proportional glyphs (narrow "i") paint the
			// link left of where monospace CharacterWidth hit-testing expects it.
			var renderer = new TextRenderer { FontSize = 16 };
			using (var em = renderer.GetTextLayout("X", 999999, false, Brushes.Black))
			{
				renderer.ViewModel.ViewMetrics.CharacterHeight = Math.Max(1, em.Height);
				renderer.ViewModel.ViewMetrics.CharacterWidth = Math.Max(1, em.WidthIncludingTrailingWhitespace);
			}

			var prefix = new string('i', 40);
			const string linkText = "Keystone.md";
			var full = prefix + linkText;
			var linkStart = prefix.Length;
			var linkEnd = full.Length;

			renderer.ViewModel.Load(full);
			renderer.ViewModel.TokenManager.Initialize(new MarkdownViewTokenizer());
			renderer.ViewModel.TokenManager.Add(
				new MarkdownViewTokenizer().CreateOrUpdateSection(
					MarkdownTokenizer.TokenTypeLink, linkStart, linkEnd));
			renderer.ViewModel.Lines.Measure(new Size(1200, 400), false);

			using var prefixLayout = renderer.GetTextLayout(prefix, 999999, false, Brushes.Black);
			using var firstLinkChar = renderer.GetTextLayout("K", 999999, false, Brushes.Black);
			var sampleX = prefixLayout.WidthIncludingTrailingWhitespace
				+ (firstLinkChar.WidthIncludingTrailingWhitespace / 2.0);

			IsTrue(renderer.TryGetDocumentOffsetAtPoint(new Point(sampleX, 2), out var paintMatchedOffset));
			IsTrue(paintMatchedOffset >= linkStart);
			IsTrue(paintMatchedOffset < linkEnd);

			// Same X with monospace GetAdvance still lands in the narrow prefix.
			var monoOffset = renderer.ViewModel.Lines[0].GetNearestOffsetAtVisual(sampleX, 2, false);
			IsTrue(monoOffset < linkStart);
		});
	}

	#endregion
}
