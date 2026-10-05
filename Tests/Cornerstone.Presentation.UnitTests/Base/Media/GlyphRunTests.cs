#region References

using System;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class GlyphRunTests : TestWithServicesBase
{
	#region Methods

	[DataRow(new double[] { 30, 0, 0 }, new[] { 0, 0, 0 }, 0)]
	[DataRow(new double[] { 0, 0, 30 }, new[] { 0, 0, 0 }, 1)]
	[DataRow(new double[] { 10, 10, 10, 10 }, new[] { 0, 0, 0, 3 }, 0)]
	[DataRow(new double[] { 10, 10, 10, 10 }, new[] { 3, 0, 0, 0 }, 1)]
	[DataRow(new double[] { 10, 10, 10, 10, 10 }, new[] { 0, 1, 1, 1, 4 }, 0)]
	[DataRow(new double[] { 10, 10, 10, 10, 10 }, new[] { 4, 1, 1, 1, 0 }, 1)]
	[PresentationTestMethod]
	public void ShouldFindGlyphIndex(double[] advances, int[] clusters, int bidiLevel)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		using (var glyphRun = CreateGlyphRun(advances, clusters, bidiLevel))
		{
			if (glyphRun.IsLeftToRight)
			{
				for (var i = 0; i < clusters.Length; i++)
				{
					var cluster = clusters[i];

					var found = glyphRun.FindGlyphIndex(cluster);

					var expected = i;

					while (((expected - 1) >= 0) && (clusters[expected - 1] == cluster))
					{
						expected--;
					}

					CornerstoneTest.AreEqual(expected, found);
				}
			}
			else
			{
				for (var i = clusters.Length - 1; i > 0; i--)
				{
					var cluster = clusters[i];

					var found = glyphRun.FindGlyphIndex(cluster);

					var expected = i;

					while (((expected + 1) < clusters.Length) && (clusters[expected + 1] == cluster))
					{
						expected++;
					}

					CornerstoneTest.AreEqual(expected, found);
				}
			}
		}
	}

	[DataRow(new double[] { 10, 10, 10 }, new[] { 10, 11, 12 }, 0, -1, 10, 1, 10)]
	[DataRow(new double[] { 10, 10, 10 }, new[] { 10, 11, 12 }, 0, 15, 12, 1, 10)]
	[DataRow(new double[] { 30, 0, 0 }, new[] { 0, 0, 0 }, 0, 0, 0, 3, 30.0)]
	[DataRow(new double[] { 10, 10, 10 }, new[] { 0, 1, 2 }, 0, 1, 1, 1, 10.0)]
	[DataRow(new double[] { 10, 20, 0, 10 }, new[] { 0, 1, 1, 3 }, 0, 2, 1, 2, 20.0)]
	[DataRow(new double[] { 10, 20, 0, 10 }, new[] { 0, 1, 1, 3 }, 0, 1, 1, 2, 20.0)]
	[DataRow(new double[] { 10, 0, 20, 10 }, new[] { 3, 1, 1, 0 }, 1, 1, 1, 2, 20.0)]
	[PresentationTestMethod]
	public void ShouldFindNearestCharacterHit(double[] advances, int[] clusters, int bidiLevel,
		int index, int expectedIndex, int expectedLength, double expectedWidth)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		using (var glyphRun = CreateGlyphRun(advances, clusters, bidiLevel))
		{
			var textBounds = glyphRun.FindNearestCharacterHit(index, out var width);

			CornerstoneTest.AreEqual(expectedIndex, textBounds.FirstCharacterIndex);

			CornerstoneTest.AreEqual(expectedLength, textBounds.TrailingLength);

			CornerstoneTest.AreEqual(expectedWidth, width, 2);
		}
	}

	[DataRow(new double[] { 30, 0, 0 }, new[] { 0, 0, 0 }, 26.0, 0, 3, true)]
	[DataRow(new double[] { 10, 10, 10 }, new[] { 0, 1, 2 }, 20.0, 2, 0, true)]
	[DataRow(new double[] { 10, 10, 10 }, new[] { 0, 1, 2 }, 26.0, 2, 1, true)]
	[DataRow(new double[] { 10, 10, 10 }, new[] { 0, 1, 2 }, 35.0, 2, 1, false)]
	[PresentationTestMethod]
	public void ShouldGetCharacterHitFromDistance(double[] advances, int[] clusters, double distance, int start,
		int trailingLengthExpected, bool isInsideExpected)
	{
		using (Start())
		using (var glyphRun = CreateGlyphRun(advances, clusters))
		{
			var textBounds = glyphRun.GetCharacterHitFromDistance(distance, out var isInside);

			CornerstoneTest.AreEqual(start, textBounds.FirstCharacterIndex);

			CornerstoneTest.AreEqual(trailingLengthExpected, textBounds.TrailingLength);

			CornerstoneTest.AreEqual(isInsideExpected, isInside);
		}
	}

	[DataRow(new double[] { 30, 0, 0 }, new[] { 0, 0, 0 }, 0, 0, 0)]
	[DataRow(new double[] { 30, 0, 0 }, new[] { 0, 0, 0 }, 0, 3, 30)]
	[DataRow(new double[] { 10, 10, 10 }, new[] { 0, 1, 2 }, 1, 0, 10)]
	[DataRow(new double[] { 10, 10, 10 }, new[] { 0, 1, 2 }, 2, 0, 20)]
	[DataRow(new double[] { 10, 10, 10 }, new[] { 0, 1, 2 }, 2, 1, 30)]
	[PresentationTestMethod]
	public void ShouldGetDistanceFromCharacterHit(double[] advances, int[] clusters, int start, int trailingLength, double expectedDistance)
	{
		using (Start())
		using (var glyphRun = CreateGlyphRun(advances, clusters))
		{
			var characterHit = new CharacterHit(start, trailingLength);

			var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

			CornerstoneTest.AreEqual(expectedDistance, distance);
		}
	}

	[DataRow(new double[] { 30, 0, 0 }, new[] { 0, 0, 0 }, 0, 0, 0, 3, 0)]
	[DataRow(new double[] { 0, 0, 30 }, new[] { 0, 0, 0 }, 0, 0, 0, 3, 1)]
	[DataRow(new double[] { 30, 0, 0, 10 }, new[] { 0, 0, 0, 3 }, 3, 0, 3, 1, 0)]
	[DataRow(new double[] { 10, 0, 0, 30 }, new[] { 3, 0, 0, 0 }, 3, 0, 3, 1, 1)]
	[DataRow(new double[] { 10, 30, 0, 0, 10 }, new[] { 0, 1, 1, 1, 4 }, 1, 0, 4, 0, 0)]
	[DataRow(new double[] { 10, 0, 0, 30, 10 }, new[] { 4, 1, 1, 1, 0 }, 1, 0, 4, 0, 1)]
	[PresentationTestMethod]
	public void ShouldGetNextCharacterHit(double[] advances, int[] clusters,
		int firstCharacterIndex, int trailingLength,
		int nextIndex, int nextLength,
		int bidiLevel)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		using (var glyphRun = CreateGlyphRun(advances, clusters, bidiLevel))
		{
			var characterHit = glyphRun.GetNextCaretCharacterHit(new CharacterHit(firstCharacterIndex, trailingLength));

			CornerstoneTest.AreEqual(nextIndex, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(nextLength, characterHit.TrailingLength);
		}
	}

	[DataRow(new double[] { 30, 0, 0 }, new[] { 0, 0, 0 }, 0, 0, 0, 0, 0)]
	[DataRow(new double[] { 0, 0, 30 }, new[] { 0, 0, 0 }, 0, 0, 0, 0, 1)]
	[DataRow(new double[] { 30, 0, 0, 10 }, new[] { 0, 0, 0, 3 }, 3, 1, 3, 0, 0)]
	[DataRow(new double[] { 0, 0, 30, 10 }, new[] { 3, 0, 0, 0 }, 3, 1, 3, 0, 1)]
	[DataRow(new double[] { 10, 30, 0, 0, 10 }, new[] { 0, 1, 1, 1, 4 }, 4, 1, 4, 0, 0)]
	[DataRow(new double[] { 10, 0, 0, 30, 10 }, new[] { 4, 1, 1, 1, 0 }, 4, 1, 4, 0, 1)]
	[PresentationTestMethod]
	public void ShouldGetPreviousCharacterHit(double[] advances, int[] clusters,
		int currentIndex, int currentLength,
		int previousIndex, int previousLength,
		int bidiLevel)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		using (var glyphRun = CreateGlyphRun(advances, clusters, bidiLevel))
		{
			var characterHit = glyphRun.GetPreviousCaretCharacterHit(new CharacterHit(currentIndex + currentLength));

			CornerstoneTest.AreEqual(previousIndex, characterHit.FirstCharacterIndex);

			CornerstoneTest.AreEqual(previousLength, characterHit.TrailingLength);
		}
	}

	// A run can hold characters and no glyphs at all: a line break in a font that gives the shaper
	// no way to hide it shapes to nothing. Hit-testing has no cluster to snap to there, so it must
	// degenerate to the run's single zero-width position instead of indexing an empty glyph list.
	[PresentationTestMethod]
	public void ShouldHitTestRunWithoutGlyphs()
	{
		using (Start())
		using (var glyphRun = new GlyphRun(Typeface.Default.GlyphTypeface, 10, "\r\n".AsMemory(),
					Array.Empty<GlyphInfo>()))
		{
			CornerstoneTest.AreEqual(0, glyphRun.Bounds.Width);

			// Answered without building a platform glyph run for something that marks nothing.
			CornerstoneTest.AreEqual(default, glyphRun.InkBounds);

			CornerstoneTest.AreEqual(0, glyphRun.GetDistanceFromCharacterHit(new CharacterHit(0)));
			CornerstoneTest.AreEqual(0, glyphRun.GetDistanceFromCharacterHit(new CharacterHit(0, 2)));

			CornerstoneTest.AreEqual(0, glyphRun.FindGlyphIndex(0));

			var nearestHit = glyphRun.FindNearestCharacterHit(0, out var width);

			CornerstoneTest.AreEqual(0, nearestHit.FirstCharacterIndex);
			CornerstoneTest.AreEqual(2, nearestHit.TrailingLength);
			CornerstoneTest.AreEqual(0, width);

			var hitFromDistance = glyphRun.GetCharacterHitFromDistance(0, out var isInside);

			CornerstoneTest.IsFalse(isInside);
			CornerstoneTest.AreEqual(0, hitFromDistance.FirstCharacterIndex);
		}
	}

	private static GlyphRun CreateGlyphRun(double[] glyphAdvances, int[] glyphClusters, int bidiLevel = 0)
	{
		var count = glyphAdvances.Length;

		var glyphInfos = new GlyphInfo[count];
		for (var i = 0; i < count; ++i)
		{
			glyphInfos[i] = new GlyphInfo(0, glyphClusters[i], glyphAdvances[i]);
		}

		return new GlyphRun(Typeface.Default.GlyphTypeface, 10, new string('a', count).AsMemory(), glyphInfos, biDiLevel: bidiLevel);
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.StyledWindow.With(
			renderInterface: new HeadlessPlatformRenderInterface()));
	}

	#endregion
}