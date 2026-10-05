#region References

using System;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Base.Media.Fonts.Tables;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

[TestClass]
public class HarfBuzzTextShaperTests
{
	#region Fields

	private readonly HarfBuzzTextShaper _shaper;

	private static readonly string sInterFontUri =
		"resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.Inter-Regular.ttf?assembly=Cornerstone.Presentation.UnitTests";

	#endregion

	#region Constructors

	public HarfBuzzTextShaperTests()
	{
		_shaper = new HarfBuzzTextShaper();
	}

	#endregion

	#region Properties

	private TestServices Services =>
		TestServices.MockThreadingInterface.With(
			textShaperImpl: _shaper);

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ShapeTextEndWithCRLFMergesBreakPair()
	{
		using (UnitTestApplication.Start(Services))
		{
			var text = "Line1\r\n".AsMemory();
			var options = CreateTextShaperOptions();

			var result = _shaper.ShapeText(text, options);

			CornerstoneTest.IsNotNull(result);
			CornerstoneTest.AreEqual(0.0, result[5].GlyphAdvance);
		}
	}

	[PresentationTestMethod]
	public void ShapeTextWithCRLFMergesBreakPair()
	{
		using (UnitTestApplication.Start(Services))
		{
			var text = "Line1\r\nLine2".AsMemory();
			var options = CreateTextShaperOptions();

			var result = _shaper.ShapeText(text, options);

			CornerstoneTest.IsNotNull(result);
			CornerstoneTest.AreNotEqual(0.0, result[5].GlyphAdvance);
		}
	}

	[PresentationTestMethod]
	public void ShapeTextWithEmptyStringReturnsEmptyShapedBuffer()
	{
		using (UnitTestApplication.Start(Services))
		{
			var text = "".AsMemory();
			var options = CreateTextShaperOptions();

			var result = _shaper.ShapeText(text, options);

			CornerstoneTest.IsNotNull(result);
			CornerstoneTest.AreEqual(0, result.Length);
		}
	}

	[PresentationTestMethod]
	public void ShapeTextWithSlicedMemoryClusterValuesAreSliceRelative()
	{
		using (UnitTestApplication.Start(Services))
		{
			var fullString = new string('A', 1000) + "Hello" + new string('B', 1000);
			var sliced = fullString.AsMemory().Slice(1000, 5);

			var options = CreateTextShaperOptions();

			var result = _shaper.ShapeText(sliced, options);

			CornerstoneTest.IsNotNull(result);
			CornerstoneTest.AreEqual(5, result.Length);

			for (var i = 0; i < result.Length; i++)
			{
				CornerstoneTest.IsTrue((result[i].GlyphCluster >= 0) && (result[i].GlyphCluster < 5), $"Glyph cluster at index {i} was {result[i].GlyphCluster}, expected a value in [0, 5).");
			}
		}
	}

	[PresentationTestMethod]
	public void ShapeTextWithTabCharacterReplacesWithSpace()
	{
		using (UnitTestApplication.Start(Services))
		{
			var text = "Hello\tWorld".AsMemory();
			var options = CreateTextShaperOptions();

			var result = _shaper.ShapeText(text, options);

			CornerstoneTest.IsNotNull(result);
			CornerstoneTest.IsTrue(result.Length == 11);
		}
	}

	[PresentationTestMethod]
	public void ShapeTextWithValidInputReturnsShapedBuffer()
	{
		using (UnitTestApplication.Start(Services))
		{
			var text = "Hello World".AsMemory();
			var options = CreateTextShaperOptions();

			var result = _shaper.ShapeText(text, options);

			CornerstoneTest.IsNotNull(result);
			CornerstoneTest.AreEqual(text.Length, result.Length);
		}
	}

	private TextShaperOptions CreateTextShaperOptions(
		sbyte bidiLevel = 0,
		double letterSpacing = 0,
		double fontSize = 16)
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		return new TextShaperOptions(
			typeface,
			fontSize,
			bidiLevel,
			letterSpacing: letterSpacing);
	}

	#endregion
}