#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

[TestClass]
public class TextRunCacheTests
{
	#region Methods

	// Regression tests for the double-reorder bug (commit 2837a287):
	// When shaped runs with shared ShapedBuffer backing arrays were cached, the old
	// BidiReorderer.Reverse() call on a non-owning copy mutated the same backing array
	// that the cached run referenced.  On the next layout the cache returned the already-
	// reversed buffer but with IsReversed=false, causing BidiReorderer to reverse it a
	// second time – putting glyphs back in logical (wrong visual) order for RTL runs.

	[PresentationTestMethod]
	public void BidiCacheHitDoesNotDoubleReorderRTLGlyphClusters()
	{
		using (Start())
		{
			// LTR paragraph containing an Arabic RTL island followed by Latin text.
			// "Hello مرحبا World" – the Arabic word is at source indices 6-10.
			var text = "Hello \u0645\u0631\u062D\u0628\u0627 World";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			using var cache = new TextRunCache();

			// Cache miss: shapes and caches.
			var lineMiss = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(lineMiss);

			// Cache hit: must not double-reverse the RTL run's glyph buffer.
			var lineHit = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(lineHit);
			CornerstoneTest.AreEqual(lineMiss!.TextRuns.Count, lineHit!.TextRuns.Count);

			for (var i = 0; i < lineMiss.TextRuns.Count; i++)
			{
				var missRun = lineMiss.TextRuns[i] as ShapedTextRun;
				var hitRun = lineHit.TextRuns[i] as ShapedTextRun;

				if ((missRun == null) || (hitRun == null))
				{
					continue;
				}

				CornerstoneTest.AreEqual(missRun.BidiLevel, hitRun.BidiLevel);
				CornerstoneTest.AreEqual(missRun.ShapedBuffer.IsLeftToRight, hitRun.ShapedBuffer.IsLeftToRight);
				CornerstoneTest.AreEqual(missRun.ShapedBuffer.Length, hitRun.ShapedBuffer.Length);

				// For RTL runs the shaper produces glyphs in descending cluster order
				// (visual right-to-left).  Double-reversal would flip them back to
				// ascending order (logical), causing wrong rendering.
				if (!missRun.ShapedBuffer.IsLeftToRight && (missRun.ShapedBuffer.Length > 1))
				{
					var missFirst = missRun.ShapedBuffer[0].GlyphCluster;
					var missLast = missRun.ShapedBuffer[missRun.ShapedBuffer.Length - 1].GlyphCluster;
					CornerstoneTest.IsTrue(missFirst >= missLast, $"Cache-miss RTL run: expected descending clusters but got first={missFirst} last={missLast}");

					var hitFirst = hitRun.ShapedBuffer[0].GlyphCluster;
					var hitLast = hitRun.ShapedBuffer[hitRun.ShapedBuffer.Length - 1].GlyphCluster;
					CornerstoneTest.IsTrue(hitFirst >= hitLast, $"Cache-hit RTL run: expected descending clusters but got first={hitFirst} last={hitLast}");

					// The individual cluster values must be identical between miss and hit.
					CornerstoneTest.AreEqual(missFirst, hitFirst);
					CornerstoneTest.AreEqual(missLast, hitLast);
				}
			}
		}
	}

	[PresentationTestMethod]
	public void BidiCacheHitMatchesNoCacheForPureRTLParagraph()
	{
		using (Start())
		{
			// Paragraph-level RTL: all text is Arabic so the resolved flow direction is RTL.
			// "مرحبا بالعالم" (Hello World in Arabic)
			var text = "\u0645\u0631\u062D\u0628\u0627 \u0628\u0627\u0644\u0639\u0627\u0644\u0645";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			// Reference: formatted without any cache.
			var lineNoCache = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, null);

			CornerstoneTest.IsNotNull(lineNoCache);

			using var cache = new TextRunCache();

			var lineMiss = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			var lineHit = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(lineMiss);
			CornerstoneTest.IsNotNull(lineHit);
			CornerstoneTest.AreEqual(lineNoCache!.Length, lineHit!.Length);
			CornerstoneTest.AreEqual(lineNoCache.TextRuns.Count, lineHit.TextRuns.Count);

			for (var i = 0; i < lineNoCache.TextRuns.Count; i++)
			{
				var refRun = lineNoCache.TextRuns[i] as ShapedTextRun;
				var hitRun = lineHit.TextRuns[i] as ShapedTextRun;

				if ((refRun == null) || (hitRun == null))
				{
					continue;
				}

				CornerstoneTest.AreEqual(refRun.BidiLevel, hitRun.BidiLevel);
				CornerstoneTest.AreEqual(refRun.ShapedBuffer.IsLeftToRight, hitRun.ShapedBuffer.IsLeftToRight);
				CornerstoneTest.AreEqual(refRun.ShapedBuffer.Length, hitRun.ShapedBuffer.Length);

				// Glyph cluster values must be identical: no double-reversal allowed.
				for (var j = 0; j < refRun.ShapedBuffer.Length; j++)
				{
					CornerstoneTest.AreEqual(refRun.ShapedBuffer[j].GlyphCluster, hitRun.ShapedBuffer[j].GlyphCluster);
				}
			}
		}
	}

	[PresentationTestMethod]
	public void BidiCacheHitWithWrappingDoesNotDoubleReorder()
	{
		using (Start())
		{
			// Wrapped bidi text so the Wrap path through FormatLineFromCache is exercised.
			var text = "Hello \u0645\u0631\u062D\u0628\u0627 World and more text to force wrapping";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var wrappingProperties = new GenericTextParagraphProperties(
				FlowDirection.LeftToRight, TextAlignment.Left, true, false,
				defaultProperties, TextWrapping.Wrap, 0, 0, 0);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			using var cache = new TextRunCache();

			// Cache miss pass.
			var misLines = FormatAllLines(formatter, textSource, 80.0, wrappingProperties, cache);

			// Cache hit pass.
			var hitLines = FormatAllLines(formatter, textSource, 80.0, wrappingProperties, cache);

			CornerstoneTest.AreEqual(misLines.Length, hitLines.Length);

			for (var lineIdx = 0; lineIdx < misLines.Length; lineIdx++)
			{
				var missLine = misLines[lineIdx];
				var hitLine = hitLines[lineIdx];

				CornerstoneTest.AreEqual(missLine.Length, hitLine.Length);
				CornerstoneTest.AreEqual(missLine.TextRuns.Count, hitLine.TextRuns.Count);

				for (var i = 0; i < missLine.TextRuns.Count; i++)
				{
					var missRun = missLine.TextRuns[i] as ShapedTextRun;
					var hitRun = hitLine.TextRuns[i] as ShapedTextRun;

					if ((missRun == null) || (hitRun == null))
					{
						continue;
					}

					CornerstoneTest.AreEqual(missRun.BidiLevel, hitRun.BidiLevel);
					CornerstoneTest.AreEqual(missRun.ShapedBuffer.IsLeftToRight, hitRun.ShapedBuffer.IsLeftToRight);
					CornerstoneTest.AreEqual(missRun.ShapedBuffer.Length, hitRun.ShapedBuffer.Length);

					for (var j = 0; j < missRun.ShapedBuffer.Length; j++)
					{
						CornerstoneTest.AreEqual(missRun.ShapedBuffer[j].GlyphCluster, hitRun.ShapedBuffer[j].GlyphCluster);
					}
				}
			}
		}
	}

	[PresentationTestMethod]
	public void BidiTextWithCacheProducesCorrectResults()
	{
		using (Start())
		{
			// Mixed LTR/RTL text.
			var text = "Hello \u0627\u0644\u0639\u0631\u0628\u064A\u0629 World";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			// Without cache.
			var lineWithout = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, null);

			// With cache.
			using var cache = new TextRunCache();
			var lineWith = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(lineWithout);
			CornerstoneTest.IsNotNull(lineWith);

			CornerstoneTest.AreEqual(lineWithout!.Length, lineWith!.Length);
			CornerstoneTest.AreEqual(lineWithout.TextRuns.Count, lineWith.TextRuns.Count);

			// Cache hit should also produce correct results.
			var lineCacheHit = formatter.FormatLine(textSource, 0, 200.0,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(lineCacheHit);
			CornerstoneTest.AreEqual(lineWithout.Length, lineCacheHit!.Length);
		}
	}

	[PresentationTestMethod]
	public void CacheHitProducesIdenticalLayout()
	{
		using (Start())
		{
			var text = "Hello World";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			using var cache = new TextRunCache();

			// First call: cache miss, populates cache.
			var line1 = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(line1);

			// Second call: cache hit, different paragraph width.
			var line2 = formatter.FormatLine(textSource, 0, 200.0,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(line2);

			// Both lines should have the same text length.
			CornerstoneTest.AreEqual(line1!.Length, line2!.Length);

			// Both lines should have the same number of text runs.
			CornerstoneTest.AreEqual(line1.TextRuns.Count, line2.TextRuns.Count);
		}
	}

	[PresentationTestMethod]
	public void CacheWithMultipleParagraphs()
	{
		using (Start())
		{
			var text = "First line\nSecond line\nThird line";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			using var cache = new TextRunCache();

			// Format all paragraphs.
			var lines = FormatAllLines(formatter, textSource, double.PositiveInfinity,
				paragraphProperties, cache);

			CornerstoneTest.IsTrue(lines.Length >= 3);

			// Format again from cache with different width.
			var lines2 = FormatAllLines(formatter, textSource, double.PositiveInfinity,
				paragraphProperties, cache);

			CornerstoneTest.AreEqual(lines.Length, lines2.Length);

			for (var i = 0; i < lines.Length; i++)
			{
				CornerstoneTest.AreEqual(lines[i].Length, lines2[i].Length);
			}
		}
	}

	[PresentationTestMethod]
	public void DisposeReleasesCacheEntries()
	{
		using (Start())
		{
			var text = "Hello World";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			var cache = new TextRunCache();

			// Populate cache.
			formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			// Dispose should not throw.
			cache.Dispose();

			// After dispose, using the cache should still work (re-creates entries).
			using var cache2 = new TextRunCache();

			var line = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache2);

			CornerstoneTest.IsNotNull(line);
		}
	}

	[PresentationTestMethod]
	public void FullInvalidationClearsCache()
	{
		using (Start())
		{
			var text = "Hello World";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			using var cache = new TextRunCache();

			// Populate cache.
			formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			// Invalidate.
			cache.Invalidate();

			// Verify cache miss: should not throw and should produce a valid line.
			var line = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(line);
			CornerstoneTest.AreEqual(text.Length, line!.Length);
		}
	}

	[PresentationTestMethod]
	public void PartialInvalidationPreservesEarlierEntries()
	{
		using (Start())
		{
			var text = "First paragraph\nSecond paragraph";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			using var cache = new TextRunCache();

			// Format first paragraph (populates cache at index 0).
			var line1 = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(line1);

			var firstLineLength = line1!.Length;

			// Format second paragraph (populates cache at firstLineLength).
			var line2 = formatter.FormatLine(textSource, firstLineLength, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(line2);

			// Invalidate from the second paragraph index.
			cache.InvalidateFrom(firstLineLength);

			// First paragraph should still be cached (cache hit).
			var line1Again = formatter.FormatLine(textSource, 0, 200.0,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(line1Again);
			CornerstoneTest.AreEqual(line1.Length, line1Again!.Length);

			// Second paragraph should be re-shaped (cache miss then re-populated).
			var line2Again = formatter.FormatLine(textSource, firstLineLength, double.PositiveInfinity,
				paragraphProperties, null, cache);

			CornerstoneTest.IsNotNull(line2Again);
			CornerstoneTest.AreEqual(line2!.Length, line2Again!.Length);
		}
	}

	[PresentationTestMethod]
	public void TextLayoutRecreatedFromCacheWithBidiHasSameGlyphOrder()
	{
		using (Start())
		{
			// Mixed LTR/RTL text used for two successive TextLayout instances that share a cache.
			// Before the fix, the second instance would double-reverse RTL glyph buffers.
			var text = "Hello \u0645\u0631\u062D\u0628\u0627 World";

			using var cache = new TextRunCache();

			using var layout1 = new TextLayout(text, Typeface.Default, 12,
				textRunCache: cache);

			// Second layout: triggers cache-hit path – previously double-reordered RTL runs.
			using var layout2 = new TextLayout(text, Typeface.Default, 12,
				textRunCache: cache);

			CornerstoneTest.AreEqual(layout1.TextLines.Count, layout2.TextLines.Count);

			for (var lineIdx = 0; lineIdx < layout1.TextLines.Count; lineIdx++)
			{
				var line1 = layout1.TextLines[lineIdx];
				var line2 = layout2.TextLines[lineIdx];

				CornerstoneTest.AreEqual(line1.Length, line2.Length);
				CornerstoneTest.AreEqual(line1.TextRuns.Count, line2.TextRuns.Count);

				for (var i = 0; i < line1.TextRuns.Count; i++)
				{
					var run1 = line1.TextRuns[i] as ShapedTextRun;
					var run2 = line2.TextRuns[i] as ShapedTextRun;

					if ((run1 == null) || (run2 == null))
					{
						continue;
					}

					CornerstoneTest.AreEqual(run1.BidiLevel, run2.BidiLevel);
					CornerstoneTest.AreEqual(run1.ShapedBuffer.IsLeftToRight, run2.ShapedBuffer.IsLeftToRight);
					CornerstoneTest.AreEqual(run1.ShapedBuffer.Length, run2.ShapedBuffer.Length);

					for (var j = 0; j < run1.ShapedBuffer.Length; j++)
					{
						CornerstoneTest.AreEqual(run1.ShapedBuffer[j].GlyphCluster, run2.ShapedBuffer[j].GlyphCluster);
					}
				}
			}
		}
	}

	[PresentationTestMethod]
	public void TextLayoutWithCacheMatchesWithout()
	{
		using (Start())
		{
			var text = "The quick brown fox jumps over the lazy dog";

			// Layout without cache.
			var layout1 = new TextLayout(text, Typeface.Default, 12,
				textWrapping: TextWrapping.Wrap, maxWidth: 100);

			// Layout with cache.
			using var cache = new TextRunCache();
			var layout2 = new TextLayout(text, Typeface.Default, 12,
				textWrapping: TextWrapping.Wrap, maxWidth: 100, textRunCache: cache);

			CornerstoneTest.AreEqual(layout1.TextLines.Count, layout2.TextLines.Count);
			CornerstoneTest.AreEqual(layout1.Height, layout2.Height);
			CornerstoneTest.AreEqual(layout1.WidthIncludingTrailingWhitespace, layout2.WidthIncludingTrailingWhitespace);

			// Second layout from cache with different width.
			var layout3 = new TextLayout(text, Typeface.Default, 12,
				textWrapping: TextWrapping.Wrap, maxWidth: 80, textRunCache: cache);

			// Should still be valid (more lines due to narrower width).
			CornerstoneTest.IsTrue(layout3.TextLines.Count >= layout2.TextLines.Count);
			CornerstoneTest.IsTrue(layout3.Height > 0);

			layout1.Dispose();
			layout2.Dispose();
			layout3.Dispose();
		}
	}

	[PresentationTestMethod]
	public void TextWrappingWithCacheProducesCorrectLines()
	{
		using (Start())
		{
			var text = "The quick brown fox jumps over the lazy dog";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var wrappingProperties = new GenericTextParagraphProperties(
				FlowDirection.LeftToRight, TextAlignment.Left, true, false,
				defaultProperties, TextWrapping.Wrap, 0, 0, 0);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			// Format without cache.
			var linesWithout = FormatAllLines(formatter, textSource, 100.0, wrappingProperties, null);

			// Format with cache (first pass: cache miss).
			using var cache = new TextRunCache();
			var linesWith = FormatAllLines(formatter, textSource, 100.0, wrappingProperties, cache);

			CornerstoneTest.AreEqual(linesWithout.Length, linesWith.Length);

			for (var i = 0; i < linesWithout.Length; i++)
			{
				CornerstoneTest.AreEqual(linesWithout[i].Length, linesWith[i].Length);
			}

			// Format with cache again (second pass: cache hit).
			var linesCacheHit = FormatAllLines(formatter, textSource, 100.0, wrappingProperties, cache);

			CornerstoneTest.AreEqual(linesWithout.Length, linesCacheHit.Length);

			for (var i = 0; i < linesWithout.Length; i++)
			{
				CornerstoneTest.AreEqual(linesWithout[i].Length, linesCacheHit[i].Length);
			}
		}
	}

	[PresentationTestMethod]
	public void WrappingWithDifferentWidthFromCache()
	{
		using (Start())
		{
			var text = "The quick brown fox jumps over the lazy dog";
			var defaultProperties = new GenericTextRunProperties(Typeface.Default);
			var wrappingProperties = new GenericTextParagraphProperties(
				FlowDirection.LeftToRight, TextAlignment.Left, true, false,
				defaultProperties, TextWrapping.Wrap, 0, 0, 0);
			var textSource = new SingleBufferTextSource(text, defaultProperties);
			var formatter = new TextFormatterImpl();

			using var cache = new TextRunCache();

			// First format: wide (cache miss, populates).
			var wideLines = FormatAllLines(formatter, textSource, 500.0, wrappingProperties, cache);

			// Second format: narrow (cache hit, different wrapping).
			var narrowLines = FormatAllLines(formatter, textSource, 80.0, wrappingProperties, cache);

			// Narrow should produce more lines.
			CornerstoneTest.IsTrue(narrowLines.Length >= wideLines.Length);

			// Total characters should be the same.
			int wideTotal = 0, narrowTotal = 0;
			foreach (var l in wideLines)
			{
				wideTotal += l.Length;
			}
			foreach (var l in narrowLines)
			{
				narrowTotal += l.Length;
			}

			CornerstoneTest.AreEqual(wideTotal, narrowTotal);
		}
	}

	private static TextLine[] FormatAllLines(TextFormatterImpl formatter, ITextSource textSource,
		double paragraphWidth, TextParagraphProperties paragraphProperties, TextRunCache cache)
	{
		var lines = new List<TextLine>();
		var currentIndex = 0;
		TextLine previousLine = null;

		while (true)
		{
			var line = formatter.FormatLine(textSource, currentIndex, paragraphWidth,
				paragraphProperties, previousLine?.TextLineBreak, cache);

			if (line == null)
			{
				break;
			}

			lines.Add(line);
			currentIndex += line.Length;
			previousLine = line;

			if (line.TextLineBreak?.TextEndOfLine is TextEndOfParagraph)
			{
				break;
			}
		}

		return lines.ToArray();
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
			.With(renderInterface: new PlatformRenderInterface(null),
				fontManagerImpl: new CustomFontManagerImpl()));
	}

	#endregion
}