#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

/// <summary>
/// Characterization tests for <see cref="TextCollapsingProperties" />
/// implementations, with emphasis on BiDi correctness.
/// </summary>
[TestClass]
public class TextCollapsingBidiTests
{
	#region Methods

	[PresentationTestMethod]
	public void CollapseWithMultipleShapedRunsPreservesEllipsis()
	{
		// Three independent runs via FixedRunsTextSource. Trim point lands
		// somewhere in the middle — collapse must not silently drop a run
		// or duplicate one (covers the SplitTextRuns interaction).
		using (TextFormatterTests.Start())
		{
			var props = new GenericTextRunProperties(Typeface.Default);
			var sourceRuns = new TextRun[]
			{
				new TextCharacters("AAAA", props),
				new TextCharacters("BBBB", props),
				new TextCharacters("CCCC", props)
			};
			var src = new FixedRunsTextSource(sourceRuns);
			var formatter = new TextFormatterImpl();
			var line = formatter.FormatLine(src, 0, double.PositiveInfinity,
				new GenericTextParagraphProperties(props));
			CornerstoneTest.IsNotNull(line);

			var collapsing = TrailingChar(line!.Width / 2, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var text = LogicalText(collapsed);
			CornerstoneTest.Contains(text, "…");

			// Every preserved character must come from the original source
			// text in original order — no garbage.
			var preserved = text.Replace("…", string.Empty);
			CornerstoneTest.StartsWith("AAAABBBBCCCC", preserved);
		}
	}

	[PresentationTestMethod]
	public void LeadingPrefixHonoursFlowDirectionForSymbol()
	{
		using (TextFormatterTests.Start())
		{
			const string text = "السلام عليكم ورحمة";
			var line = BuildLine(text, FlowDirection.RightToLeft);
			var collapsing = LeadingPrefix(4, line.Width / 2, FlowDirection.RightToLeft);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);

			// The ellipsis symbol's run should pick up the RTL bidi level
			// from the FlowDirection passed to the constructor. Today the
			// ctor in Collapse() hardcodes LeftToRight, so the symbol run
			// has IsLeftToRight == true.
			var ellipsisRun = collapsed.TextRuns
				.OfType<ShapedTextRun>()
				.FirstOrDefault(r => r.Text.ToString().Contains("…"));
			CornerstoneTest.IsNotNull(ellipsisRun);
			CornerstoneTest.IsFalse(ellipsisRun!.ShapedBuffer.IsLeftToRight);
		}
	}

	[PresentationTestMethod]
	public void LeadingPrefixNegativePrefixLengthThrows()
	{
		using (TextFormatterTests.Start())
		{
			var props = new GenericTextRunProperties(Typeface.Default);
			Assert.Throws<ArgumentOutOfRangeException>(() => new TextLeadingPrefixCharacterEllipsis(
				"…", -1, 100, props, FlowDirection.LeftToRight));
		}
	}

	[PresentationTestMethod]
	public void LeadingPrefixWithFullyFittingTailRunDoesNotThrow()
	{
		// Regression: on a multi-run line a logical-tail run can fit entirely
		// within the remaining suffix budget. TryMeasureCharactersBackwards then
		// returns suffixCount == run.Length, and the old code called
		// ShapedTextRun.Split(0), which throws ArgumentOutOfRangeException. The
		// long leading run forces the collapse; the short trailing run wholly
		// fits the suffix budget and exercises that boundary.
		using (TextFormatterTests.Start())
		{
			var props = new GenericTextRunProperties(Typeface.Default);
			var sourceRuns = new TextRun[]
			{
				new TextCharacters("AAAAAAAAAAAA", props), // long: forces the collapse
				new TextCharacters("B", props) // short: wholly fits the suffix budget
			};
			var src = new FixedRunsTextSource(sourceRuns);
			var formatter = new TextFormatterImpl();
			var line = formatter.FormatLine(src, 0, double.PositiveInfinity,
				new GenericTextParagraphProperties(props));
			CornerstoneTest.IsNotNull(line);

			var collapsing = LeadingPrefix(2, line!.Width * 0.7, FlowDirection.LeftToRight);

			// Previously threw ArgumentOutOfRangeException from Split(0).
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var text = LogicalText(collapsed);
			CornerstoneTest.Contains(text, "…");

			// The fully-fitting trailing run must survive in the logical-tail suffix.
			CornerstoneTest.Contains(text, "B");
		}
	}

	[PresentationTestMethod]
	public void LogicalTextRunEnumeratorWithoutIndexedRunsReturnsDistinctRuns()
	{
		using (TextFormatterTests.Start())
		{
			var props = new GenericTextRunProperties(Typeface.Default);
			var runs = new TextRun[]
			{
				new TextCharacters("AAA", props),
				new TextCharacters("BBB", props),
				new TextCharacters("CCC", props)
			};

			// Construct TextLineImpl directly and SKIP FinalizeLine so that
			// _indexedTextRuns stays null. This is exactly the branch
			// LogicalTextRunEnumerator handles incorrectly today.
			var paragraphProps = new GenericTextParagraphProperties(props);
			var line = new TextLineImpl(runs, 0, 9, double.PositiveInfinity, paragraphProps);

			var enumerator = new LogicalTextRunEnumerator(line);
			var seen = new List<TextRun>();
			while (enumerator.MoveNext(out var run))
			{
				seen.Add(run!);
			}

			CornerstoneTest.AreEqual(3, seen.Count);
			CornerstoneTest.Same(runs[0], seen[0]);
			CornerstoneTest.Same(runs[1], seen[1]);
			CornerstoneTest.Same(runs[2], seen[2]);
		}
	}

	[PresentationTestMethod]
	public void LtrPathSegmentEllipsisCollapsesMiddle()
	{
		using (TextFormatterTests.Start())
		{
			var line = BuildLine("verylongdirectory\\file.txt", FlowDirection.LeftToRight);
			var collapsing = new TextPathSegmentEllipsis(
				"…", line.Width / 2,
				new GenericTextRunProperties(Typeface.Default),
				FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var text = LogicalText(collapsed);
			CornerstoneTest.Contains(text, "…");

			// Last segment ("file.txt") should be preserved on at least
			// some prefix; we don't assert exact width because Width math
			// depends on the font.
			CornerstoneTest.Contains(text, ".txt");
		}
	}

	[PresentationTestMethod]
	public void LtrPathSegmentEllipsisMiddleCollapsePreservesFirstAndLastSegments()
	{
		using (TextFormatterTests.Start())
		{
			// 3-segment path; middle is intentionally long so collapsing
			// it alone produces a fitting result.
			const string text = "a/middlemiddlemiddlemiddlemiddlemiddlemiddlemiddlemiddlemiddle/c.txt";
			var line = BuildLine(text, FlowDirection.LeftToRight);
			var budget = line.Width * 0.3;
			var collapsing = new TextPathSegmentEllipsis(
				"…", budget,
				new GenericTextRunProperties(Typeface.Default),
				FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var logical = LogicalText(collapsed);
			CornerstoneTest.Contains(logical, "…");
			CornerstoneTest.Contains(logical, "a");
			CornerstoneTest.Contains(logical, "c.txt");
		}
	}

	[PresentationTestMethod]
	public void LtrPrefixCharacterEllipsisPreservesPrefixAndSuffix()
	{
		using (TextFormatterTests.Start())
		{
			var line = BuildLine("01234 01234 01234", FlowDirection.LeftToRight);
			var collapsing = LeadingPrefix(8, 120.0, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var text = LogicalText(collapsed);
			CornerstoneTest.StartsWith(text, "01234 01");
			CornerstoneTest.Contains(text, "…");

			// Suffix must reappear after the symbol.
			CornerstoneTest.EndsWith(text, "4 01234");
		}
	}

	[PresentationTestMethod]
	public void LtrTrailingCharacterTrimsFromEnd()
	{
		using (TextFormatterTests.Start())
		{
			var line = BuildLine("Hello world", FlowDirection.LeftToRight);
			var collapsing = TrailingChar(line.Width / 2, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var text = LogicalText(collapsed);
			CornerstoneTest.Contains(text, "…");
			CornerstoneTest.StartsWith(text, "H");
		}
	}

	[PresentationTestMethod]
	public void LtrTrailingWordTrimsOnWordBoundary()
	{
		using (TextFormatterTests.Start())
		{
			var line = BuildLine("Hello world foo", FlowDirection.LeftToRight);
			var collapsing = TrailingWord(line.Width / 2, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			CornerstoneTest.Contains(LogicalText(collapsed), "…");
		}
	}

	[PresentationTestMethod]
	public void MixedPathSegmentEllipsisPreservesLastSegment()
	{
		using (TextFormatterTests.Start())
		{
			// Mixed-bidi path: ASCII-only separators with an RTL directory
			// name embedded. Segmentation is separator-driven, so the
			// logical-tail segment ("file.txt") must survive.
			const string text = "C:\\folder\\مجلد\\file.txt";
			var line = BuildLine(text, FlowDirection.LeftToRight);
			var collapsing = new TextPathSegmentEllipsis(
				"…", line.Width / 2,
				new GenericTextRunProperties(Typeface.Default),
				FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var logical = LogicalText(collapsed);
			CornerstoneTest.Contains(logical, "…");
			CornerstoneTest.Contains(logical, "file.txt");
		}
	}

	[PresentationTestMethod]
	public void MixedPrefixCharacterEllipsisPreservesLogicalPrefixAndSuffix()
	{
		using (TextFormatterTests.Start())
		{
			const string text = "Hello مرحبا world";
			var line = BuildLine(text, FlowDirection.LeftToRight);
			var collapsing = LeadingPrefix(5, line.Width * 0.6, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var logical = LogicalText(collapsed);
			CornerstoneTest.StartsWith(logical, "Hello");
			CornerstoneTest.Contains(logical, "…");
		}
	}

	[PresentationTestMethod]
	public void MixedTrailingCharacterPreservesLogicalPrefix()
	{
		using (TextFormatterTests.Start())
		{
			const string text = "Hello مرحبا world";
			var line = BuildLine(text, FlowDirection.LeftToRight);
			var collapsing = TrailingChar(line.Width * 0.6, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			CornerstoneTest.StartsWith(LogicalText(collapsed), "Hello");
		}
	}

	[PresentationTestMethod]
	public void MixedTrailingWordPreservesLogicalPrefix()
	{
		using (TextFormatterTests.Start())
		{
			const string text = "Hello مرحبا world";
			var line = BuildLine(text, FlowDirection.LeftToRight);
			var collapsing = TrailingWord(line.Width * 0.6, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var logical = LogicalText(collapsed);
			CornerstoneTest.Contains(logical, "…");
			CornerstoneTest.StartsWith(logical, "Hello");
		}
	}

	[PresentationTestMethod]
	public void RtlPathSegmentEllipsisMiddleCollapsePreservesFirstAndLastSegments()
	{
		using (TextFormatterTests.Start())
		{
			const string text = "اول/منتصفمنتصفمنتصفمنتصفمنتصفمنتصفمنتصفمنتصف/اخر.txt";
			var line = BuildLine(text, FlowDirection.RightToLeft);
			var budget = line.Width * 0.3;
			var collapsing = new TextPathSegmentEllipsis(
				"…", budget,
				new GenericTextRunProperties(Typeface.Default),
				FlowDirection.RightToLeft);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var logical = LogicalText(collapsed);
			CornerstoneTest.Contains(logical, "…");
			CornerstoneTest.Contains(logical, "اول");
			CornerstoneTest.Contains(logical, "اخر.txt");
		}
	}

	[PresentationTestMethod]
	public void RtlPathSegmentEllipsisPreservesLastSegment()
	{
		using (TextFormatterTests.Start())
		{
			// Pure-RTL path. Cornerstone's font fallback may render Arabic as
			// .notdef glyphs in the test environment, but segmentation is
			// character-driven (separators are ASCII '/' and '\\') so the
			// logical-tail segment "ملف.txt" must still be detected and
			// preserved.
			const string text = "مجلد/مجلد2/ملف.txt";
			var line = BuildLine(text, FlowDirection.RightToLeft);
			var collapsing = new TextPathSegmentEllipsis(
				"…", line.Width / 2,
				new GenericTextRunProperties(Typeface.Default),
				FlowDirection.RightToLeft);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var logical = LogicalText(collapsed);
			CornerstoneTest.Contains(logical, "…");
			CornerstoneTest.Contains(logical, "ملف.txt");
		}
	}

	[PresentationTestMethod]
	public void RtlPrefixCharacterEllipsisPreservesLogicalPrefix()
	{
		using (TextFormatterTests.Start())
		{
			const string text = "السلام عليكم ورحمة الله وبركاته";
			var line = BuildLine(text, FlowDirection.RightToLeft);
			var collapsing = LeadingPrefix(4, line.Width / 2, FlowDirection.RightToLeft);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var logical = LogicalText(collapsed);
			CornerstoneTest.StartsWith(logical, text.Substring(0, 4));
			CornerstoneTest.Contains(logical, "…");
		}
	}

	[PresentationTestMethod]
	public void RtlTrailingCharacterPreservesLogicalPrefix()
	{
		using (TextFormatterTests.Start())
		{
			const string text = "السلام عليكم ورحمة الله وبركاته";
			var line = BuildLine(text, FlowDirection.RightToLeft);
			var collapsing = TrailingChar(line.Width / 2, FlowDirection.RightToLeft);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			var logical = LogicalText(collapsed);
			CornerstoneTest.Contains(logical, "…");
			CornerstoneTest.StartsWith(logical, text.Substring(0, 1));
		}
	}

	[PresentationTestMethod]
	public void RtlTrailingWordPreservesLogicalPrefix()
	{
		using (TextFormatterTests.Start())
		{
			const string text = "السلام عليكم ورحمة الله وبركاته";
			var line = BuildLine(text, FlowDirection.RightToLeft);
			var collapsing = TrailingWord(line.Width / 2, FlowDirection.RightToLeft);
			var collapsed = line.Collapse(collapsing);

			AssertCollapsed(collapsed, line);
			CornerstoneTest.Contains(LogicalText(collapsed), "…");
		}
	}

	[PresentationTestMethod]
	[DataRow("Hello world abcdef", true)]
	[DataRow("السلام عليكم ورحمة", false)]
	public void TryMeasureCharactersBackwardsReturnedLengthFitsLogicalTrailingInBudget(string text, bool ltr)
	{
		using (TextFormatterTests.Start())
		{
			var dir = ltr ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
			var line = BuildLine(text, dir);
			var shapedRun = line.TextRuns.OfType<ShapedTextRun>().FirstOrDefault();
			CornerstoneTest.IsNotNull(shapedRun);

			var buffer = shapedRun!.ShapedBuffer;
			var totalWidth = shapedRun.Size.Width;
			var textLength = shapedRun.Length;

			for (var i = 1; i < 10; i++)
			{
				var budget = (totalWidth * i) / 10;
				if (!shapedRun.TryMeasureCharactersBackwards(budget, out var measured, out _) || (measured <= 0))
				{
					continue;
				}

				var actualTrailingWidth = buffer.GetCharRangeWidth(textLength - measured, textLength);

				CornerstoneTest.IsTrue(actualTrailingWidth <= (budget + 0.5), $"{dir}: budget={budget:F2}, measured={measured}, " +
					$"actual logical-trailing width={actualTrailingWidth:F2}");
			}
		}
	}

	[PresentationTestMethod]
	[DataRow("Hello world abcdef", true)]
	[DataRow("السلام عليكم ورحمة", false)]
	public void TryMeasureCharactersReturnedLengthFitsLogicalLeadingInBudget(string text, bool ltr)
	{
		using (TextFormatterTests.Start())
		{
			var dir = ltr ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
			var line = BuildLine(text, dir);
			var shapedRun = line.TextRuns.OfType<ShapedTextRun>().FirstOrDefault();
			CornerstoneTest.IsNotNull(shapedRun);

			var buffer = shapedRun!.ShapedBuffer;
			var totalWidth = shapedRun.Size.Width;

			// Probe at several budget points; the contract must hold at all of them.
			for (var i = 1; i < 10; i++)
			{
				var budget = (totalWidth * i) / 10;
				if (!shapedRun.TryMeasureCharacters(budget, out var measured) || (measured <= 0))
				{
					continue;
				}

				// The width of the LOGICAL leading `measured` characters must fit
				// in `budget`. GetCharRangeWidth uses the cluster cache, which is
				// built in logical order for both directions.
				var actualLeadingWidth = buffer.GetCharRangeWidth(0, measured);

				CornerstoneTest.IsTrue(actualLeadingWidth <= (budget + 0.5), $"{dir}: budget={budget:F2}, measured={measured}, " +
					$"actual logical-leading width={actualLeadingWidth:F2}");
			}
		}
	}

	[PresentationTestMethod]
	public void WidthGreaterThanLineReturnsSameLine()
	{
		using (TextFormatterTests.Start())
		{
			var line = BuildLine("abc", FlowDirection.LeftToRight);
			var collapsing = TrailingChar(line.Width + 100, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			// Collapse returns null → TextLineImpl.Collapse returns `this`.
			CornerstoneTest.Same(line, collapsed);
			CornerstoneTest.IsFalse(collapsed.HasCollapsed);
		}
	}

	[PresentationTestMethod]
	public void WidthLessThanSymbolReturnsEmptyCollapsedLine()
	{
		using (TextFormatterTests.Start())
		{
			var line = BuildLine("abcdef", FlowDirection.LeftToRight);

			// Width below symbol width → implementation returns [] → line
			// gets HasCollapsed = true but no runs.
			var collapsing = TrailingChar(0.001, FlowDirection.LeftToRight);
			var collapsed = line.Collapse(collapsing);

			CornerstoneTest.IsTrue(collapsed.HasCollapsed);
			CornerstoneTest.Empty(collapsed.TextRuns);
		}
	}

	private static void AssertCollapsed(TextLine collapsed, TextLine original)
	{
		CornerstoneTest.NotSame(original, collapsed);
		CornerstoneTest.IsTrue(collapsed.HasCollapsed, "Collapsed line must report HasCollapsed = true.");
	}

	private static TextLine BuildLine(string text, FlowDirection flow)
	{
		var props = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
		var paragraphProps = new GenericTextParagraphProperties(
			flow, TextAlignment.Left, true, true, props, TextWrapping.NoWrap, 0, 0, 0);
		var source = new SingleBufferTextSource(text, props);
		var formatter = new TextFormatterImpl();
		var line = formatter.FormatLine(source, 0, double.PositiveInfinity, paragraphProps);
		CornerstoneTest.IsNotNull(line);
		return line!;
	}

	private static TextLeadingPrefixCharacterEllipsis LeadingPrefix(
		int prefixLength, double width, FlowDirection flow)
	{
		return new("…", prefixLength, width,
			new GenericTextRunProperties(Typeface.Default), flow);
	}

	/// <summary>
	/// Concatenates run text in logical order via
	/// <see cref="LogicalTextRunEnumerator" />. For LTR-only lines this
	/// equals walking <c> TextRuns </c> directly; for RTL/mixed lines it
	/// returns the original-text order (what the collapse contract
	/// requires) instead of the visual post-bidi order.
	/// </summary>
	private static string LogicalText(TextLine line)
	{
		var enumerator = new LogicalTextRunEnumerator(line);
		var sb = new StringBuilder();
		while (enumerator.MoveNext(out var run))
		{
			sb.Append(run!.Text.Span);
		}
		return sb.ToString();
	}

	private static TextTrailingCharacterEllipsis TrailingChar(double width, FlowDirection flow)
	{
		return new("…", width, new GenericTextRunProperties(Typeface.Default), flow);
	}

	private static TextTrailingWordEllipsis TrailingWord(double width, FlowDirection flow)
	{
		return new("…", width, new GenericTextRunProperties(Typeface.Default), flow);
	}

	#endregion

	#region Classes

	/// <summary>
	/// Local copy of the FixedRunsTextSource pattern used in
	/// TextLineTests — that class is private, so duplicate here.
	/// </summary>
	private sealed class FixedRunsTextSource : ITextSource
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
			var pos = 0;
			foreach (var run in _textRuns)
			{
				if (pos == textSourceIndex)
				{
					return run;
				}
				pos += run.Length;
			}
			return null;
		}

		#endregion
	}

	#endregion
}