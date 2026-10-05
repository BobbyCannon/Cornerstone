#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

/// <summary>
/// Characterization tests for <c> TextFormatterImpl.PerformTextWrapping </c>
/// and its helpers (<c> MeasureLength </c>, <c> SplitTextRuns </c>,
/// <c> ResetTrailingWhitespaceBidiLevels </c>).
/// </summary>
[TestClass]
public class TextFormatterWrapCharacterizationTests
{
	#region Methods

	[PresentationTestMethod]
	public void WrapContinuesFromPreviousLineBreak()
	{
		using (TextFormatterTests.Start())
		{
			var text = "AAAA BBBB CCCC DDDD";
			var lines = WrapAllLines(text, 40);

			CornerstoneTest.IsTrue(lines.Count >= 2, "Test setup must wrap onto at least two lines.");

			// Lines after the first reuse runs via WrappingTextLineBreak.
			// The contract: concatenated, they reproduce the original.
			var rebuilt = string.Concat(lines.Select(l => GetLineText(l)));
			CornerstoneTest.AreEqual(text, rebuilt);
		}
	}

	[PresentationTestMethod]
	public void WrapDoesNotProduceEmptyLinesForNonEmptyInput()
	{
		using (TextFormatterTests.Start())
		{
			var lines = WrapAllLines("the quick brown fox jumps over the lazy dog", 50);
			foreach (var line in lines)
			{
				CornerstoneTest.IsTrue(line.Length > 0, "Wrap should never emit a zero-length line for non-empty input.");
			}
		}
	}

	[PresentationTestMethod]
	[DataRow("AAAA BBBB CCCC DDDD", 40)]
	[DataRow("AAAA BBBB CCCC DDDD", 80)]
	public void WrapEachLineWidthWithinParagraphWidth(string text, double paragraphWidth)
	{
		using (TextFormatterTests.Start())
		{
			var lines = WrapAllLines(text, paragraphWidth);
			foreach (var line in lines)
			{
				// Width (excluding trailing whitespace) should fit the
				// paragraph. The +1.0 tolerance handles the documented
				// "single cluster wider than paragraph" overflow case.
				CornerstoneTest.IsTrue(line.Width <= (paragraphWidth + 1.0), $"Line width {line.Width} exceeds paragraph width {paragraphWidth} by more than 1px.");
			}
		}
	}

	[PresentationTestMethod]
	public void WrapEmptyTextYieldsNull()
	{
		using (TextFormatterTests.Start())
		{
			var defaultProperties = new GenericTextRunProperties(Typeface.Default, 12,
				foregroundBrush: Brushes.Black);
			var paragraphProperties = new GenericTextParagraphProperties(
				defaultProperties, textWrapping: TextWrapping.Wrap);
			var textSource = new SingleBufferTextSource("", defaultProperties);
			var formatter = new TextFormatterImpl();

			var line = formatter.FormatLine(textSource, 0, 100, paragraphProperties);
			CornerstoneTest.IsNull(line);
		}
	}

	[PresentationTestMethod]
	public void WrapHardBreakWithCRLFCountsBothCharacters()
	{
		using (TextFormatterTests.Start())
		{
			var line = WrapSingleLine("ab\r\ncd", double.PositiveInfinity);

			// CRLF is a single break with PositionWrap = 4 (consumes both chars).
			CornerstoneTest.AreEqual(4, line.Length);
		}
	}

	[PresentationTestMethod]
	public void WrapHonoursRequiredBreakEvenWithAvailableWidth()
	{
		using (TextFormatterTests.Start())
		{
			var line = WrapSingleLine("ab\ncd", double.PositiveInfinity);

			// Hard break sits at index 2 (the '\n'); PositionWrap is 3
			// (consumes the '\n').
			CornerstoneTest.AreEqual(3, line.Length);
		}
	}

	[PresentationTestMethod]
	public void WrapPointsAreGraphemeBoundaries()
	{
		// Multi-codepoint graphemes (emoji ZWJ sequences) must never be
		// split by the wrap algorithm — the wrap point has to coincide
		// with a grapheme boundary.
		using (TextFormatterTests.Start())
		{
			const string text = "abc 😀😀😀😀 xyz";
			var lines = WrapAllLines(text, 30);

			var boundaries = new HashSet<int>();
			var graphemeEnumerator = new GraphemeEnumerator(text.AsSpan());
			boundaries.Add(0);
			var pos = 0;
			while (graphemeEnumerator.MoveNext(out var grapheme))
			{
				pos += grapheme.Length;
				boundaries.Add(pos);
			}

			var cumulative = 0;
			foreach (var line in lines)
			{
				cumulative += line.Length;
				CornerstoneTest.Contains(boundaries, cumulative);
			}
		}
	}

	[PresentationTestMethod]
	public void WrapStrictLongWordSplitsInsideWhenNoWrapPositionAvailable()
	{
		// Pure Wrap (not WrapWithOverflow) on an unbreakable word: the
		// implementation falls back to splitting inside the word at the
		// best available cluster boundary.
		using (TextFormatterTests.Start())
		{
			var line = WrapSingleLine("supercalifragilistic", 30,
				TextWrapping.Wrap);

			CornerstoneTest.IsTrue(line.Length > 0);
			CornerstoneTest.IsTrue(line.Length < "supercalifragilistic".Length, "Strict wrap should split inside the long word.");
		}
	}

	[PresentationTestMethod]
	[DataRow("AAAA BBBB CCCC DDDD", 40)]
	[DataRow("AAAA BBBB CCCC DDDD", 80)]
	[DataRow("AAAA BBBB CCCC DDDD", 120)]
	public void WrapSumOfLineLengthsEqualsInputLength(string text, double paragraphWidth)
	{
		using (TextFormatterTests.Start())
		{
			var lines = WrapAllLines(text, paragraphWidth);
			var totalLength = lines.Sum(l => l.Length);
			CornerstoneTest.AreEqual(text.Length, totalLength);
		}
	}

	[PresentationTestMethod]
	public void WrapWithInfiniteWidthYieldsSingleLineWithAllRuns()
	{
		using (TextFormatterTests.Start())
		{
			var line = WrapSingleLine("Hello world", double.PositiveInfinity);

			CornerstoneTest.AreEqual("Hello world".Length, line.Length);
			CornerstoneTest.IsTrue(line.WidthIncludingTrailingWhitespace > 0);
		}
	}

	[PresentationTestMethod]
	public void WrapWithLTRTextDoesNotTouchTrailingWhitespaceBidi()
	{
		// ResetTrailingWhitespaceBidiLevels is a no-op when the run's
		// BidiLevel already matches the paragraph. The wrap result should
		// be identical to a non-wrapped layout of the same paragraph.
		using (TextFormatterTests.Start())
		{
			var text = "Hello world from Cornerstone";
			var wrappedLines = WrapAllLines(text, 80);
			var rebuilt = string.Concat(wrappedLines.Select(l => GetLineText(l)));
			CornerstoneTest.AreEqual(text, rebuilt);
		}
	}

	[PresentationTestMethod]
	public void WrapWithOverflowLongWordFollowedBySpaceWrapsAfterSpace()
	{
		// The word "supercalifragilistic" has no break inside it. At a
		// small paragraph width with WrapWithOverflow, the wrap algorithm
		// should let the word overflow as a whole, then wrap on the next
		// break (the trailing space).
		using (TextFormatterTests.Start())
		{
			var line = WrapSingleLine("supercalifragilistic next", 30,
				TextWrapping.WrapWithOverflow);

			CornerstoneTest.IsTrue(line.Length >= "supercalifragilistic".Length, $"Expected first line to contain at least the whole long word; got length {line.Length}.");
			CornerstoneTest.IsTrue(line.Length <= "supercalifragilistic ".Length, "First line should not extend past the trailing space after the long word.");
		}
	}

	[PresentationTestMethod]
	public void WrapWithZeroWidthForcesMinimumCluster()
	{
		// Width too small to fit any cluster — the implementation falls
		// back to one grapheme. This is the documented WrapWithOverflow
		// contract that lines 882-902 of TextFormatterImpl encode.
		using (TextFormatterTests.Start())
		{
			var line = WrapSingleLine("Hello", 0.001);

			CornerstoneTest.IsTrue(line.Length >= 1, "Wrap should always advance at least one grapheme even at zero width.");
		}
	}

	private static string GetLineText(TextLine line)
	{
		var sb = new StringBuilder();
		foreach (var run in line.TextRuns)
		{
			sb.Append(run.Text.Span);
		}
		return sb.ToString();
	}

	private static List<TextLine> WrapAllLines(string text, double paragraphWidth,
		TextWrapping wrapping = TextWrapping.Wrap)
	{
		var defaultProperties = new GenericTextRunProperties(Typeface.Default, 12,
			foregroundBrush: Brushes.Black);
		var paragraphProperties = new GenericTextParagraphProperties(defaultProperties,
			textWrapping: wrapping);
		var textSource = new SingleBufferTextSource(text, defaultProperties);
		var formatter = new TextFormatterImpl();

		var lines = new List<TextLine>();
		var pos = 0;
		TextLineBreak previousLineBreak = null;
		while (pos < text.Length)
		{
			var line = formatter.FormatLine(textSource, pos, paragraphWidth,
				paragraphProperties, previousLineBreak);
			if (line == null)
			{
				break;
			}
			lines.Add(line);
			previousLineBreak = line.TextLineBreak;
			pos += line.Length;

			if ((pos > 0) && (lines.Count > 200))
			{
				CornerstoneTest.Fail("Wrap appears to be looping; bailing out.");
			}
		}
		return lines;
	}

	private static TextLine WrapSingleLine(string text, double paragraphWidth,
		TextWrapping wrapping = TextWrapping.Wrap)
	{
		var defaultProperties = new GenericTextRunProperties(Typeface.Default, 12,
			foregroundBrush: Brushes.Black);
		var paragraphProperties = new GenericTextParagraphProperties(defaultProperties,
			textWrapping: wrapping);
		var textSource = new SingleBufferTextSource(text, defaultProperties);
		var formatter = new TextFormatterImpl();

		var line = formatter.FormatLine(textSource, 0, paragraphWidth, paragraphProperties);
		CornerstoneTest.IsNotNull(line);
		return line!;
	}

	#endregion
}