#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

/// <summary>
/// Exercises the ref-counted shared storage and the per-write generation
/// counter that <see cref="ShapedBuffer.Split" /> and
/// <see cref="ShapedBuffer.WithBidiLevel" /> rely on. Each test starts from
/// a freshly shaped buffer (so the glyph and cluster arrays are pool-rented
/// and ref-counted) and checks that:
/// <list type="bullet">
/// <item> aliases keep working after their source is disposed, </item>
/// <item> <see cref="ShapedBuffer.Dispose" /> is idempotent, </item>
/// <item>
/// indexer mutations propagate to siblings via the generation bump
/// (i.e. nobody is left with a stale cluster cache).
/// </item>
/// </list>
/// </summary>
[TestClass]
public class ShapedBufferSharedStorageTests
{
	#region Constants

	private const string AsciiText = "The quick brown fox";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void DisposeIsIdempotent()
	{
		using (Start())
		{
			var buffer = ShapeAscii(AsciiText);
			_ = buffer.TotalGlyphAdvance; // prime cluster cache so refs are non-null.

			buffer.Dispose();
			buffer.Dispose();
			buffer.Dispose();
		}
	}

	[PresentationTestMethod]
	public void IndexerMutationInvalidatesOwnClusterCache()
	{
		using (Start())
		{
			using var buffer = ShapeAscii(AsciiText);

			var advanceBefore = buffer.TotalGlyphAdvance;
			var original = buffer[0];

			// Mutate the leading glyph's advance via the indexer setter.
			const double delta = 50d;
			buffer[0] = new GlyphInfo(
				original.GlyphIndex, original.GlyphCluster,
				original.GlyphAdvance + delta, original.GlyphOffset);

			// The cache must be rebuilt against the new glyph data.
			CornerstoneTest.AreEqual(advanceBefore + delta, buffer.TotalGlyphAdvance, 3);
		}
	}

	[PresentationTestMethod]
	public void IndexerMutationInvalidatesWithBidiLevelAliasCache()
	{
		using (Start())
		{
			using var original = ShapeAscii(AsciiText);
			using var alias = original.WithBidiLevel(2);

			var aliasBefore = alias.TotalGlyphAdvance;

			const double delta = 33d;
			var glyph = original[0];
			original[0] = new GlyphInfo(
				glyph.GlyphIndex, glyph.GlyphCluster,
				glyph.GlyphAdvance + delta, glyph.GlyphOffset);

			CornerstoneTest.AreEqual(aliasBefore + delta, alias.TotalGlyphAdvance, 3);
		}
	}

	[PresentationTestMethod]
	public void IndexerMutationOnChildInvalidatesParentAndSibling()
	{
		using (Start())
		{
			using var parent = ShapeAscii(AsciiText);

			var splitIndex = parent.Text.Length / 2;
			var split = parent.Split(splitIndex);
			using var first = split.First!;
			using var second = split.Second!;

			var parentBefore = parent.TotalGlyphAdvance;
			_ = first.TotalGlyphAdvance;
			_ = second.TotalGlyphAdvance;

			const double delta = 17d;
			var glyph = first[0];
			first[0] = new GlyphInfo(
				glyph.GlyphIndex, glyph.GlyphCluster,
				glyph.GlyphAdvance + delta, glyph.GlyphOffset);

			CornerstoneTest.AreEqual(parentBefore + delta, parent.TotalGlyphAdvance, 3);
		}
	}

	[PresentationTestMethod]
	public void IndexerMutationOnParentInvalidatesSiblingCachesAfterSplit()
	{
		using (Start())
		{
			using var parent = ShapeAscii(AsciiText);

			var splitIndex = parent.Text.Length / 2;
			var split = parent.Split(splitIndex);
			using var first = split.First!;
			using var second = split.Second!;

			// Prime both children's views of the inherited cluster cache.
			var firstBefore = first.TotalGlyphAdvance;
			var secondBefore = second.TotalGlyphAdvance;

			// Mutate parent[0]: this lives in the first child's slice but
			// bumps the generation counter on the shared glyph holder so
			// the second child also sees the change (and rebuilds if needed).
			const double delta = 25d;
			var first0 = parent[0];
			parent[0] = new GlyphInfo(
				first0.GlyphIndex, first0.GlyphCluster,
				first0.GlyphAdvance + delta, first0.GlyphOffset);

			CornerstoneTest.AreEqual(firstBefore + delta, first.TotalGlyphAdvance, 3);

			// Second child's range doesn't include glyph 0 so its advance is unchanged,
			// but its cache must still have been invalidated/rebuilt without error.
			CornerstoneTest.AreEqual(secondBefore, second.TotalGlyphAdvance, 3);
		}
	}

	// Regression: when a buffer is mutated *before* it is Split / aliased,
	// the parent's _cacheGeneration is already > 0 by the time the alias
	// inherits the cluster cache. The alias constructor must copy that
	// generation onto the child; otherwise the child's stamp stays at 0,
	// EnsureClusterCache sees a mismatch on first access and rebuilds a
	// fresh cache instead of reusing the parent's pooled arrays — silently
	// defeating the cached-split fast path.
	[PresentationTestMethod]
	public void SplitChildrenShareParentClusterCacheWhenParentWasMutatedBeforeSplit()
	{
		using (Start())
		{
			using var parent = ShapeAscii(AsciiText);

			// Mutate first so the shared glyph holder's generation is
			// non-zero before the cluster cache is built.
			var first0 = parent[0];
			parent[0] = new GlyphInfo(
				first0.GlyphIndex, first0.GlyphCluster,
				first0.GlyphAdvance + 10d, first0.GlyphOffset);

			// Prime the parent's cluster cache against the bumped generation.
			_ = parent.TotalGlyphAdvance;
			var parentPrefix = parent.ClusterPrefix;
			CornerstoneTest.IsNotNull(parentPrefix);

			var split = parent.Split(parent.Text.Length / 2);
			using var first = split.First!;
			using var second = split.Second!;

			// Touching the child's metrics must reuse the parent's prefix
			// array, not rebuild a fresh one.
			_ = first.TotalGlyphAdvance;
			_ = second.TotalGlyphAdvance;

			CornerstoneTest.Same(parentPrefix, first.ClusterPrefix);
			CornerstoneTest.Same(parentPrefix, second.ClusterPrefix);
		}
	}

	[PresentationTestMethod]
	public void SplitChildrenSurviveParentDisposal()
	{
		using (Start())
		{
			var parent = ShapeAscii(AsciiText);
			var totalBefore = parent.TotalGlyphAdvance;

			var split = parent.Split(parent.Text.Length / 2);
			var first = split.First!;
			var second = split.Second!;

			// Prime sibling caches via the parent's cluster-cache reference
			// (children inherit the parent's prefix sums by ref).
			_ = first.TotalGlyphAdvance;
			_ = second.TotalGlyphAdvance;

			// Release the parent's references first; the ref-counted holders
			// must keep the pool arrays alive for the surviving children.
			parent.Dispose();

			CornerstoneTest.AreEqual(totalBefore, first.TotalGlyphAdvance + second.TotalGlyphAdvance, 3);
			CornerstoneTest.AreEqual(parent.Text.Length / 2, first.Text.Length);
			CornerstoneTest.IsTrue(first.Length > 0);
			CornerstoneTest.IsTrue(second.Length > 0);

			first.Dispose();
			second.Dispose();
		}
	}

	[PresentationTestMethod]
	public void SplitParentSurvivesChildrenDisposal()
	{
		using (Start())
		{
			using var parent = ShapeAscii(AsciiText);
			var totalBefore = parent.TotalGlyphAdvance;

			var split = parent.Split(parent.Text.Length / 2);
			split.First!.Dispose();
			split.Second!.Dispose();

			// Parent's own refs must still hold the pool arrays alive.
			CornerstoneTest.AreEqual(totalBefore, parent.TotalGlyphAdvance, 6);
		}
	}

	[PresentationTestMethod]
	public void WithBidiLevelAliasSharesClusterCacheWhenOriginalWasMutatedBeforeAlias()
	{
		using (Start())
		{
			using var original = ShapeAscii(AsciiText);

			var first0 = original[0];
			original[0] = new GlyphInfo(
				first0.GlyphIndex, first0.GlyphCluster,
				first0.GlyphAdvance + 7d, first0.GlyphOffset);

			_ = original.TotalGlyphAdvance;
			var originalPrefix = original.ClusterPrefix;
			CornerstoneTest.IsNotNull(originalPrefix);

			using var alias = original.WithBidiLevel(2);

			_ = alias.TotalGlyphAdvance;

			CornerstoneTest.Same(originalPrefix, alias.ClusterPrefix);
		}
	}

	[PresentationTestMethod]
	public void WithBidiLevelAliasSurvivesOriginalDisposal()
	{
		using (Start())
		{
			var original = ShapeAscii(AsciiText);
			var totalBefore = original.TotalGlyphAdvance;
			CornerstoneTest.AreEqual(0, original.BidiLevel);

			using var alias = original.WithBidiLevel(2);

			original.Dispose();

			CornerstoneTest.AreEqual(totalBefore, alias.TotalGlyphAdvance, 6);
			CornerstoneTest.AreEqual(2, alias.BidiLevel);
			CornerstoneTest.AreEqual(original.Text.Length, alias.Text.Length);
		}
	}

	[PresentationTestMethod]
	public void WithBidiLevelReturnsSameInstanceWhenLevelMatches()
	{
		using (Start())
		{
			using var original = ShapeAscii(AsciiText);
			var alias = original.WithBidiLevel(original.BidiLevel);
			CornerstoneTest.Same(original, alias);
		}
	}

	private static ShapedBuffer ShapeAscii(string text)
	{
		var options = new TextShaperOptions(
			Typeface.Default.GlyphTypeface, 12, 0, CultureInfo.CurrentCulture);
		return TextShaper.Current.ShapeText(text, options);
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
			.With(renderInterface: new PlatformRenderInterface(null),
				fontManagerImpl: new CustomFontManagerImpl()));
	}

	#endregion
}