#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

[TestClass]
public class TextLineTests
{
	#region Constants

	private const string smultiLineText = "012345678\r\r0123456789";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void BackspaceShouldTreatCRLFAsAUnit()
	{
		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Manrope"));
			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new SingleBufferTextSource("one\r\n", defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var backspaceHit = textLine.GetBackspaceCaretCharacterHit(new CharacterHit(5));

			CornerstoneTest.AreEqual(3, backspaceHit.FirstCharacterIndex);
		}
	}

	public static string ExtractTextFromRuns(TextLine textLine)
	{
		// Only extract text for ShapedTextRun instances.
		return string.Concat(textLine.TextRuns
			.OfType<ShapedTextRun>()
			.Select(r => r.Text.ToString()));
	}

	[PresentationTestMethod]
	public void ShouldAddHalfLineGapToBaseline()
	{
		using (Start())
		{
			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Inter");
			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource("F", defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textMetrics = new TextMetrics(typeface.GlyphTypeface, 12);

			var expectedBaseline = -textMetrics.Ascent + (textMetrics.LineGap / 2);

			CornerstoneTest.AreEqual(expectedBaseline, textLine.Baseline);
		}
	}

	[PresentationTestMethod]
	public void ShouldClampBaselineWhenLineHeightIsSmallerThanNatural()
	{
		using (Start())
		{
			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Inter");
			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource("F", defaultProperties);

			var formatter = new TextFormatterImpl();

			var textMetrics = new TextMetrics(typeface.GlyphTypeface, 12);
			var natural = -textMetrics.Ascent + textMetrics.Descent + textMetrics.LineGap;

			var smallerLineHeight = natural - 2;

			// Force a smaller line height than ascent+descent+lineGap
			var paragraphProps = new GenericTextParagraphProperties(defaultProperties, lineHeight: smallerLineHeight);

			var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProps);

			CornerstoneTest.IsNotNull(textLine);

			// In this case, baseline should equal -Ascent (lineGap ignored)
			var expectedBaseline = -textMetrics.Ascent;

			CornerstoneTest.AreEqual(expectedBaseline, textLine.Baseline);
			CornerstoneTest.AreEqual(paragraphProps.LineHeight, textLine.Height);
		}
	}

	[PresentationTestMethod]
	[DataRow("01234 01234 01234", 120, nameof(TextTrimming.PrefixCharacterEllipsis), "01234 01\u20264 01234")]
	[DataRow("01234 01234", 58, nameof(TextTrimming.CharacterEllipsis), "01234 0\u2026")]
	[DataRow("01234 01234", 58, nameof(TextTrimming.WordEllipsis), "01234\u2026")]
	[DataRow("01234", 9, nameof(TextTrimming.CharacterEllipsis), "\u2026")]
	[DataRow("01234", 2, nameof(TextTrimming.CharacterEllipsis), "")]
	public void ShouldCollapseLine(string text, double width, string trimmingName, string expected)
	{
		using (Start())
		{
			var trimming = ResolveTrimming(trimmingName);
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.IsFalse(textLine.HasCollapsed);

			var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(width, defaultProperties, FlowDirection.LeftToRight));

			var collapsedLine = textLine.Collapse(collapsingProperties);

			CornerstoneTest.IsTrue(collapsedLine.HasCollapsed);

			var trimmedText = collapsedLine.TextRuns.SelectMany(x => x.Text.ToString()).ToArray();

			CornerstoneTest.AreEqual(expected.Length, trimmedText.Length);

			for (var i = 0; i < expected.Length; i++)
			{
				CornerstoneTest.AreEqual(expected[i], trimmedText[i]);
			}
		}
	}

	[PresentationTestMethod]
	[DataRow("directory\\file.txt")]
	[DataRow("directory/file.txt")]
	public void ShouldCollapseWithEllipsis(string path)
	{
		var typeface = Typeface.Default;
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource(path, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var trimming = new TextPathSegmentTrimming("*");

			var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(8, defaultProperties, FlowDirection.LeftToRight));

			var collapsedLine = textLine.Collapse(collapsingProperties);

			CornerstoneTest.IsNotNull(collapsedLine);

			var result = ExtractTextFromRuns(collapsedLine);

			CornerstoneTest.AreEqual("*", result);
		}
	}

	[PresentationTestMethod]
	public void ShouldCollapseWithTextPathSegmentTrimmingNoSpace()
	{
		var text = "foo";

		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var trimming = new TextPathSegmentTrimming("*");

			var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(8, defaultProperties, FlowDirection.LeftToRight));

			var collapsedLine = textLine.Collapse(collapsingProperties);

			CornerstoneTest.IsNotNull(collapsedLine);

			var result = ExtractTextFromRuns(collapsedLine);

			CornerstoneTest.AreEqual("*", result);
		}
	}

	[PresentationTestMethod]
	public void ShouldCollapseWithTextPathSegmentTrimmingWithoutPathSegment()
	{
		var text = "foo";

		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var trimming = new TextPathSegmentTrimming("*");

			var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(15, defaultProperties, FlowDirection.LeftToRight));

			var collapsedLine = textLine.Collapse(collapsingProperties);

			CornerstoneTest.IsNotNull(collapsedLine);

			var result = ExtractTextFromRuns(collapsedLine);

			CornerstoneTest.AreEqual("*o", result);
		}
	}

	[PresentationTestMethod]
	public void ShouldDistributeExtraSpaceWhenLineHeightIsLargerThanNatural()
	{
		using (Start())
		{
			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Inter");
			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource("F", defaultProperties);

			var formatter = new TextFormatterImpl();

			var textMetrics = new TextMetrics(typeface.GlyphTypeface, 12);
			var natural = -textMetrics.Ascent + textMetrics.Descent + textMetrics.LineGap;

			var largerLineHeight = natural + 50;

			var paragraphProps = new GenericTextParagraphProperties(defaultProperties, lineHeight: largerLineHeight);

			var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProps);

			CornerstoneTest.IsNotNull(textLine);

			// Extra space is distributed evenly above and below
			var extra = largerLineHeight - (textMetrics.Descent - textMetrics.Ascent);
			var expectedBaseline = -textMetrics.Ascent + (extra / 2);

			CornerstoneTest.AreEqual(expectedBaseline, textLine.Baseline, 5);
			CornerstoneTest.AreEqual(largerLineHeight, textLine.Height, 5);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetCharacterHitForDistanceWithTextEndOfLine()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource("Hello World", defaultProperties, true);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, 1000,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var characterHit = textLine.GetCharacterHitFromDistance(1000);

			CornerstoneTest.AreEqual(10, characterHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(1, characterHit.TrailingLength);
		}
	}

	[DataRow("ABC012345")] //LeftToRight
	[DataRow("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן")] //RightToLeft
	[PresentationTestMethod]
	public void ShouldGetCharacterHitFromDistance(string text)
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

			var isRightToLeft = IsRightToLeft(textLine);
			var rects = BuildRects(textLine);
			var glyphClusters = BuildGlyphClusters(textLine);

			for (var i = 0; i < rects.Count; i++)
			{
				var cluster = glyphClusters[i];
				var rect = rects[i];

				var characterHit = textLine.GetCharacterHitFromDistance(rect.Left);

				CornerstoneTest.AreEqual(isRightToLeft ? cluster + 1 : cluster, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetCharacterHitFromDistanceForDrawableRuns()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new DrawableRunTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var characterHit = textLine.GetCharacterHitFromDistance(50);

			CornerstoneTest.AreEqual(5, characterHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(1, characterHit.TrailingLength);

			characterHit = textLine.GetCharacterHitFromDistance(32);

			CornerstoneTest.AreEqual(3, characterHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetCharacterHitFromDistanceFromMixedTextBuffer()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new MixedTextBufferTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 20, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var characterHit = textLine.GetCharacterHitFromDistance(double.PositiveInfinity);

			CornerstoneTest.AreEqual(40, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetDistanceFromCharacterHit()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(smultiLineText, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var currentDistance = 0.0;

			foreach (var run in textLine.TextRuns)
			{
				var textRun = (ShapedTextRun) run;

				var glyphRun = textRun.GlyphRun;

				for (var i = 0; i < glyphRun.GlyphInfos.Count; i++)
				{
					var cluster = glyphRun.GlyphInfos[i].GlyphCluster;

					var advance = glyphRun.GlyphInfos[i].GlyphAdvance;

					var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(cluster));

					CornerstoneTest.AreEqual(currentDistance, distance);

					currentDistance += advance;
				}
			}

			var actualDistance = textLine.GetDistanceFromCharacterHit(new CharacterHit(smultiLineText.Length));

			CornerstoneTest.AreEqual(currentDistance, actualDistance);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetDistanceFromCharacterHitDrawableRuns()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new DrawableRunTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(1));

			CornerstoneTest.AreEqual(14, distance);

			distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(2));

			CornerstoneTest.IsTrue(distance > 14);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetDistanceFromCharacterHitMixedTextBuffer()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new MixedTextBufferTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(10));

			CornerstoneTest.AreEqual(72.01171875, distance);

			distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(20));

			CornerstoneTest.AreEqual(144.0234375, distance);

			distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(30));

			CornerstoneTest.AreEqual(216.03515625, distance);

			distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(40));

			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, distance);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetFirstCharacterHit()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(smultiLineText, defaultProperties);

			var formatter = new TextFormatterImpl();

			var currentIndex = 0;

			while (currentIndex < smultiLineText.Length)
			{
				var textLine =
					formatter.FormatLine(textSource, currentIndex, double.PositiveInfinity,
						new GenericTextParagraphProperties(defaultProperties));

				CornerstoneTest.IsNotNull(textLine);

				var firstCharacterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(int.MinValue));

				CornerstoneTest.AreEqual(textLine.FirstTextSourceIndex, firstCharacterHit.FirstCharacterIndex);

				currentIndex += textLine.Length;
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetInClusterBackspaceHit()
	{
		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Manrope"));
			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new SingleBufferTextSource("ff", defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var backspaceHit = textLine.GetBackspaceCaretCharacterHit(new CharacterHit(1, 1));

			CornerstoneTest.AreEqual(1, backspaceHit.FirstCharacterIndex);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetLastCharacterHit()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(smultiLineText, defaultProperties);

			var formatter = new TextFormatterImpl();

			var currentIndex = 0;

			while (currentIndex < smultiLineText.Length)
			{
				var textLine =
					formatter.FormatLine(textSource, currentIndex, double.PositiveInfinity,
						new GenericTextParagraphProperties(defaultProperties));

				CornerstoneTest.IsNotNull(textLine);

				var lastCharacterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(int.MaxValue));

				CornerstoneTest.AreEqual(textLine.FirstTextSourceIndex + textLine.Length, lastCharacterHit.FirstCharacterIndex + lastCharacterHit.TrailingLength);

				currentIndex += textLine.Length;
			}
		}
	}

	[DataRow("𐐷𐐷𐐷𐐷𐐷")]
	[DataRow("01234567🎉\n")]
	[DataRow("𐐷1234")]
	[PresentationTestMethod]
	public void ShouldGetNextCaretCharacterHit(string text)
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

			var clusters = BuildGlyphClusters(textLine);

			var nextCharacterHit = new CharacterHit(0);

			for (var i = 0; i < clusters.Count; i++)
			{
				var expectedCluster = clusters[i];
				var actualCluster = nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength;

				CornerstoneTest.AreEqual(expectedCluster, actualCluster);

				nextCharacterHit = textLine.GetNextCaretCharacterHit(nextCharacterHit);
			}

			var lastCharacterHit = nextCharacterHit;

			nextCharacterHit = textLine.GetNextCaretCharacterHit(lastCharacterHit);

			CornerstoneTest.AreEqual(lastCharacterHit.FirstCharacterIndex, nextCharacterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(lastCharacterHit.TrailingLength, nextCharacterHit.TrailingLength);

			nextCharacterHit = new CharacterHit(0, clusters[1] - clusters[0]);

			foreach (var cluster in clusters)
			{
				CornerstoneTest.AreEqual(cluster, nextCharacterHit.FirstCharacterIndex);

				nextCharacterHit = textLine.GetNextCaretCharacterHit(nextCharacterHit);
			}

			lastCharacterHit = nextCharacterHit;

			nextCharacterHit = textLine.GetNextCaretCharacterHit(lastCharacterHit);

			CornerstoneTest.AreEqual(lastCharacterHit.FirstCharacterIndex, nextCharacterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(lastCharacterHit.TrailingLength, nextCharacterHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetNextCaretCharacterHitBidi()
	{
		const string text = "אבג 1 ABC";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var clusters = new List<int>();

			foreach (var textRun in textLine.TextRuns.OrderBy(x => TextTestHelper.GetStartCharIndex(x.Text)))
			{
				var shapedRun = (ShapedTextRun) textRun;
				var runOffset = TextTestHelper.GetStartCharIndex(shapedRun.Text);

				var runClusters = shapedRun.ShapedBuffer.Select(glyph => glyph.GlyphCluster + runOffset);

				clusters.AddRange(shapedRun.ShapedBuffer.IsLeftToRight ? runClusters : runClusters.Reverse());
			}

			var nextCharacterHit = new CharacterHit(0, clusters[1] - clusters[0]);

			foreach (var cluster in clusters)
			{
				CornerstoneTest.AreEqual(cluster, nextCharacterHit.FirstCharacterIndex);

				nextCharacterHit = textLine.GetNextCaretCharacterHit(nextCharacterHit);
			}

			var lastCharacterHit = nextCharacterHit;

			nextCharacterHit = textLine.GetNextCaretCharacterHit(lastCharacterHit);

			CornerstoneTest.AreEqual(lastCharacterHit.FirstCharacterIndex, nextCharacterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(lastCharacterHit.TrailingLength, nextCharacterHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetNextCaretCharacterHitFromMixedTextBuffer()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new MixedTextBufferTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(9, 1));

			CornerstoneTest.AreEqual(10, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(1, characterHit.TrailingLength);

			characterHit = textLine.GetNextCaretCharacterHit(characterHit);

			CornerstoneTest.AreEqual(11, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(1, characterHit.TrailingLength);

			characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(19, 1));

			CornerstoneTest.AreEqual(20, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(1, characterHit.TrailingLength);

			characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(10));

			CornerstoneTest.AreEqual(11, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);

			characterHit = textLine.GetNextCaretCharacterHit(characterHit);

			CornerstoneTest.AreEqual(12, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);

			characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(20));

			CornerstoneTest.AreEqual(21, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetNextCharacterHitForDrawableRuns()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new DrawableRunTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(4, textLine.TextRuns.Count);

			var currentHit = textLine.GetNextCaretCharacterHit(new CharacterHit(0));

			CornerstoneTest.AreEqual(1, currentHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, currentHit.TrailingLength);

			currentHit = textLine.GetNextCaretCharacterHit(currentHit);

			CornerstoneTest.AreEqual(2, currentHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, currentHit.TrailingLength);

			currentHit = textLine.GetNextCaretCharacterHit(currentHit);

			CornerstoneTest.AreEqual(3, currentHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, currentHit.TrailingLength);

			currentHit = textLine.GetNextCaretCharacterHit(currentHit);

			CornerstoneTest.AreEqual(4, currentHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, currentHit.TrailingLength);
		}
	}

	[DataRow("𐐷𐐷𐐷𐐷𐐷")]
	[DataRow("01234567🎉\n")]
	[DataRow("𐐷1234")]
	[PresentationTestMethod]
	public void ShouldGetPreviousCaretCharacterHit(string text)
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

			var clusters = textLine.TextRuns
				.Cast<ShapedTextRun>()
				.SelectMany(x => x.ShapedBuffer, (run, glyph) => glyph.GlyphCluster + TextTestHelper.GetStartCharIndex(run.Text))
				.ToArray();

			var previousCharacterHit = new CharacterHit(text.Length);

			for (var i = clusters.Length - 1; i >= 0; i--)
			{
				previousCharacterHit = textLine.GetPreviousCaretCharacterHit(previousCharacterHit);

				CornerstoneTest.AreEqual(clusters[i], previousCharacterHit.FirstCharacterIndex + previousCharacterHit.TrailingLength);
			}

			var firstCharacterHit = previousCharacterHit;

			previousCharacterHit = textLine.GetPreviousCaretCharacterHit(firstCharacterHit);

			CornerstoneTest.AreEqual(firstCharacterHit.FirstCharacterIndex, previousCharacterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, previousCharacterHit.TrailingLength);

			previousCharacterHit = new CharacterHit(clusters[^1], text.Length - clusters[^1]);

			for (var i = clusters.Length - 1; i > 0; i--)
			{
				previousCharacterHit = textLine.GetPreviousCaretCharacterHit(previousCharacterHit);

				CornerstoneTest.AreEqual(clusters[i], previousCharacterHit.FirstCharacterIndex + previousCharacterHit.TrailingLength);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetPreviousCaretCharacterHitBidi()
	{
		const string text = "אבג 1 ABC";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var clusters = new List<int>();

			foreach (var textRun in textLine.TextRuns.OrderBy(x => TextTestHelper.GetStartCharIndex(x.Text)))
			{
				var shapedRun = (ShapedTextRun) textRun;
				var runOffset = TextTestHelper.GetStartCharIndex(shapedRun.Text);

				var runClusters = shapedRun.ShapedBuffer.Select(glyph => glyph.GlyphCluster + runOffset);

				clusters.AddRange(shapedRun.ShapedBuffer.IsLeftToRight ? runClusters : runClusters.Reverse());
			}

			clusters.Reverse();

			var nextCharacterHit = new CharacterHit(text.Length - 1);

			foreach (var cluster in clusters)
			{
				var currentCaretIndex = nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength;

				CornerstoneTest.AreEqual(cluster, currentCaretIndex);

				nextCharacterHit = textLine.GetPreviousCaretCharacterHit(nextCharacterHit);
			}

			var lastCharacterHit = nextCharacterHit;

			nextCharacterHit = textLine.GetPreviousCaretCharacterHit(lastCharacterHit);

			CornerstoneTest.AreEqual(lastCharacterHit.FirstCharacterIndex, nextCharacterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(lastCharacterHit.TrailingLength, nextCharacterHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetPreviousCaretCharacterHitFromMixedTextBuffer()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new MixedTextBufferTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(20, 1));

			CornerstoneTest.AreEqual(20, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);

			characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(10, 1));

			CornerstoneTest.AreEqual(10, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);

			characterHit = textLine.GetPreviousCaretCharacterHit(characterHit);

			CornerstoneTest.AreEqual(9, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);

			characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(21));

			CornerstoneTest.AreEqual(20, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);

			characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(11));

			CornerstoneTest.AreEqual(10, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);

			characterHit = textLine.GetPreviousCaretCharacterHit(characterHit);

			CornerstoneTest.AreEqual(9, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(0, characterHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetPreviousCharacterHitForDrawableRuns()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new DrawableRunTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(4, textLine.TextRuns.Count);

			var currentHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(3, 1));

			CornerstoneTest.AreEqual(3, currentHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, currentHit.TrailingLength);

			currentHit = textLine.GetPreviousCaretCharacterHit(currentHit);

			CornerstoneTest.AreEqual(2, currentHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, currentHit.TrailingLength);

			currentHit = textLine.GetPreviousCaretCharacterHit(currentHit);

			CornerstoneTest.AreEqual(1, currentHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, currentHit.TrailingLength);

			currentHit = textLine.GetPreviousCaretCharacterHit(currentHit);

			CornerstoneTest.AreEqual(0, currentHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(0, currentHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetPreviousCharacterHitNonTrailing()
	{
		var text = "123.45.67.•";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new SingleBufferTextSource(text, defaultProperties, true);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(10, 1));
		}
	}

	[PresentationTestMethod]
	public void ShouldGetRunBounds()
	{
		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Manrope"));
			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new CustomTextBufferTextSource(
				new TextCharacters("He", defaultProperties),
				new TextCharacters("Wo", defaultProperties),
				new TextCharacters("ff", defaultProperties));

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(1, 1);

			CornerstoneTest.NotEmpty(textBounds);

			textBounds = textLine.GetTextBounds(2, 1);

			CornerstoneTest.NotEmpty(textBounds);

			textBounds = textLine.GetTextBounds(4, 1);

			CornerstoneTest.NotEmpty(textBounds);
		}
	}

	[Win32TestMethod("Windows font")]
	public void ShouldGetTextBoundsAfterLastIndex()
	{
		using (Start())
		{
			var typeface = new Typeface("Segoe UI Emoji");

			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new CustomTextBufferTextSource(new TextCharacters("🙈", defaultProperties));
			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(2, 1);

			CornerstoneTest.NotEmpty(textBounds);

			var firstBounds = textBounds[0];

			CornerstoneTest.AreEqual(textLine.Width, firstBounds.Rectangle.Right);

			CornerstoneTest.IsNotNull(firstBounds.TextRunBounds);

			CornerstoneTest.Empty(firstBounds.TextRunBounds);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsBiDiLeftToRight()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var text = "אאא AAA";
			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, 200,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(0, 3);

			var firstRun = CornerstoneTest.IsType<ShapedTextRun>(textLine.TextRuns[0]);

			CornerstoneTest.AreEqual(1, textBounds.Count);
			CornerstoneTest.AreEqual(firstRun.Size.Width, textBounds.Sum(x => x.Rectangle.Width));

			textBounds = textLine.GetTextBounds(3, 4);

			var secondRun = CornerstoneTest.IsType<ShapedTextRun>(textLine.TextRuns[1]);

			CornerstoneTest.AreEqual(1, textBounds.Count);
			CornerstoneTest.AreEqual(secondRun.Size.Width, textBounds.Sum(x => x.Rectangle.Width));

			textBounds = textLine.GetTextBounds(0, 4);

			CornerstoneTest.AreEqual(2, textBounds.Count);

			CornerstoneTest.AreEqual(firstRun.Size.Width, textBounds[0].Rectangle.Width);

			CornerstoneTest.AreEqual(7.201171875, textBounds[1].Rectangle.Width);

			CornerstoneTest.AreEqual(firstRun.Size.Width, textBounds[1].Rectangle.Left);

			textBounds = textLine.GetTextBounds(0, text.Length);

			CornerstoneTest.AreEqual(2, textBounds.Count);
			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, textBounds.Sum(x => x.Rectangle.Width));
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsBiDiRightToLeft()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var text = "אאא AAA";
			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, 200,
					new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			// Runs come in visual order: the Latin word sits leftmost, then the space, then the
			// Hebrew word. The space belongs to the primary font, so it is a run of its own.
			var latinRun = CornerstoneTest.IsType<ShapedTextRun>(textLine.TextRuns[0]);
			var spaceRun = CornerstoneTest.IsType<ShapedTextRun>(textLine.TextRuns[1]);
			var hebrewRun = CornerstoneTest.IsType<ShapedTextRun>(textLine.TextRuns[2]);

			var hebrewAndSpaceWidth = hebrewRun.Size.Width + spaceRun.Size.Width;

			var textBounds = textLine.GetTextBounds(0, 4);

			CornerstoneTest.AreEqual(1, textBounds.Count);
			CornerstoneTest.AreEqual(hebrewAndSpaceWidth, textBounds.Sum(x => x.Rectangle.Width));

			textBounds = textLine.GetTextBounds(4, 3);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			CornerstoneTest.AreEqual(3, textBounds[0].TextRunBounds.Sum(x => x.Length));
			CornerstoneTest.AreEqual(latinRun.Size.Width, textBounds.Sum(x => x.Rectangle.Width));

			textBounds = textLine.GetTextBounds(0, 5);

			CornerstoneTest.AreEqual(2, textBounds.Count);
			CornerstoneTest.AreEqual(5, textBounds.Sum(x => x.TextRunBounds.Sum(x => x.Length)));

			CornerstoneTest.AreEqual(hebrewAndSpaceWidth, textBounds[1].Rectangle.Width);
			CornerstoneTest.AreEqual(7.201171875, textBounds[0].Rectangle.Width);

			CornerstoneTest.AreEqual(textLine.Start + 7.201171875, textBounds[0].Rectangle.Right, 2);
			CornerstoneTest.AreEqual(textLine.Start + latinRun.Size.Width, textBounds[1].Rectangle.Left, 2);

			textBounds = textLine.GetTextBounds(0, text.Length);

			CornerstoneTest.AreEqual(2, textBounds.Count);
			CornerstoneTest.AreEqual(7, textBounds.Sum(x => x.TextRunBounds.Sum(x => x.Length)));
			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, textBounds.Sum(x => x.Rectangle.Width), 2);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsBidi()
	{
		var text = "אבגדה 12345 ABCDEF אבגדה";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new SingleBufferTextSource(text, defaultProperties, true);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var bounds = textLine.GetTextBounds(6, 1);

			CornerstoneTest.AreEqual(1, bounds.Count);

			CornerstoneTest.AreEqual(0, bounds[0].Rectangle.Left);

			bounds = textLine.GetTextBounds(5, 1);

			CornerstoneTest.AreEqual(1, bounds.Count);

			// Layout bounds depend on the order in which floating-point glyph
			// advances are summed inside ShapedBuffer; that order changed when
			// the cluster-width cache landed, so this value moved by one ULP
			// (36.005859374999993 → 36.005859375). Both round to the same
			// sub-pixel position; use a tolerant compare to capture intent.
			CornerstoneTest.AreEqual(36.005859375, bounds[0].Rectangle.Left, 5);

			bounds = textLine.GetTextBounds(0, 1);

			CornerstoneTest.AreEqual(1, bounds.Count);

			// The space between the Hebrew word and the digits is drawn with the primary font
			// rather than the Hebrew fallback, which is 4.08 wider at this size, so everything
			// laid out after it sits that much further right.
			CornerstoneTest.AreEqual(75.247031249999992, bounds[0].Rectangle.Right);

			bounds = textLine.GetTextBounds(11, 1);

			CornerstoneTest.AreEqual(1, bounds.Count);

			CornerstoneTest.AreEqual(75.247031249999992, bounds[0].Rectangle.Left);

			bounds = textLine.GetTextBounds(0, 25);

			CornerstoneTest.AreEqual(4, bounds.Count);

			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, bounds.Last().Rectangle.Right);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsBidi2()
	{
		var text = "אבג ABC אבג 123";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new SingleBufferTextSource(text, defaultProperties, true);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var bounds = textLine.GetTextBounds(0, text.Length);

			CornerstoneTest.AreEqual(4, bounds.Count);

			var right = bounds.Last().Rectangle.Right;

			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, right);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsForClusteredZeroWidthCharacters()
	{
		const string text = "\r\n";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new TextFormatterTests.ListTextSource(new TextHidden(1), new TextCharacters(text, defaultProperties));

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(2, 1);

			CornerstoneTest.NotEmpty(textBounds);

			var firstBounds = textBounds[0];

			CornerstoneTest.NotEmpty(firstBounds.TextRunBounds);

			var firstRunBounds = firstBounds.TextRunBounds[0];

			CornerstoneTest.AreEqual(2, firstRunBounds.TextSourceCharacterIndex);

			CornerstoneTest.AreEqual(1, firstRunBounds.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsForExceedingTextLength()
	{
		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new CustomTextBufferTextSource(new TextCharacters("1234", defaultProperties));
			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(10, 1);

			CornerstoneTest.IsNotNull(textBounds);

			CornerstoneTest.NotEmpty(textBounds);

			var firstBounds = textBounds[0];

			CornerstoneTest.Empty(firstBounds.TextRunBounds);

			CornerstoneTest.AreEqual(0, firstBounds.Rectangle.Width);

			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, firstBounds.Rectangle.Right);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsForLineBreak()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new SingleBufferTextSource(Environment.NewLine, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(0, Environment.NewLine.Length);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			CornerstoneTest.AreEqual(1, textBounds[0].TextRunBounds.Count);

			CornerstoneTest.AreEqual(Environment.NewLine.Length, textBounds[0].TextRunBounds[0].Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsForMixedHiddenRuns()
	{
		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Manrope"));

			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new CustomTextBufferTextSource(
				new TextHidden(1),
				new TextCharacters("Authenti", defaultProperties),
				new TextHidden(1),
				new TextHidden(1),
				new TextEndOfParagraph(1));

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(8, 1);

			CornerstoneTest.NotEmpty(textBounds);

			var firstBounds = textBounds[0];

			CornerstoneTest.IsNotNull(firstBounds.TextRunBounds);
			CornerstoneTest.NotEmpty(firstBounds.TextRunBounds);

			var firstRun = firstBounds.TextRunBounds[0];

			CornerstoneTest.IsNotNull(firstRun);

			CornerstoneTest.AreEqual(8, firstRun.TextSourceCharacterIndex);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsForMixedHiddenRunsWithLigature()
	{
		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Manrope"));

			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new CustomTextBufferTextSource(
				new TextHidden(1),
				new TextCharacters("Authenti", defaultProperties),
				new TextHidden(1),
				new TextHidden(1),
				new TextCharacters("ff", defaultProperties),
				new TextHidden(1),
				new TextHidden(1));

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(12, 1);

			CornerstoneTest.NotEmpty(textBounds);

			var firstBounds = textBounds[0];

			CornerstoneTest.IsNotNull(firstBounds.TextRunBounds);
			CornerstoneTest.NotEmpty(firstBounds.TextRunBounds);

			var firstRun = firstBounds.TextRunBounds[0];

			CornerstoneTest.IsNotNull(firstRun);

			CornerstoneTest.AreEqual(12, firstRun.TextSourceCharacterIndex);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsForMultipleTextRuns()
	{
		var text = "Test👩🏽‍🚒";

		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface, 12);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var result = textLine.GetTextBounds(0, 11);

			CornerstoneTest.AreEqual(1, result.Count);

			var firstBounds = result[0];

			CornerstoneTest.NotEmpty(firstBounds.TextRunBounds);

			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, firstBounds.Rectangle.Width, 2);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsForNegativeTextLength()
	{
		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new CustomTextBufferTextSource(new TextCharacters("1234", defaultProperties));
			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(0, -1);

			CornerstoneTest.IsNotNull(textBounds);

			CornerstoneTest.NotEmpty(textBounds);

			var firstBounds = textBounds[0];

			CornerstoneTest.Empty(firstBounds.TextRunBounds);

			CornerstoneTest.AreEqual(0, firstBounds.Rectangle.Width);

			CornerstoneTest.AreEqual(0, firstBounds.Rectangle.Left);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsFromMixedTextBuffer()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new MixedTextBufferTextSource();

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(0, 10);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			CornerstoneTest.AreEqual(72.01171875, textBounds[0].Rectangle.Width);

			textBounds = textLine.GetTextBounds(0, 20);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			CornerstoneTest.AreEqual(144.0234375, textBounds.Sum(x => x.Rectangle.Width));

			textBounds = textLine.GetTextBounds(0, 30);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			CornerstoneTest.AreEqual(216.03515625, textBounds.Sum(x => x.Rectangle.Width));

			textBounds = textLine.GetTextBounds(0, 40);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, textBounds.Sum(x => x.Rectangle.Width));
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsMixed()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var text = "0123";
			var shaperOption = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, 0, CultureInfo.CurrentCulture);

			var firstRun = new ShapedTextRun(TextShaper.Current.ShapeText(text, shaperOption), defaultProperties);

			var textRuns = new List<TextRun>
			{
				new CustomDrawableRun(),
				firstRun,
				new CustomDrawableRun(),
				new ShapedTextRun(TextShaper.Current.ShapeText(text, shaperOption), defaultProperties),
				new CustomDrawableRun(),
				new ShapedTextRun(TextShaper.Current.ShapeText(text, shaperOption), defaultProperties)
			};

			var textSource = new FixedRunsTextSource(textRuns);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(0, textLine.Length);

			CornerstoneTest.AreEqual(1, textBounds.Count);
			CornerstoneTest.AreEqual(textLine.WidthIncludingTrailingWhitespace, textBounds.Sum(x => x.Rectangle.Width));

			textBounds = textLine.GetTextBounds(0, 1);

			CornerstoneTest.AreEqual(1, textBounds.Count);
			CornerstoneTest.AreEqual(14, textBounds[0].Rectangle.Width);

			textBounds = textLine.GetTextBounds(0, firstRun.Length + 1);

			CornerstoneTest.AreEqual(1, textBounds.Count);
			CornerstoneTest.AreEqual(firstRun.Size.Width + 14, textBounds.Sum(x => x.Rectangle.Width));

			textBounds = textLine.GetTextBounds(1, firstRun.Length);

			CornerstoneTest.AreEqual(1, textBounds.Count);
			CornerstoneTest.AreEqual(firstRun.Size.Width, textBounds[0].Rectangle.Width);

			textBounds = textLine.GetTextBounds(0, 1 + firstRun.Length);

			CornerstoneTest.AreEqual(1, textBounds.Count);
			CornerstoneTest.AreEqual(firstRun.Size.Width + 14, textBounds.Sum(x => x.Rectangle.Width));
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsNotInfiniteLoop()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var shaperOption = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, 0, CultureInfo.CurrentCulture);
			var shaperOption2 = new TextShaperOptions(Typeface.Default.GlyphTypeface, 11, 0, CultureInfo.CurrentCulture);

			var textRuns = new List<TextRun>
			{
				new ShapedTextRun(TextShaper.Current.ShapeText("قرأ ", shaperOption), defaultProperties),
				new ShapedTextRun(TextShaper.Current.ShapeText("Wikipedia\u2122", shaperOption), defaultProperties),
				new ShapedTextRun(TextShaper.Current.ShapeText("\u200e ", shaperOption2), defaultProperties),
				new ShapedTextRun(TextShaper.Current.ShapeText("طوال اليوم", shaperOption), defaultProperties),
				new ShapedTextRun(TextShaper.Current.ShapeText(".", shaperOption), defaultProperties)
			};

			var textSource = new FixedRunsTextSource(textRuns);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			textLine.GetTextBounds(4, 11);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsTamil()
	{
		var text = "எடுத்துக்காட்டு வழி வினவல்";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new SingleBufferTextSource(text, defaultProperties, true);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.NotEmpty(textLine.TextRuns);

			var firstRun = textLine.TextRuns[0] as ShapedTextRun;

			CornerstoneTest.IsNotNull(firstRun);

			var clusterWidth = new List<double>();
			var distances = new List<double>();
			var clusters = new List<int>();
			var lastCluster = -1;
			var currentDistance = 0.0;
			var currentAdvance = 0.0;

			foreach (var glyphInfo in firstRun.ShapedBuffer)
			{
				if (lastCluster != glyphInfo.GlyphCluster)
				{
					clusterWidth.Add(currentAdvance);
					distances.Add(currentDistance);
					clusters.Add(glyphInfo.GlyphCluster);

					currentAdvance = 0;
				}

				lastCluster = glyphInfo.GlyphCluster;
				currentDistance += glyphInfo.GlyphAdvance;
				currentAdvance += glyphInfo.GlyphAdvance;
			}

			clusterWidth.RemoveAt(0);

			clusterWidth.Add(currentAdvance);

			for (var i = 6; i < clusters.Count; i++)
			{
				var cluster = clusters[i];
				var expectedDistance = distances[i];
				var expectedWidth = clusterWidth[i];

				var actualDistance = textLine.GetDistanceFromCharacterHit(new CharacterHit(cluster));

				CornerstoneTest.AreEqual(expectedDistance, actualDistance, 2);

				var characterHit = textLine.GetCharacterHitFromDistance(expectedDistance);

				var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

				CornerstoneTest.AreEqual(cluster, textPosition);

				var bounds = textLine.GetTextBounds(cluster, 1);

				CornerstoneTest.IsNotNull(bounds);
				CornerstoneTest.NotEmpty(bounds);

				var firstBounds = bounds[0];

				CornerstoneTest.NotEmpty(firstBounds.TextRunBounds);

				var firstRunBounds = firstBounds.TextRunBounds[0];

				CornerstoneTest.AreEqual(cluster, firstRunBounds.TextSourceCharacterIndex);

				var width = firstRunBounds.Rectangle.Width;

				CornerstoneTest.AreEqual(expectedWidth, width, 2);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsTrailingZeroWidth()
	{
		var text = "dasdsad\r\n";

		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);
			var shaperOption = new TextShaperOptions(typeface.GlyphTypeface);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(7, 3);

			CornerstoneTest.NotEmpty(textBounds);

			var firstBounds = textBounds[0];

			CornerstoneTest.NotEmpty(firstBounds.TextRunBounds);

			var firstRunBounds = firstBounds.TextRunBounds[0];

			CornerstoneTest.AreEqual(7, firstRunBounds.TextSourceCharacterIndex);

			CornerstoneTest.AreEqual(2, firstRunBounds.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsWithEndOfParagraph()
	{
		var text = "abc";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new SingleBufferTextSource(text, defaultProperties, true);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(3, 1);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			var firstBounds = textBounds.First();

			CornerstoneTest.IsTrue(firstBounds.TextRunBounds.Count > 0);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsWithEndOfParagraphRightToLeft()
	{
		var text = "لوحة المفاتيح العربية";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new SingleBufferTextSource(text, defaultProperties, true);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(0, 1);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			var firstBounds = textBounds.First();

			CornerstoneTest.IsTrue(firstBounds.TextRunBounds.Count > 0);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsWithGlue()
	{
		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);
			var text = "a\u202C\u202C\u202C\u202Cb";

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(1, 1);

			CornerstoneTest.NotEmpty(textBounds);

			var firstTextBounds = textBounds[0];

			CornerstoneTest.NotEmpty(firstTextBounds.TextRunBounds);

			var firstRunBounds = firstTextBounds.TextRunBounds[0];

			CornerstoneTest.AreEqual(1, firstRunBounds.TextSourceCharacterIndex);
			CornerstoneTest.AreEqual(1, firstRunBounds.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsWithMixedRunsWithinCluster()
	{
		using (Start())
		{
			const string manropeFont = "resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Manrope";

			var typeface = new Typeface(manropeFont);

			var defaultProperties = new GenericTextRunProperties(typeface);
			var text = "Fotografin";
			var shaperOption = new TextShaperOptions(typeface.GlyphTypeface);

			var firstRun = new ShapedTextRun(TextShaper.Current.ShapeText(text, shaperOption), defaultProperties);

			var textRuns = new List<TextRun>
			{
				new CustomDrawableRun(),
				new CustomDrawableRun(),
				firstRun,
				new CustomDrawableRun()
			};

			var textSource = new FixedRunsTextSource(textRuns);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(10, 1);

			CornerstoneTest.AreEqual(1, textBounds.Count);

			var firstBounds = textBounds[0];

			CornerstoneTest.NotEmpty(firstBounds.TextRunBounds);

			var firstRunBounds = firstBounds.TextRunBounds[0];

			CornerstoneTest.AreEqual(1, firstRunBounds.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsWithTrailingZeroAdvance()
	{
		const string df7Font = "resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#DF7segHMI";

		using (Start())
		{
			var typeface = new Typeface(df7Font);
			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new SingleBufferTextSource("3,47-=?:#", defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(0, 2);

			CornerstoneTest.NotEmpty(textBounds);

			var textRunBounds = textBounds.First().TextRunBounds;

			CornerstoneTest.NotEmpty(textBounds);

			var first = textRunBounds.First();

			CornerstoneTest.AreEqual(0, first.TextSourceCharacterIndex);
			CornerstoneTest.AreEqual(2, first.Length);
		}
	}

	[Win32TestMethod("Windows font")]
	public void ShouldGetTextBoundsWithinCluster()
	{
		using (Start())
		{
			var typeface = new Typeface("Segoe UI Emoji");

			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new CustomTextBufferTextSource(new TextCharacters("🙈", defaultProperties));
			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textBounds = textLine.GetTextBounds(0, 1);

			CornerstoneTest.NotEmpty(textBounds);

			var runBounds = textBounds[0].TextRunBounds[0];

			CornerstoneTest.AreEqual(0, runBounds.TextSourceCharacterIndex);

			textBounds = textLine.GetTextBounds(1, 1);

			CornerstoneTest.NotEmpty(textBounds);

			runBounds = textBounds[0].TextRunBounds[0];

			CornerstoneTest.AreEqual(1, runBounds.TextSourceCharacterIndex);

			textBounds = textLine.GetTextBounds(2, 1);

			CornerstoneTest.NotEmpty(textBounds);

			CornerstoneTest.IsNotNull(textBounds[0].TextRunBounds);

			CornerstoneTest.Empty(textBounds[0].TextRunBounds);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextBoundsWithinCluster2()
	{
		var text = "Test👩🏽‍🚒";

		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface, 12);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			var textPosition = 0;

			while (textPosition < text.Length)
			{
				var bounds = textLine.GetTextBounds(textPosition, 1);

				CornerstoneTest.AreEqual(1, bounds.Count);

				var firstBounds = bounds[0];

				CornerstoneTest.AreEqual(1, firstBounds.TextRunBounds.Count);

				var firstRunBounds = firstBounds.TextRunBounds[0];

				CornerstoneTest.AreEqual(textPosition, firstRunBounds.TextSourceCharacterIndex);

				var expectedDistance = firstRunBounds.Rectangle.Left;

				var characterHit = new CharacterHit(textPosition);

				var distance = textLine.GetDistanceFromCharacterHit(characterHit);

				CornerstoneTest.AreEqual(expectedDistance, distance, 2);

				var nextCharacterHit = textLine.GetNextCaretCharacterHit(characterHit);

				var expectedNextPosition = textPosition + firstRunBounds.Length;

				var nextPosition = nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength;

				CornerstoneTest.AreEqual(expectedNextPosition, nextPosition);

				var previousCharacterHit = textLine.GetPreviousCaretCharacterHit(nextCharacterHit);

				CornerstoneTest.AreEqual(characterHit, previousCharacterHit);

				textPosition += firstRunBounds.Length;
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTextRange()
	{
		var text = "שדגככעיחדגכAישדגשדגחייטYDASYWIWחיחלדשSAטויליHUHIUHUIDWKLאא'ק'קחליק/'וקןגגגלךשף'/קפוכדגכשדגשיח'/קטאגשד";

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var textRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

			var lineWidth = textLine.WidthIncludingTrailingWhitespace;

			var textBounds = textLine.GetTextBounds(0, text.Length);

			TextBounds lastBounds = null;

			var runBounds = textBounds.SelectMany(x => x.TextRunBounds).ToList();

			CornerstoneTest.AreEqual(textRuns.Count, runBounds.Count);

			for (var i = 0; i < textRuns.Count; i++)
			{
				var run = textRuns[i];
				var bounds = runBounds[i];

				CornerstoneTest.AreEqual(TextTestHelper.GetStartCharIndex(run.Text), bounds.TextSourceCharacterIndex);
				CornerstoneTest.AreEqual(run, bounds.TextRun);
				CornerstoneTest.AreEqual(run.Size.Width, bounds.Rectangle.Width, 2);
			}

			for (var i = 0; i < textBounds.Count; i++)
			{
				var currentBounds = textBounds[i];

				if (lastBounds != null)
				{
					CornerstoneTest.AreEqual(lastBounds.Rectangle.Right, currentBounds.Rectangle.Left, 2);
				}

				var sumOfRunWidth = currentBounds.TextRunBounds.Sum(x => x.Rectangle.Width);

				CornerstoneTest.AreEqual(sumOfRunWidth, currentBounds.Rectangle.Width, 2);

				lastBounds = currentBounds;
			}

			var sumOfBoundsWidth = textBounds.Sum(x => x.Rectangle.Width);

			CornerstoneTest.AreEqual(lineWidth, sumOfBoundsWidth, 2);
		}
	}

	[PresentationTestMethod]
	public void ShouldHandleNewLineInRTLText()
	{
		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource("test\r\n", defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Right,
						true, true, defaultProperties, TextWrapping.Wrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreNotEqual(textLine.NewLineLength, 0);
		}
	}

	[PresentationTestMethod]
	[DataRow("\0", 0.0)]
	[DataRow("\0\0\0", 0.0)]
	[DataRow("\0A\0\0", 7.201171875)]
	[DataRow("\0AA\0AA\0", 28.8046875)]
	public void ShouldIgnoreNullTerminator(string text, double width)
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var textSource = new SingleBufferTextSource(text, defaultProperties, true);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(width, textLine.Width);
		}
	}

	[DataRow("y", -8, -1.304, -5.44)]
	[DataRow("f", -12, -11.824, -4.44)]
	[DataRow("a", 1, -0.232, -20.44)]
	[Win32TestMethod("Values depend on the Skia platform backend")]
	public void ShouldProduceOverhang(string text, double leading, double trailing, double after)
	{
		const string symbolsFont = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Source Serif";

		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse(symbolsFont));

			var defaultProperties = new GenericTextRunProperties(typeface, 64);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
						true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);

			CornerstoneTest.AreEqual(leading, textLine.OverhangLeading, 2);
			CornerstoneTest.AreEqual(trailing, textLine.OverhangTrailing, 2);
			CornerstoneTest.AreEqual(after, textLine.OverhangAfter, 2);
		}
	}

	[PresentationTestMethod]
	[DataRow("hello\r\nworld")]
	[DataRow("مرحباً\r\nبالعالم")]
	[DataRow("hello مرحباً\r\nworld بالعالم")]
	[DataRow("مرحباً hello\r\nبالعالم nworld")]
	public void ShouldSetNewLineLengthForCRLFInRTLText(string text)
	{
		using (Start())
		{
			var typeface = Typeface.Default;
			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Right,
						true, true, defaultProperties, TextWrapping.Wrap, 0, 0, 0));

			CornerstoneTest.IsNotNull(textLine);
			CornerstoneTest.AreNotEqual(0, textLine.NewLineLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldThrowArgumentOutOfRangeExceptionForZeroTextLength()
	{
		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);
			var textSource = new CustomTextBufferTextSource(new TextCharacters("1234", defaultProperties));
			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			Assert.Throws<ArgumentOutOfRangeException>(() => textLine.GetTextBounds(0, 0));
		}
	}

	[PresentationTestMethod]
	public void ShouldTrimPathAtTheEnd()
	{
		var text = "verylongdirectory\\file.txt";

		using (Start())
		{
			var typeface = Typeface.Default;

			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource(text, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var trimming = new TextPathSegmentTrimming("*");

			var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(40, defaultProperties, FlowDirection.LeftToRight));

			var collapsedLine = textLine.Collapse(collapsingProperties);

			CornerstoneTest.IsNotNull(collapsedLine);

			var result = ExtractTextFromRuns(collapsedLine);

			CornerstoneTest.AreEqual("*.txt", result);
		}
	}

	[PresentationTestMethod]
	[DataRow("somedirectory\\")]
	[DataRow("somedirectory/")]
	public void TruncatePathPathEndingWithSlashReturnsNonEmpty(string path)
	{
		var typeface = Typeface.Default;

		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(typeface);

			var textSource = new SingleBufferTextSource(path, defaultProperties);

			var formatter = new TextFormatterImpl();

			var textLine =
				formatter.FormatLine(textSource, 0, double.PositiveInfinity,
					new GenericTextParagraphProperties(defaultProperties));

			CornerstoneTest.IsNotNull(textLine);

			var trimming = new TextPathSegmentTrimming("*");

			var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(50, defaultProperties, FlowDirection.LeftToRight));

			var collapsedLine = textLine.Collapse(collapsingProperties);

			CornerstoneTest.IsNotNull(collapsedLine);

			var result = ExtractTextFromRuns(collapsedLine);

			CornerstoneTest.IsTrue(result.Contains("ory"));
		}
	}

	private static List<int> BuildGlyphClusters(TextLine textLine)
	{
		var glyphClusters = new List<int>();

		var shapedTextRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

		var lastCluster = -1;

		foreach (var textRun in shapedTextRuns)
		{
			var runOffset = TextTestHelper.GetStartCharIndex(textRun.Text);
			var shapedBuffer = textRun.ShapedBuffer;

			var currentClusters = shapedBuffer.Select(glyph => glyph.GlyphCluster + runOffset).ToList();

			foreach (var currentCluster in currentClusters)
			{
				if (lastCluster == currentCluster)
				{
					continue;
				}

				glyphClusters.Add(currentCluster);

				lastCluster = currentCluster;
			}
		}

		return glyphClusters;
	}

	private static List<Rect> BuildRects(TextLine textLine)
	{
		var rects = new List<Rect>();
		var height = textLine.Height;

		var currentX = 0d;

		var lastCluster = -1;

		var shapedTextRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

		foreach (var textRun in shapedTextRuns)
		{
			// Glyph clusters are relative to the run's own text, so they only line up across a
			// multi-run line once the run's start is added - same as BuildGlyphClusters.
			var runOffset = TextTestHelper.GetStartCharIndex(textRun.Text);
			var shapedBuffer = textRun.ShapedBuffer;

			for (var index = 0; index < shapedBuffer.Length; index++)
			{
				var currentCluster = shapedBuffer[index].GlyphCluster + runOffset;

				var advance = shapedBuffer[index].GlyphAdvance;

				if (lastCluster != currentCluster)
				{
					rects.Add(new Rect(currentX, 0, advance, height));
				}
				else
				{
					// Another glyph of the cluster that produced the last rect: widen it.
					var rect = rects[rects.Count - 1];

					rects[rects.Count - 1] = rect.WithWidth(rect.Width + advance);
				}

				currentX += advance;

				lastCluster = currentCluster;
			}
		}

		return rects;
	}

	private static bool IsRightToLeft(TextLine textLine)
	{
		return textLine.TextRuns.Cast<ShapedTextRun>().Any(x => !x.ShapedBuffer.IsLeftToRight);
	}

	private static TextTrimming ResolveTrimming(string trimmingName)
	{
		return trimmingName switch
		{
			nameof(TextTrimming.PrefixCharacterEllipsis) => TextTrimming.PrefixCharacterEllipsis,
			nameof(TextTrimming.CharacterEllipsis) => TextTrimming.CharacterEllipsis,
			nameof(TextTrimming.WordEllipsis) => TextTrimming.WordEllipsis,
			_ => throw new ArgumentOutOfRangeException(nameof(trimmingName), trimmingName, null)
		};
	}

	private static IDisposable Start()
	{
		var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
			.With(renderInterface: new PlatformRenderInterface(null),
				fontManagerImpl: new CustomFontManagerImpl()));

		return disposable;
	}

	#endregion

	#region Classes

	private class CustomDrawableRun : DrawableTextRun
	{
		#region Properties

		public override double Baseline => 14;
		public override Size Size => new(14, 14);

		#endregion

		#region Methods

		public override void Draw(DrawingContext drawingContext, Point origin)
		{
		}

		#endregion
	}

	private class CustomTextBufferTextSource : ITextSource
	{
		#region Fields

		private readonly IReadOnlyList<TextRun> _textRuns;

		#endregion

		#region Constructors

		public CustomTextBufferTextSource(params TextRun[] textRuns)
		{
			_textRuns = textRuns;
		}

		#endregion

		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			var pos = 0;

			for (var i = 0; i < _textRuns.Count; i++)
			{
				var currentRun = _textRuns[i];

				if ((pos + currentRun.Length) > textSourceIndex)
				{
					return currentRun;
				}

				pos += currentRun.Length;
			}

			return null;
		}

		#endregion
	}

	private class DrawableRunTextSource : ITextSource
	{
		#region Constants

		private const string Text = "_A_A";

		#endregion

		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			switch (textSourceIndex)
			{
				case 0:
					return new CustomDrawableRun();
				case 1:
					return new TextCharacters(Text, new GenericTextRunProperties(Typeface.Default));
				case 5:
					return new CustomDrawableRun();
				case 6:
					return new TextCharacters(Text, new GenericTextRunProperties(Typeface.Default));
				default:
					return null;
			}
		}

		#endregion
	}

	private class FixedRunsTextSource : ITextSource
	{
		#region Fields

		private readonly IReadOnlyList<TextRun> _textRuns;

		#endregion

		#region Constructors

		public FixedRunsTextSource(IReadOnlyList<TextRun> textRuns)
		{
			_textRuns = textRuns;
		}

		#endregion

		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			var currentPosition = 0;

			foreach (var textRun in _textRuns)
			{
				if (currentPosition == textSourceIndex)
				{
					return textRun;
				}

				currentPosition += textRun.Length;
			}

			return null;
		}

		#endregion
	}

	private class MixedTextBufferTextSource : ITextSource
	{
		#region Methods

		public TextRun GetTextRun(int textSourceIndex)
		{
			switch (textSourceIndex)
			{
				case 0:
					return new TextCharacters("aaaaaaaaaa", new GenericTextRunProperties(Typeface.Default));
				case 10:
					return new TextCharacters("bbbbbbbbbb", new GenericTextRunProperties(Typeface.Default));
				case 20:
					return new TextCharacters("cccccccccc", new GenericTextRunProperties(Typeface.Default));
				case 30:
					return new TextCharacters("dddddddddd", new GenericTextRunProperties(Typeface.Default));
				default:
					return null;
			}
		}

		#endregion
	}

	private class TextHidden : TextRun
	{
		#region Constructors

		public TextHidden(int length)
		{
			Length = length;
		}

		#endregion

		#region Properties

		public override int Length { get; }

		#endregion
	}

	#endregion
}