#region References

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Rendering;

public class TextViewTests : CornerstoneUnitTest
{
	#region Constants

	// https://github.com/AvaloniaUI/Avalonia/blob/master/src/Headless/Avalonia.Headless/HeadlessPlatformStubs.cs#L126
	private const int _headlessGlyphAdvance = 8;

	#endregion

	#region Methods

	[AvaloniaTest]
	public void VisualLineShouldCreateOneTextLinesWhenNotWrapping()
	{
		var textView = new TextView();
		var document = new TextDocument("hello world".ToCharArray());

		textView.Document = document;
		textView.EnsureVisualLines();
		((ILogicalScrollable) textView).CanHorizontallyScroll = false;
		textView.Width = _headlessGlyphAdvance * 500;

		textView.Measure(Size.Infinity);

		var visualLine = textView.GetOrConstructVisualLine(document.Lines[0]);

		AreEqual(1, visualLine.TextLines.Count);
		AreEqual("hello world", new string(visualLine.TextLines[0].TextRuns[0].Text.Span));
	}

	[AvaloniaTest]
	public void VisualLineShouldCreateTwoTextLinesWhenWrapping()
	{
		var textView = new TextView();
		var document = new TextDocument("hello world".ToCharArray());

		textView.Document = document;

		((ILogicalScrollable) textView).CanHorizontallyScroll = false;
		textView.Width = _headlessGlyphAdvance * 8;
		textView.Measure(Size.Infinity);

		var visualLine = textView.GetOrConstructVisualLine(document.Lines[0]);

		AreEqual(2, visualLine.TextLines.Count);
		AreEqual("hello ", new string(visualLine.TextLines[0].TextRuns[0].Text.Span));
		AreEqual("world", new string(visualLine.TextLines[1].TextRuns[0].Text.Span));
	}

	#endregion
}