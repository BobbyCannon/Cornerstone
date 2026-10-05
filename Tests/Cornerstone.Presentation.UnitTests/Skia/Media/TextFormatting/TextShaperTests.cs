#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

[TestClass]
public class TextShaperTests
{
	#region Methods

	[PresentationTestMethod]
	public void ClusterCacheNotSimpleModeForComplexClusters()
	{
		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Cascadia Code"));

			// Same text the existing Should_Not_Split_Cluster test uses: contains a
			// two-codepoint cluster that breaks the one-char-per-cluster invariant.
			var buffer = TextShaper.Current.ShapeText("a\"๊a", new TextShaperOptions(typeface.GlyphTypeface));

			CornerstoneTest.IsFalse(buffer.IsClusterCacheSimple, "Multi-char clusters should fall back to the full cluster-start-chars table.");
		}
	}

	[PresentationTestMethod]
	public void ClusterCacheSimpleModeForLatinText()
	{
		using (Start())
		{
			var buffer = TextShaper.Current.ShapeText("ABCDEFGH", new TextShaperOptions(Typeface.Default.GlyphTypeface));

			CornerstoneTest.IsTrue(buffer.IsClusterCacheSimple, "Single-codepoint LTR text should use the simple cluster-cache mode.");
		}
	}

	[PresentationTestMethod]
	public void ClusterCacheSimpleModeMeasuresCorrectly()
	{
		using (Start())
		{
			var buffer = TextShaper.Current.ShapeText("ABCDEFGH", new TextShaperOptions(Typeface.Default.GlyphTypeface));

			CornerstoneTest.IsTrue(buffer.IsClusterCacheSimple);

			// Sum advances linearly and compare to TotalGlyphAdvance.
			var expectedTotal = 0d;
			for (var i = 0; i < buffer.Length; i++)
			{
				expectedTotal += buffer[i].GlyphAdvance;
			}

			CornerstoneTest.AreEqual(expectedTotal, buffer.TotalGlyphAdvance, 5);

			// Measure: ask for the width of the first 3 glyphs.
			var threeGlyphsWidth = buffer[0].GlyphAdvance + buffer[1].GlyphAdvance + buffer[2].GlyphAdvance;
			var fit = buffer.FindLeadingCharCountWithinWidth(threeGlyphsWidth);
			var widthConsumed = buffer.GetCharRangeWidth(0, fit);

			CornerstoneTest.AreEqual(3, fit);
			CornerstoneTest.AreEqual(threeGlyphsWidth, widthConsumed, 5);

			// FirstClusterCharLength must be 1 in simple mode.
			CornerstoneTest.AreEqual(1, buffer.FirstClusterCharLength);
		}
	}

	[PresentationTestMethod]
	public void ClusterCacheSimpleModeSurvivesSplit()
	{
		using (Start())
		{
			var buffer = TextShaper.Current.ShapeText("ABCDEFGH", new TextShaperOptions(Typeface.Default.GlyphTypeface));

			CornerstoneTest.IsTrue(buffer.IsClusterCacheSimple);

			var split = buffer.Split(3);

			CornerstoneTest.IsNotNull(split.First);
			CornerstoneTest.IsNotNull(split.Second);
			CornerstoneTest.AreEqual(3, split.First!.Length);
			CornerstoneTest.AreEqual(5, split.Second!.Length);

			CornerstoneTest.IsTrue(split.First.IsClusterCacheSimple, "Split halves of a simple-mode buffer should also be simple-mode.");
			CornerstoneTest.IsTrue(split.Second.IsClusterCacheSimple);

			var firstWidth = buffer[0].GlyphAdvance + buffer[1].GlyphAdvance + buffer[2].GlyphAdvance;
			CornerstoneTest.AreEqual(firstWidth, split.First.TotalGlyphAdvance, 5);
		}
	}

	[PresentationTestMethod]
	public void ClusterCacheSimpleModeTrimmingHelpersAreCorrect()
	{
		using (Start())
		{
			var buffer = TextShaper.Current.ShapeText("ABCDEFGH", new TextShaperOptions(Typeface.Default.GlyphTypeface));

			CornerstoneTest.IsTrue(buffer.IsClusterCacheSimple);

			var advances = new double[buffer.Length];
			for (var i = 0; i < buffer.Length; i++)
			{
				advances[i] = buffer[i].GlyphAdvance;
			}

			double Sum(int start, int end)
			{
				var w = 0d;
				for (var i = start; i < end; i++)
				{
					w += advances[i];
				}
				return w;
			}

			// GetCharRangeWidth: exact sub-range sums, including out-of-range clamping.
			// These must not throw in simple mode (the regression: _clusterStartChars is null).
			CornerstoneTest.AreEqual(Sum(0, 3), buffer.GetCharRangeWidth(0, 3), 5);
			CornerstoneTest.AreEqual(Sum(2, 5), buffer.GetCharRangeWidth(2, 5), 5);
			CornerstoneTest.AreEqual(Sum(0, 8), buffer.GetCharRangeWidth(-2, 100), 5); // clamped to [0, 8]
			CornerstoneTest.AreEqual(0d, buffer.GetCharRangeWidth(4, 4), 5);

			// FindLeadingCharCountWithinWidth: budget mid-way into the 4th glyph -> first 3 fit.
			var leadingBudget = Sum(0, 3) + (advances[3] * 0.5);
			CornerstoneTest.AreEqual(3, buffer.FindLeadingCharCountWithinWidth(leadingBudget));

			// FindTrailingCharCountWithinWidth: budget mid-way into glyph index 4 -> last 3 fit.
			var trailingBudget = Sum(5, 8) + (advances[4] * 0.5);
			var trailingCount = buffer.FindTrailingCharCountWithinWidth(trailingBudget, out var consumed);
			CornerstoneTest.AreEqual(3, trailingCount);
			CornerstoneTest.AreEqual(Sum(5, 8), consumed, 5);
		}
	}

	[PresentationTestMethod]
	public void ClusterCacheSimpleModeTrimmingHelpersSurviveSplit()
	{
		using (Start())
		{
			var buffer = TextShaper.Current.ShapeText("ABCDEFGH", new TextShaperOptions(Typeface.Default.GlyphTypeface));
			CornerstoneTest.IsTrue(buffer.IsClusterCacheSimple);

			var split = buffer.Split(3);
			var second = split.Second;
			CornerstoneTest.IsNotNull(second);
			CornerstoneTest.IsTrue(second!.IsClusterCacheSimple);
			CornerstoneTest.AreEqual(5, second.Length); // "DEFGH"

			var advances = new double[second.Length];
			for (var i = 0; i < second.Length; i++)
			{
				advances[i] = second[i].GlyphAdvance;
			}

			// Exercises the _clusterStartIdx offset on a simple-mode sub-buffer.
			var firstTwo = advances[0] + advances[1];
			CornerstoneTest.AreEqual(firstTwo, second.GetCharRangeWidth(0, 2), 5);

			var leadingBudget = firstTwo + (advances[2] * 0.5);
			CornerstoneTest.AreEqual(2, second.FindLeadingCharCountWithinWidth(leadingBudget));
		}
	}

	[PresentationTestMethod]
	public void ShouldApplyIncrementalTabWidth()
	{
		using (Start())
		{
			var text = "012345\t";
			var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 12, 0, CultureInfo.CurrentCulture, 100);
			var shapedBuffer = TextShaper.Current.ShapeText(text.AsMemory().Slice(6), options);

			CornerstoneTest.AreEqual(1, shapedBuffer.Length);
			CornerstoneTest.AreEqual(100, shapedBuffer[0].GlyphAdvance);
		}
	}

	[PresentationTestMethod]
	public void ShouldFormClustersForBreakPairs()
	{
		using (Start())
		{
			var text = "\n\r\n";
			var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 12, 0, CultureInfo.CurrentCulture);
			var shapedBuffer = TextShaper.Current.ShapeText(text, options);

			CornerstoneTest.AreEqual(shapedBuffer.Length, text.Length);
			CornerstoneTest.AreEqual(shapedBuffer.Length, text.Length);
			CornerstoneTest.AreEqual(0, shapedBuffer[0].GlyphCluster);
			CornerstoneTest.AreEqual(1, shapedBuffer[1].GlyphCluster);
			CornerstoneTest.AreEqual(1, shapedBuffer[2].GlyphCluster);
		}
	}

	[PresentationTestMethod]
	public void ShouldNotSplitCluster()
	{
		using (Start())
		{
			var typeface = new Typeface(FontFamily.Parse("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Cascadia Code"));

			var buffer = TextShaper.Current.ShapeText("a\"๊a", new TextShaperOptions(typeface.GlyphTypeface));

			var splitResult = buffer.Split(1);

			CornerstoneTest.IsNotNull(splitResult.First);
			CornerstoneTest.AreEqual(1, splitResult.First.Length);

			buffer = splitResult.Second;

			CornerstoneTest.IsNotNull(buffer);

			//\"๊  
			splitResult = buffer.Split(1);

			CornerstoneTest.IsNotNull(splitResult.First);
			CornerstoneTest.AreEqual(2, splitResult.First.Length);

			buffer = splitResult.Second;

			CornerstoneTest.IsNotNull(buffer);
		}
	}

	[PresentationTestMethod]
	public void ShouldNotSplitRightToLeftCluster()
	{
		// Arabic letters carry their harakat in the same cluster, so the first cluster of this
		// text spans two characters. Splitting inside it keeps the cluster's glyphs together in
		// the leading half - the text boundary has to follow them, or the two halves disagree
		// about which characters their glyphs cover.
		const string text = "أَبْجَدِيَّة";

		using (Start())
		{
			var codepoint = Codepoint.ReadAt(text, 0, out _);

			CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(codepoint, FontStyle.Normal, FontWeight.Normal,
				FontStretch.Normal, null, null, out var typeface));

			var options = new TextShaperOptions(typeface.GlyphTypeface, 12, 1, CultureInfo.InvariantCulture);
			var buffer = TextShaper.Current.ShapeText(text.AsMemory(), options);

			// Precondition: the first cluster covers the first two characters.
			CornerstoneTest.IsFalse(buffer.IsLeftToRight);
			CornerstoneTest.AreEqual(0, buffer[buffer.Length - 1].GlyphCluster);
			CornerstoneTest.AreEqual(0, buffer[buffer.Length - 2].GlyphCluster);
			CornerstoneTest.AreEqual(2, buffer[buffer.Length - 3].GlyphCluster);

			var splitResult = buffer.Split(1);

			var first = splitResult.First;
			var second = splitResult.Second;

			CornerstoneTest.IsNotNull(first);
			CornerstoneTest.IsNotNull(second);

			// The split snaps forward past the cluster, exactly like the left-to-right path.
			CornerstoneTest.AreEqual(2, first!.Text.Length);
			CornerstoneTest.AreEqual(2, first.Length);

			// No character and no glyph is lost or duplicated.
			CornerstoneTest.AreEqual(text.Length, first.Text.Length + second!.Text.Length);
			CornerstoneTest.AreEqual(buffer.Length, first.Length + second.Length);

			// Every glyph of the trailing half belongs to the characters the trailing half owns.
			for (var i = 0; i < second.Length; i++)
			{
				CornerstoneTest.IsTrue(second[i].GlyphCluster >= first.Text.Length, $"Glyph {i} has cluster {second[i].GlyphCluster}, which the leading half owns.");
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldSplitRightToLeft()
	{
		var text = "أَبْجَدِيَّة عَرَبِيَّة";

		using (Start())
		{
			var codePoint = Codepoint.ReadAt(text, 0, out _);

			CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(codePoint, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var typeface));

			var buffer = TextShaper.Current.ShapeText(text, new TextShaperOptions(typeface.GlyphTypeface));

			var splitResult = buffer.Split(6);

			var first = splitResult.First;

			CornerstoneTest.IsNotNull(first);
			CornerstoneTest.AreEqual(6, first.Length);
		}
	}

	[PresentationTestMethod]
	public void ShouldSplitZeroLength()
	{
		var text = "ABC";

		using (Start())
		{
			var buffer = TextShaper.Current.ShapeText(text, new TextShaperOptions(Typeface.Default.GlyphTypeface));

			var splitResult = buffer.Split(0);

			CornerstoneTest.IsNotNull(splitResult.First);
			CornerstoneTest.AreEqual(0, splitResult.First.Length);

			CornerstoneTest.IsNotNull(splitResult.Second);

			CornerstoneTest.AreEqual(text.Length, splitResult.Second.Length);
		}
	}

	private static IDisposable Start()
	{
		var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
			.With(renderInterface: new PlatformRenderInterface(null),
				fontManagerImpl: new CustomFontManagerImpl()));

		return disposable;
	}

	#endregion
}