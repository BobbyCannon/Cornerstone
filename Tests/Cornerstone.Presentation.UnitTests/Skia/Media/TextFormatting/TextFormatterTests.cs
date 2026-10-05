#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

[TestClass]
public class TextFormatterTests
{
	#region Methods

	[PresentationTestMethod]
	public void DrawableRunWithSameBaselineAndBiggerHeightShouldNotAlterBaseline()
	{
		using (Start())
		{
			var text = "ABC";

			var typeface = new Typeface(new FontFamily(new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests"), "Noto Mono"));
			var defaultRunProperties = new GenericTextRunProperties(typeface);
			var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties, textWrapping: TextWrapping.Wrap);

			var embeddedTextLine = TextFormatter.Current.FormatLine(new SimpleTextSource(text, defaultRunProperties), 0, 120, paragraphProperties);

			CornerstoneTest.IsNotNull(embeddedTextLine);

			var expectedHeight = embeddedTextLine.Height + 10;

			var embeddedSize = new Size(embeddedTextLine.Width, expectedHeight);

			var expectedBaseline = embeddedTextLine.Baseline;

			var textSource = new ListTextSource(new TextCharacters("ABC", defaultRunProperties), new CustomDrawableRun(embeddedSize, expectedBaseline));

			var textLine = TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(expectedHeight, textLine.Height);

			CornerstoneTest.AreEqual(expectedBaseline, textLine.Baseline);
		}
	}

	[PresentationTestMethod]
	public void DrawableRunWithSameBaselineAndSizeShouldNotAlterLineHeight()
	{
		using (Start())
		{
			var text = "ABC";

			var typeface = new Typeface(new FontFamily(new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests"), "Noto Mono"));
			var defaultRunProperties = new GenericTextRunProperties(typeface);
			var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties, textWrapping: TextWrapping.Wrap);

			var embeddedTextLine = TextFormatter.Current.FormatLine(new SimpleTextSource(text, defaultRunProperties), 0, 120, paragraphProperties);

			CornerstoneTest.IsNotNull(embeddedTextLine);

			var expectedHeight = embeddedTextLine.Height;
			var expectedBaseline = embeddedTextLine.Baseline;

			var textSource = new ListTextSource(new TextCharacters("ABC", defaultRunProperties), new EmbeddedTextLineRun(embeddedTextLine));

			var textLine = TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(expectedHeight, textLine.Height);

			CornerstoneTest.AreEqual(expectedBaseline, textLine.Baseline);
		}
	}

	[PresentationTestMethod]
	public void GetTextBoundsForTextLineWithZeroWidthSpacesDoesNotFreeze()
	{
		var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
		var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);

		using (Start())
		{
			var text = new TextCharacters("\u200B\u200B",
				new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black));

			var source = new ListTextSource(text, new InvisibleRun(1), new TextEndOfParagraph());

			var textLine =
				TextFormatter.Current.FormatLine(source, 0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			var bounds = textLine.GetTextBounds(0, 3);

			CornerstoneTest.AreEqual(1, bounds.Count);

			var runBounds = bounds[0].TextRunBounds;

			CornerstoneTest.AreEqual(2, runBounds.Count);
		}
	}

	[PresentationTestMethod]
	[DataRow(TextWrapping.NoWrap)]
	[DataRow(TextWrapping.Wrap)]
	[DataRow(TextWrapping.WrapWithOverflow)]
	public void LineFormattingForOversizedEmbeddedRunsDoesNotProduceEmptyLines(TextWrapping wrapping)
	{
		var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
		var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties,
			textWrapping: wrapping);

		using (Start())
		{
			var source = new ListTextSource(new RectangleRun(new Rect(0, 0, 200, 10), Brushes.Aqua));
			var textLine = TextFormatter.Current.FormatLine(source, 0, 100, paragraphProperties);
			CornerstoneTest.IsNotNull(textLine);
			CornerstoneTest.AreEqual(200d, textLine.WidthIncludingTrailingWhitespace);
		}
	}

	[PresentationTestMethod]
	[DataRow(TextWrapping.NoWrap)]
	[DataRow(TextWrapping.Wrap)]
	[DataRow(TextWrapping.WrapWithOverflow)]
	public void LineFormattingForOversizedEmbeddedRunsInsideNormalTextDoesNotProduceEmptyLines(
		TextWrapping wrapping)
	{
		var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
		var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties,
			textWrapping: wrapping);

		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#DejaVu Sans"));

			var text1 = new TextCharacters("Hello",
				new GenericTextRunProperties(typeface, foregroundBrush: Brushes.Black));
			var text2 = new TextCharacters("world",
				new GenericTextRunProperties(typeface, foregroundBrush: Brushes.Black));

			var source = new ListTextSource(
				text1,
				new RectangleRun(new Rect(0, 0, 200, 10), Brushes.Aqua),
				new InvisibleRun(1),
				new TextEndOfLine(),
				text2,
				new TextEndOfParagraph(1));

			var lines = new List<TextLine>();
			var dcp = 0;
			for (var c = 0;; c++)
			{
				CornerstoneTest.IsTrue(c < 1000, "Infinite loop");
				var textLine = TextFormatter.Current.FormatLine(source, dcp, 30, paragraphProperties);
				CornerstoneTest.IsNotNull(textLine);
				lines.Add(textLine);
				dcp += textLine.Length;

				if (textLine.TextLineBreak is { } eol && eol.TextEndOfLine is TextEndOfParagraph)
				{
					break;
				}
			}

			CornerstoneTest.NotEmpty(lines);
		}
	}

	[PresentationTestMethod]
	public void LineWithIncrementalTabShouldReturnCorrectBackspacePosition()
	{
		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#DejaVu Sans"));

			var defaultRunProperties = new GenericTextRunProperties(typeface, foregroundBrush: Brushes.Black);
			var paragraphProperties = new IncrementalTabProperties(defaultRunProperties);

			var text = new TextCharacters("ff",
				new GenericTextRunProperties(typeface, foregroundBrush: Brushes.Black));

			var source = new ListTextSource(text);

			var textLine = TextFormatter.Current.FormatLine(source, 0, double.PositiveInfinity, paragraphProperties);
			CornerstoneTest.IsNotNull(textLine);

			var backspaceHit = textLine.GetBackspaceCaretCharacterHit(new CharacterHit(2));
			CornerstoneTest.AreEqual(1, backspaceHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, backspaceHit.TrailingLength);
		}
	}

	[DataRow("0123456789", TextAlignment.Left, FlowDirection.LeftToRight)]
	[DataRow("0123456789", TextAlignment.Center, FlowDirection.LeftToRight)]
	[DataRow("0123456789", TextAlignment.Right, FlowDirection.LeftToRight)]
	[DataRow("0123456789", TextAlignment.Left, FlowDirection.RightToLeft)]
	[DataRow("0123456789", TextAlignment.Center, FlowDirection.RightToLeft)]
	[DataRow("0123456789", TextAlignment.Right, FlowDirection.RightToLeft)]
	[DataRow("שנבגק", TextAlignment.Left, FlowDirection.RightToLeft)]
	[DataRow("שנבגק", TextAlignment.Center, FlowDirection.RightToLeft)]
	[DataRow("שנבגק", TextAlignment.Right, FlowDirection.RightToLeft)]
	[PresentationTestMethod]
	public void ShouldAlignTextLine(string text, TextAlignment textAlignment, FlowDirection flowDirection)
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var paragraphProperties = new GenericTextParagraphProperties(flowDirection, textAlignment, true, true,
				defaultProperties, TextWrapping.NoWrap, 0, 0, 0);

			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, 100, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			var expectedOffset = 0d;

			switch (textAlignment)
			{
				case TextAlignment.Center:
					expectedOffset = 50 - (textLine.Width / 2);
					break;
				case TextAlignment.Right:
					expectedOffset = 100 - textLine.WidthIncludingTrailingWhitespace;
					break;
			}

			CornerstoneTest.AreEqual(expectedOffset, textLine.Start);
		}
	}

	[PresentationTestMethod]
	public void ShouldFormatLineWithDrawableRuns()
	{
		var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
		var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
		var textSource = new CustomTextSource("Hello World ->");

		using (Start())
		{
			var textLine =
				TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(3, textLine.TextRuns.Count);

			CornerstoneTest.IsTrue(textLine.TextRuns[1] is RectangleRun);
		}
	}

	[PresentationTestMethod]
	public void ShouldFormatLineWithEmergencyBreaks()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap);

			var textSource = new SingleBufferTextSource("0123456789_0123456789_0123456789_0123456789", defaultProperties);
			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, 33, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			var remainingRunsLineBreak = CornerstoneTest.IsType<WrappingTextLineBreak>(textLine.TextLineBreak);
			var remainingRuns = remainingRunsLineBreak.AcquireRemainingRuns();
			CornerstoneTest.IsNotNull(remainingRuns);
			CornerstoneTest.NotEmpty(remainingRuns);
		}
	}

	[PresentationTestMethod]
	public void ShouldFormatTextLineWithNonTextTextRuns()
	{
		using (Start())
		{
			var defaultProperties =
				new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

			var textSource = new TextSourceWithDummyRuns(defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(5, textLine.TextRuns.Count);

			CornerstoneTest.AreEqual(14, textLine.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldFormatTextLineWithNonTextTextRunsRightToLeft()
	{
		using (Start())
		{
			var defaultProperties =
				new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

			var textSource = new TextSourceWithDummyRuns(defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Left, true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(5, textLine.TextRuns.Count);

			CornerstoneTest.AreEqual(14, textLine.Length);

			var first = textLine.TextRuns[0] as ShapedTextRun;

			var last = textLine.TextRuns[4] as TextEndOfParagraph;

			CornerstoneTest.IsNotNull(first);

			CornerstoneTest.IsNotNull(last);

			CornerstoneTest.AreEqual("Hello".AsMemory(), first.Text);
		}
	}

	[PresentationTestMethod]
	public void ShouldFormatTextRunsWithDefaultStyle()
	{
		using (Start())
		{
			const string text = "0123456789";

			var defaultProperties =
				new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.Single(textLine.TextRuns);

			var textRun = textLine.TextRuns[0];

			CornerstoneTest.IsNotNull(textRun.Properties);

			CornerstoneTest.AreEqual(defaultProperties.Typeface, textRun.Properties.Typeface);

			CornerstoneTest.AreEqual(defaultProperties.ForegroundBrush, textRun.Properties.ForegroundBrush);

			CornerstoneTest.AreEqual(text.Length, textRun.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldFormatTextRunsWithMultipleBuffers()
	{
		using (Start())
		{
			var defaultProperties =
				new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

			var textSource = new MultiBufferTextSource(defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(5, textLine.TextRuns.Count);

			CornerstoneTest.AreEqual(50, textLine.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldFormatTextRunsWithTextRunStyles()
	{
		using (Start())
		{
			const string text = "0123456789";

			var defaultProperties =
				new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

			var GenericTextRunPropertiesRuns = new[]
			{
				new ValueSpan<TextRunProperties>(0, 3, defaultProperties),
				new ValueSpan<TextRunProperties>(3, 3,
					new GenericTextRunProperties(Typeface.Default, 13, foregroundBrush: Brushes.Black)),
				new ValueSpan<TextRunProperties>(6, 3,
					new GenericTextRunProperties(Typeface.Default, 14, foregroundBrush: Brushes.Black)),
				new ValueSpan<TextRunProperties>(9, 1, defaultProperties)
			};

			var textSource = new FormattedTextSource(text, defaultProperties, GenericTextRunPropertiesRuns);

			var formatter = new TextFormatterImpl();

			var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(text.Length, textLine.Length);

			for (var i = 0; i < GenericTextRunPropertiesRuns.Length; i++)
			{
				var GenericTextRunPropertiesRun = GenericTextRunPropertiesRuns[i];

				var textRun = textLine.TextRuns[i];

				CornerstoneTest.AreEqual(GenericTextRunPropertiesRun.Length, textRun.Length);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldFormatWithEndOfLineRun()
	{
		using (Start())
		{
			var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
			var textSource = new EndOfLineTextSource();

			var textLine =
				TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.IsNotNull(textLine.TextLineBreak);

			CornerstoneTest.AreEqual(TextRun.DefaultTextSourceLength, textLine.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldHitTestStringWithInvisibleRuns()
	{
		var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
		var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);

		//var textSource = new ListTextSource(

		using (Start())
		{
			var hello = new TextCharacters("Hello",
				new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black));
			var world = new TextCharacters("world",
				new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Red));

			var source = new ListTextSource(new InvisibleRun(1), hello, new InvisibleRun(1), world);

			var textLine =
				TextFormatter.Current.FormatLine(source, 0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			void VerifyHit(int offset)
			{
				var glyphCenter = textLine.GetTextBounds(offset, 1)[0].Rectangle.Center;
				var hit = textLine.GetCharacterHitFromDistance(glyphCenter.X);
				CornerstoneTest.AreEqual(offset, hit.FirstCharacterIndex);
			}

			VerifyHit(3);
			VerifyHit(8);
		}
	}

	[PresentationTestMethod]
	public void ShouldMatchCharacterForSpacingCombiningMark()
	{
		using (Start())
		{
			var text = "𖾇";

			var typeface = new Typeface(new FontFamily(new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests"), "Noto Mono"));
			var defaultRunProperties = new GenericTextRunProperties(typeface);
			var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties, textWrapping: TextWrapping.Wrap);
			var textLine = TextFormatter.Current.FormatLine(new SimpleTextSource(text, defaultRunProperties), 0, 120, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			var textRuns = textLine.TextRuns;

			CornerstoneTest.NotEmpty(textRuns);

			var firstRun = textRuns[0];

			CornerstoneTest.IsNotNull(firstRun.Properties);

			CornerstoneTest.AreEqual("Noto Sans Miao", firstRun.Properties.Typeface.GlyphTypeface.FamilyName);
		}
	}

	[PresentationTestMethod]
	public void ShouldNotAbsorbWhitespaceIntoAFallbackRun()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			const string text = "👍 👍 👍 👍";

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			// Four emoji in a fallback font, separated by three spaces the primary font covers:
			// the spaces keep the primary's metrics instead of the emoji font's, so they form
			// runs of their own.
			CornerstoneTest.AreEqual(7, textLine.TextRuns.Count);

			for (var i = 0; i < textLine.TextRuns.Count; i++)
			{
				var isSpace = (i % 2) == 1;

				CornerstoneTest.AreEqual(isSpace ? 1 : 2, textLine.TextRuns[i].Length);
				CornerstoneTest.AreEqual(isSpace, defaultProperties.Typeface == textLine.TextRuns[i].Properties!.Typeface);
			}
		}
	}

	[DataRow("פעילות הבינאום, W3C!")]
	[DataRow("abcABC")]
	[DataRow("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן")]
	[DataRow("טטטט abcDEF טטטט")]
	[PresentationTestMethod]
	public void ShouldNotAlterTextRunsAfterTextStylesWereApplied(string text)
	{
		using (Start())
		{
			var formatter = new TextFormatterImpl();

			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var paragraphProperties =
				new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.NoWrap);

			var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

			var expectedTextLine = formatter.FormatLine(new SingleBufferTextSource(text, defaultProperties),
				0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNotNull(expectedTextLine);

			var expectedRuns = expectedTextLine.TextRuns.Cast<ShapedTextRun>().ToList();

			var expectedGlyphs = expectedRuns
				.SelectMany(run => run.GlyphRun.GlyphInfos, (_, glyph) => glyph.GlyphIndex)
				.ToList();

			for (var i = 0; i < text.Length; i++)
			{
				for (var j = 1; (i + j) < text.Length; j++)
				{
					var spans = new[]
					{
						new ValueSpan<TextRunProperties>(i, j,
							new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
					};

					var textSource = new FormattedTextSource(text, defaultProperties, spans);

					var textLine =
						formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

					CornerstoneTest.IsNotNull(textLine);

					var shapedRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

					var actualGlyphs = shapedRuns
						.SelectMany(x => x.GlyphRun.GlyphInfos, (_, glyph) => glyph.GlyphIndex)
						.ToList();

					CornerstoneTest.AreEqual(expectedGlyphs, actualGlyphs);
				}
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldNotProduceTextLineWiderThanParagraphWidth()
	{
		using (Start())
		{
			const string text =
				"Multiline TextBlock with TextWrapping.\r\rLorem ipsum dolor sit amet, consectetur adipiscing elit. " +
				"Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. " +
				"Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. " +
				"Vivamus pretium ornare est.";

			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textSourceIndex = 0;

			while (textSourceIndex < text.Length)
			{
				var textLine =
					formatter.FormatLine(textSource, textSourceIndex, 200, paragraphProperties);

				CornerstoneTest.IsNotNull(textLine);

				CornerstoneTest.IsTrue(textLine.Width <= 200);

				textSourceIndex += textLine.Length;
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldProduceFixedHeightLines()
	{
		using (Start())
		{
			const string text = "012345";

			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties, lineHeight: 50));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(50, textLine.Height);
		}
	}

	[PresentationTestMethod]
	[DataRow("0123", 1)]
	[DataRow("\r\n", 1)]
	[DataRow("👍b", 2)]
	[DataRow("a👍b", 3)]
	[DataRow("a👍子b", 4)]
	public void ShouldProduceUniqueRuns(string text, int numberOfRuns)
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(numberOfRuns, textLine.TextRuns.Count);
		}
	}

	[DataRow("Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor",
		new[] { "Lorem ipsum ", "dolor sit amet, ", "consectetur ", "adipisicing ", "elit, sed do ", "eiusmod tempor" })]
	[PresentationTestMethod]
	public void ShouldProduceWrappedAndTrimmedLines(string text, string[] expectedLines)
	{
		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface, 32, foregroundBrush: Brushes.Black);

			var styleSpans = new[]
			{
				new ValueSpan<TextRunProperties>(0, 5,
					new GenericTextRunProperties(typeface, 48)),
				new ValueSpan<TextRunProperties>(6, 11,
					new GenericTextRunProperties(new Typeface(FontFamily.Default, weight: FontWeight.Bold), 32)),
				new ValueSpan<TextRunProperties>(28, 28,
					new GenericTextRunProperties(new Typeface(FontFamily.Default, FontStyle.Italic), 32))
			};

			var textSource = new FormattedTextSource(text, defaultProperties, styleSpans);

			var formatter = new TextFormatterImpl();

			var currentPosition = 0;

			var currentHeight = 0d;

			var currentLineIndex = 0;

			while ((currentPosition < text.Length) && (currentLineIndex < expectedLines.Length))
			{
				var textLine =
					formatter.FormatLine(textSource, currentPosition, 300,
						new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.WrapWithOverflow));

				CornerstoneTest.IsNotNull(textLine);

				currentPosition += textLine.Length;

				if ((textLine.Width > 300) || ((currentHeight + textLine.Height) > 240))
				{
					textLine = textLine.Collapse(new TextTrailingWordEllipsis(TextTrimming.DefaultEllipsisChar, 300, defaultProperties, FlowDirection.LeftToRight));
				}

				currentHeight += textLine.Height;

				var currentText = text.Substring(textLine.FirstTextSourceIndex, textLine.Length);

				CornerstoneTest.AreEqual(expectedLines[currentLineIndex], currentText);

				currentLineIndex++;
			}

			CornerstoneTest.AreEqual(expectedLines.Length, currentLineIndex);
		}
	}

	[PresentationTestMethod]
	public void ShouldResetBidiLevelsOfTrailingWhitespacesAfterTextWrapping()
	{
		using (Start())
		{
			const string text = "aaa bbb";

			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var paragraphProperties = new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Right, true,
				true, defaultProperties, TextWrapping.Wrap, 0, 0, 0);

			var textSource = new SimpleTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var firstLine = formatter.FormatLine(textSource, 0, 50, paragraphProperties);

			CornerstoneTest.IsNotNull(firstLine);

			CornerstoneTest.AreEqual(2, firstLine.TextRuns.Count);

			var first = firstLine.TextRuns[0] as ShapedTextRun;

			var second = firstLine.TextRuns[1] as ShapedTextRun;

			CornerstoneTest.IsNotNull(first);

			CornerstoneTest.IsNotNull(second);

			CornerstoneTest.AreEqual(" ", first.Text.ToString());

			CornerstoneTest.AreEqual("aaa", second.Text.ToString());

			CornerstoneTest.AreEqual(1, first.BidiLevel);

			CornerstoneTest.AreEqual(2, second.BidiLevel);
		}
	}

	[PresentationTestMethod]
	public void ShouldResetBidiLevelsOfTrailingWhitespacesAfterTextWrapping2()
	{
		using (Start())
		{
			const string text = "אאא בבב";

			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var paragraphProperties = new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left, true,
				true, defaultProperties, TextWrapping.Wrap, 0, 0, 0);

			var textSource = new SimpleTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var firstLine = formatter.FormatLine(textSource, 0, 40, paragraphProperties);

			CornerstoneTest.IsNotNull(firstLine);

			CornerstoneTest.AreEqual(2, firstLine.TextRuns.Count);

			var first = firstLine.TextRuns[0] as ShapedTextRun;

			var second = firstLine.TextRuns[1] as ShapedTextRun;

			CornerstoneTest.IsNotNull(first);

			CornerstoneTest.IsNotNull(second);

			CornerstoneTest.AreEqual("אאא", first.Text.ToString());

			CornerstoneTest.AreEqual(" ", second.Text.ToString());

			CornerstoneTest.AreEqual(1, first.BidiLevel);

			CornerstoneTest.AreEqual(0, second.BidiLevel);
		}
	}

	[PresentationTestMethod]
	public void ShouldRetainTextEndOfParagraphWithTextWrapping()
	{
		using (Start())
		{
			var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties, textWrapping: TextWrapping.Wrap);

			var text = "Hello World";

			var textSource = new SimpleTextSource(text, defaultRunProperties);

			var pos = 0;

			TextLineBreak previousLineBreak = null;
			TextLine textLine = null;

			while (pos < text.Length)
			{
				textLine = TextFormatter.Current.FormatLine(textSource, pos, 30, paragraphProperties, previousLineBreak);

				CornerstoneTest.IsNotNull(textLine);

				pos += textLine.Length;

				previousLineBreak = textLine.TextLineBreak;
			}

			CornerstoneTest.IsNotNull(textLine);
			CornerstoneTest.IsNotNull(textLine.TextLineBreak);
			CornerstoneTest.IsNotNull(textLine.TextLineBreak.TextEndOfLine);
		}
	}

	[PresentationTestMethod]
	public void ShouldReturnNullForEmptyTextSource()
	{
		using (Start())
		{
			var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
			var textSource = new EmptyTextSource();

			var textLine = TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNull(textLine);
		}
	}

	[PresentationTestMethod]
	public void ShouldSplitRunOnScript()
	{
		using (Start())
		{
			const string text = "ABCDالدولي";

			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var firstRun = textLine.TextRuns[0];

			CornerstoneTest.AreEqual(4, firstRun.Length);
		}
	}

	[DataRow("Whether to turn off HTTPS. This option only applies if Individual, " +
		"IndividualB2C, SingleOrg, or MultiOrg aren't used for &#8209;&#8209;auth."
		, "Noto Sans", 40)]
	[DataRow("01234 56789 01234 56789", "Noto Mono", 7)]
	[PresentationTestMethod]
	public void ShouldWrap(string text, string familyName, int numberOfCharactersPerLine)
	{
		using (Start())
		{
			var lineBreaker = new LineBreakEnumerator(text);

			var expected = new List<int>();

			while (lineBreaker.MoveNext(out var lineBreak))
			{
				expected.Add(lineBreak.PositionWrap - 1);
			}

			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#" +
				familyName);

			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var glyph = typeface.GlyphTypeface.CharacterToGlyphMap['a'];

			typeface.GlyphTypeface.TryGetHorizontalGlyphAdvance(glyph, out var advance);

			var scale = 12.0 / typeface.GlyphTypeface.Metrics.DesignEmHeight;

			var paragraphWidth = advance * scale * numberOfCharactersPerLine;

			var currentPosition = 0;

			while (currentPosition < text.Length)
			{
				var textLine =
					formatter.FormatLine(textSource, currentPosition, paragraphWidth,
						new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap));

				CornerstoneTest.IsNotNull(textLine);

				var end = (textLine.FirstTextSourceIndex + textLine.Length) - 1;

				CornerstoneTest.IsTrue(expected.Contains(end));

				var index = expected.IndexOf(end);

				for (var i = 0; i <= index; i++)
				{
					expected.RemoveAt(0);
				}

				currentPosition += textLine.Length;
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldWrapChinese()
	{
		using (Start())
		{
			var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties, textWrapping: TextWrapping.Wrap);

			var text = "一二三四 TEXT 一二三四五六七八九十零";

			var textLine = TextFormatter.Current.FormatLine(new SimpleTextSource(text, defaultRunProperties), 0, 120, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);
			CornerstoneTest.AreEqual(3, textLine.TextRuns.Count);
		}
	}

	[PresentationTestMethod]
	public void ShouldWrapSyriac()
	{
		using (Start())
		{
			const string text =
				"܀ ܁ ܂ ܃ ܄ ܅ ܆ ܇ ܈ ܉ ܊ ܋ ܌ ܍ ܏ ܐ ܑ ܒ ܓ ܔ ܕ ܖ ܗ ܘ ܙ ܚ ܛ ܜ ܝ ܞ ܟ ܠ ܡ ܢ ܣ ܤ ܥ ܦ ܧ ܨ ܩ ܪ ܫ ܬ ܰ ܱ ܲ ܳ ܴ ܵ ܶ ܷ ܸ ܹ ܺ ܻ ܼ ܽ ܾ ܿ ݀ ݁ ݂ ݃ ݄ ݅ ݆ ݇ ݈ ݉ ݊";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var paragraphProperties =
				new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap);

			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			var textPosition = 87;
			TextLineBreak lastBreak = null;

			while (textPosition < text.Length)
			{
				var textLine =
					formatter.FormatLine(textSource, textPosition, 50, paragraphProperties, lastBreak);

				CornerstoneTest.IsNotNull(textLine);

				CornerstoneTest.AreEqual(textLine.Length, textLine.TextRuns.Sum(x => x.Length));

				textPosition += textLine.Length;

				lastBreak = textLine.TextLineBreak;
			}
		}
	}

	[DataRow("𐐷𐐷𐐷𐐷𐐷", 10, 1)]
	[DataRow("01234 56789 01234 56789", 6, 4)]
	[PresentationTestMethod]
	public void ShouldWrapWithOverflow(string text, int expectedCharactersPerLine, int expectedNumberOfLines)
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.WrapWithOverflow);

			var textSource = new SingleBufferTextSource("ABCDEFHFFHFJHKHFK", defaultProperties, true);

			var formatter = new TextFormatterImpl();

			formatter.FormatLine(textSource, 0, 33, paragraphProperties);

			textSource = new SingleBufferTextSource(text, defaultProperties);

			var numberOfLines = 0;

			var currentPosition = 0;

			while (currentPosition < text.Length)
			{
				var textLine =
					formatter.FormatLine(textSource, currentPosition, 1, paragraphProperties);

				CornerstoneTest.IsNotNull(textLine);

				if ((text.Length - currentPosition) > expectedCharactersPerLine)
				{
					CornerstoneTest.AreEqual(expectedCharactersPerLine, textLine.Length);
				}

				currentPosition += textLine.Length;

				numberOfLines++;
			}

			CornerstoneTest.AreEqual(expectedNumberOfLines, numberOfLines);
		}
	}

	public static IDisposable Start()
	{
		var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
			.With(renderInterface: new PlatformRenderInterface()));

		var customFontManagerImpl = new CustomFontManagerImpl();

		PresentationLocator.CurrentMutable
			.Bind<IFontManagerImpl>().ToConstant(customFontManagerImpl);

		var fontManager = new FontManager(customFontManagerImpl);

		PresentationLocator.CurrentMutable
			.Bind<FontManager>().ToConstant(fontManager);

		fontManager.AddFontCollection(customFontManagerImpl.SystemFonts);

		return disposable;
	}

	// The expectations concatenate the run texts in visual run order, so where the spaces sit
	// in the string depends on how the line is cut into runs. Each space is a run of its own
	// now (the primary font owns it, not the Hebrew fallback), which regroups the same
	// characters - the right-to-left rows below show the same content, differently split.
	[PresentationTestMethod]
	[DataRow("one שתיים three ארבע", "one שתיים thr…", FlowDirection.LeftToRight, false)]
	[DataRow("one שתיים three ארבע", "…thr שתיים one", FlowDirection.RightToLeft, false)]
	[DataRow("one שתיים three ארבע", "one שתיים…", FlowDirection.LeftToRight, true)]
	[DataRow("one שתיים three ארבע", "… שתיים one", FlowDirection.RightToLeft, true)]
	public void TextTrimmingShouldTrimCorrectly(string text, string trimmed, FlowDirection direction, bool wordEllipsis)
	{
		const double Width = 160.0;
		const double EmSize = 20.0;

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default, EmSize);

			var paragraphProperties = new GenericTextParagraphProperties(direction, TextAlignment.Start, true,
				true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0);

			var textSource = new SimpleTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

			CornerstoneTest.IsNotNull(textLine);

			var textTrimming = wordEllipsis ? TextTrimming.WordEllipsis : TextTrimming.CharacterEllipsis;

			var collapsingProperties = textTrimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(Width, defaultProperties, direction));

			var collapsedLine = textLine.Collapse(collapsingProperties);

			CornerstoneTest.IsNotNull(collapsedLine);

			var trimmedResult = string.Concat(collapsedLine.TextRuns.Select(x => x.Text));

			CornerstoneTest.AreEqual(trimmed, trimmedResult);
		}
	}

	[PresentationTestMethod]
	public void WrapShouldNotProduceEmptyLines()
	{
		using (Start())
		{
			const string text = "012345";

			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			var textSourceIndex = 0;

			while (textSourceIndex < text.Length)
			{
				var textLine =
					formatter.FormatLine(textSource, textSourceIndex, 3, paragraphProperties);

				CornerstoneTest.IsNotNull(textLine);

				CornerstoneTest.AreNotEqual(0, textLine.Length);

				textSourceIndex += textLine.Length;
			}

			CornerstoneTest.AreEqual(text.Length, textSourceIndex);
		}
	}

	#endregion

	#region Classes

	internal class ListTextSource : ITextSource
	{
		#region Fields

		private readonly List<TextRun> _runs;

		#endregion

		#region Constructors

		public ListTextSource(params TextRun[] runs) : this((IEnumerable<TextRun>) runs)
		{
		}

		public ListTextSource(IEnumerable<TextRun> runs)
		{
			_runs = runs.ToList();
		}

		#endregion

		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			var off = 0;
			for (var c = 0; c < _runs.Count; c++)
			{
				var run = _runs[c];
				if ((textSourceIndex >= off) && ((textSourceIndex - off) < run.Length))
				{
					if (run.Length == 1)
					{
						return run;
					}
					var chars = (TextCharacters) run;
					return new TextCharacters(chars.Text.Slice(textSourceIndex - off), chars.Properties);
				}

				off += run.Length;
			}

			return null;
		}

		#endregion
	}

	private class CustomDrawableRun : DrawableTextRun
	{
		#region Constructors

		public CustomDrawableRun(Size size, double baseLine)
		{
			Size = size;
			Baseline = baseLine;
		}

		#endregion

		#region Properties

		public override double Baseline { get; }

		public override Size Size { get; }

		#endregion

		#region Methods

		public override void Draw(DrawingContext drawingContext, Point origin)
		{
			// no op
		}

		#endregion
	}

	private class CustomTextSource : ITextSource
	{
		#region Fields

		private readonly string _text;

		#endregion

		#region Constructors

		public CustomTextSource(string text)
		{
			_text = text;
		}

		#endregion

		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			if (textSourceIndex >= (_text.Length + TextRun.DefaultTextSourceLength + _text.Length))
			{
				return null;
			}

			if (textSourceIndex == _text.Length)
			{
				return new RectangleRun(new Rect(0, 0, 50, 50), Brushes.Green);
			}

			return new TextCharacters(_text, new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black));
		}

		#endregion
	}

	private class EmbeddedTextLineRun : DrawableTextRun
	{
		#region Fields

		private readonly TextLine _textLine;

		#endregion

		#region Constructors

		public EmbeddedTextLineRun(TextLine textLine)
		{
			_textLine = textLine;
		}

		#endregion

		#region Properties

		public override double Baseline => _textLine.Baseline;
		public override Size Size => new(_textLine.Width, _textLine.Height);

		#endregion

		#region Methods

		public override void Draw(DrawingContext drawingContext, Point origin)
		{
			_textLine.Draw(drawingContext, origin);
		}

		#endregion
	}

	private class EmptyTextSource : ITextSource
	{
		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			return null;
		}

		#endregion
	}

	private class EndOfLineTextSource : ITextSource
	{
		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			return new TextEndOfLine();
		}

		#endregion
	}

	private class IncrementalTabProperties : TextParagraphProperties
	{
		#region Constructors

		public IncrementalTabProperties(TextRunProperties defaultTextRunProperties)
		{
			DefaultTextRunProperties = defaultTextRunProperties;
		}

		#endregion

		#region Properties

		public override double DefaultIncrementalTab => 64;
		public override TextRunProperties DefaultTextRunProperties { get; }
		public override bool FirstLineInParagraph => default;

		public override FlowDirection FlowDirection => default;
		public override double Indent => default;
		public override double LineHeight => default;
		public override TextAlignment TextAlignment => default;
		public override TextWrapping TextWrapping => default;

		#endregion
	}

	private class InvisibleRun : TextRun
	{
		#region Constructors

		public InvisibleRun(int length)
		{
			Length = length;
		}

		#endregion

		#region Properties

		public override int Length { get; }

		#endregion
	}

	private class RectangleRun : DrawableTextRun
	{
		#region Fields

		private readonly IBrush _fill;
		private readonly Rect _rect;

		#endregion

		#region Constructors

		public RectangleRun(Rect rect, IBrush fill)
		{
			_rect = rect;
			_fill = fill;
		}

		#endregion

		#region Properties

		public override double Baseline => 0;

		public override Size Size => _rect.Size;

		#endregion

		#region Methods

		public override void Draw(DrawingContext drawingContext, Point origin)
		{
			using (drawingContext.PushTransform(Matrix.CreateTranslation(new Vector(origin.X, 0))))
			{
				drawingContext.FillRectangle(_fill, _rect);
			}
		}

		#endregion
	}

	private class TextSourceWithDummyRuns : ITextSource
	{
		#region Fields

		private readonly TextRunProperties _properties;
		private readonly List<ValueSpan<TextRun>> _textRuns;

		#endregion

		#region Constructors

		public TextSourceWithDummyRuns(TextRunProperties properties)
		{
			_properties = properties;

			_textRuns = new List<ValueSpan<TextRun>>
			{
				new(0, 5, new TextCharacters("Hello", _properties)),
				new(5, 1, new DummyRun()),
				new(6, 1, new DummyRun()),
				new(7, 6, new TextCharacters(" World", _properties))
			};
		}

		#endregion

		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			foreach (var run in _textRuns)
			{
				if (textSourceIndex < (run.Start + run.Length))
				{
					return run.Value;
				}
			}

			return new TextEndOfParagraph();
		}

		#endregion

		#region Classes

		private class DummyRun : TextRun
		{
			#region Constructors

			public DummyRun()
			{
				Length = DefaultTextSourceLength;
			}

			#endregion

			#region Properties

			public override int Length { get; }

			#endregion
		}

		#endregion
	}

	#endregion

	#region Records

	protected readonly record struct SimpleTextSource : ITextSource
	{
		#region Fields

		private readonly TextRunProperties _defaultProperties;
		private readonly string _text;

		#endregion

		#region Constructors

		public SimpleTextSource(string text, TextRunProperties defaultProperties)
		{
			_text = text;
			_defaultProperties = defaultProperties;
		}

		#endregion

		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			if (textSourceIndex > _text.Length)
			{
				return new TextEndOfParagraph();
			}

			var runText = _text.AsMemory(textSourceIndex);

			if (runText.IsEmpty)
			{
				return new TextEndOfParagraph();
			}

			return new TextCharacters(runText, _defaultProperties);
		}

		#endregion
	}

	#endregion
}