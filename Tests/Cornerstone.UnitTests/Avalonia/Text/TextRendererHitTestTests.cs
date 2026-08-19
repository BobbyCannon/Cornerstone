#region References

using Avalonia;
using Avalonia.Media;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Text;
using Cornerstone.Parsers.Markdown;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[TestClass]
public class TextRendererHitTestTests : CornerstoneAvaloniaUnitTest
{
	#region Methods

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
				renderer.ViewModel.ViewMetrics.CharacterHeight = System.Math.Max(1, em.Height);
				renderer.ViewModel.ViewMetrics.CharacterWidth = System.Math.Max(1, em.WidthIncludingTrailingWhitespace);
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

	[TestMethod]
	public void TryGetDocumentOffsetAtPointAccountsForBoldRunsBeforeLink()
	{
		RunOnUi(() =>
		{
			var renderer = new TextRenderer { FontSize = 16 };
			using (var em = renderer.GetTextLayout("X", 999999, false, Brushes.Black))
			{
				renderer.ViewModel.ViewMetrics.CharacterHeight = System.Math.Max(1, em.Height);
				renderer.ViewModel.ViewMetrics.CharacterWidth = System.Math.Max(1, em.WidthIncludingTrailingWhitespace);
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
			using var boldLayout = renderer.GetTextLayout(bold, 999999, false, Brushes.Black, bold: true);
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

	#endregion
}
