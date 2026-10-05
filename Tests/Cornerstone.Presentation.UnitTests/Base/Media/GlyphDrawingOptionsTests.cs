#region References

using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class GlyphDrawingOptionsTests
{
	#region Methods

	[PresentationTestMethod]
	public void DefaultHasNullPaletteIndexAndNullPixelSize()
	{
		var options = GlyphDrawingOptions.Default;

		CornerstoneTest.IsNull(options.PaletteIndex);
		CornerstoneTest.IsNull(options.PixelSize);
	}

	[PresentationTestMethod]
	public void DefaultIsASingleton()
	{
		CornerstoneTest.Same(GlyphDrawingOptions.Default, GlyphDrawingOptions.Default);
	}

	[PresentationTestMethod]
	public void EqualityDistinguishesDifferentPaletteIndex()
	{
		var a = new GlyphDrawingOptions { PaletteIndex = 0 };
		var b = new GlyphDrawingOptions { PaletteIndex = 1 };

		CornerstoneTest.AreNotEqual(a, b);
	}

	[PresentationTestMethod]
	public void EqualityDistinguishesDifferentPixelSize()
	{
		var a = new GlyphDrawingOptions { PixelSize = 16 };
		var b = new GlyphDrawingOptions { PixelSize = 32 };

		CornerstoneTest.AreNotEqual(a, b);
	}

	[PresentationTestMethod]
	public void EqualityIsStructuralAcrossTwoRecordsWithSameValues()
	{
		var a = new GlyphDrawingOptions { PaletteIndex = 1, PixelSize = 16 };
		var b = new GlyphDrawingOptions { PaletteIndex = 1, PixelSize = 16 };

		CornerstoneTest.NotSame(a, b);
		CornerstoneTest.AreEqual(a, b);
		CornerstoneTest.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(99)]
	public void PaletteIndexAcceptsNonNegativeValues(int value)
	{
		var options = new GlyphDrawingOptions { PaletteIndex = value };

		CornerstoneTest.AreEqual(value, options.PaletteIndex);
	}

	[PresentationTestMethod]
	public void PaletteIndexAcceptsNull()
	{
		var options = new GlyphDrawingOptions { PaletteIndex = null };

		CornerstoneTest.IsNull(options.PaletteIndex);
	}

	[PresentationTestMethod]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	public void PaletteIndexRejectsNegativeValues(int value)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new GlyphDrawingOptions { PaletteIndex = value });
	}

	[PresentationTestMethod]
	public void ParameterlessConstructorProducesAnInstanceEqualToDefault()
	{
		// The record's equality contract should treat a freshly-constructed
		// instance with no overrides as equal to the Default singleton, even
		// though they're distinct instances. This keeps callers from having
		// to compare against Default by reference.
		var fresh = new GlyphDrawingOptions();

		CornerstoneTest.NotSame(GlyphDrawingOptions.Default, fresh);
		CornerstoneTest.AreEqual(GlyphDrawingOptions.Default, fresh);
		CornerstoneTest.AreEqual(GlyphDrawingOptions.Default.GetHashCode(), fresh.GetHashCode());
	}

	[PresentationTestMethod]
	public void PixelSizeAcceptsNull()
	{
		var options = new GlyphDrawingOptions { PixelSize = null };

		CornerstoneTest.IsNull(options.PixelSize);
	}

	[PresentationTestMethod]
	[DataRow(1)]
	[DataRow(16)]
	[DataRow(256)]
	public void PixelSizeAcceptsPositiveValues(int value)
	{
		var options = new GlyphDrawingOptions { PixelSize = value };

		CornerstoneTest.AreEqual(value, options.PixelSize);
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	public void PixelSizeRejectsValuesBelowOne(int value)
	{
		// A pixel size of zero is meaningless for a bitmap strike; reject it at
		// construction time rather than letting it propagate to renderer code.
		Assert.Throws<ArgumentOutOfRangeException>(() => new GlyphDrawingOptions { PixelSize = value });
	}

	[PresentationTestMethod]
	public void WithExpressionProducesAModifiedCopy()
	{
		// `record` participation is part of the public contract; ensure callers
		// can use `with` to derive a tweaked instance.
		var original = new GlyphDrawingOptions { PaletteIndex = 1, PixelSize = 16 };
		var tweaked = original with { PaletteIndex = 2 };

		CornerstoneTest.AreEqual(1, original.PaletteIndex);
		CornerstoneTest.AreEqual(2, tweaked.PaletteIndex);
		CornerstoneTest.AreEqual(16, tweaked.PixelSize);
		CornerstoneTest.AreNotEqual(original, tweaked);
	}

	[PresentationTestMethod]
	public void WithExpressionValidatesNewValues()
	{
		var original = new GlyphDrawingOptions { PaletteIndex = 1 };

		Assert.Throws<ArgumentOutOfRangeException>(() => original with { PaletteIndex = -5 });
	}

	#endregion
}