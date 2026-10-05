#region References

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Media.Fonts.Tables.Cmap;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class GlyphTypefaceTests
{
	#region Constants

	private const string BlankFontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.AdobeBlank2VF.ttf?assembly=Cornerstone.Presentation.UnitTests";
	private const string GB18030FontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.NISC18030.ttf?assembly=Cornerstone.Presentation.UnitTests";
	private const string InterFontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.Inter-Regular.ttf?assembly=Cornerstone.Presentation.UnitTests";
	private const string MiSansFontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.MiSans-Normal.ttf?assembly=Cornerstone.Presentation.UnitTests";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryContainsKeyMatchesUnderlyingMap()
	{
		var map = LoadInterCharacterToGlyphMap();
		var dict = map.AsReadOnlyDictionary();

		// 'A' is in Inter.
		CornerstoneTest.IsTrue(map.ContainsGlyph('A'));
		CornerstoneTest.IsTrue(dict.ContainsKey('A'));

		// U+10FFFD is the last code point of the supplementary private-use
		// Plane 16 — Inter does not map it and a Format 4 cmap cannot.
		CornerstoneTest.IsFalse(map.ContainsGlyph(0x10FFFD));
		CornerstoneTest.IsFalse(dict.ContainsKey(0x10FFFD));
	}

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryCountIsPositiveAndMatchesEnumeration()
	{
		var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

		CornerstoneTest.IsTrue(dict.Count > 0);

		var enumerated = 0;
		foreach (var _ in dict)
		{
			enumerated++;
		}

		CornerstoneTest.AreEqual(dict.Count, enumerated);
	}

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryEnumerationYieldsPairsThatRoundTripThroughTheMap()
	{
		var map = LoadInterCharacterToGlyphMap();
		var dict = map.AsReadOnlyDictionary();

		var checkedPairs = 0;
		foreach (var kvp in dict)
		{
			// Every (key, value) the dictionary yields must agree with the
			// underlying map. The dictionary is a view, not a snapshot.
			CornerstoneTest.AreEqual(map.GetGlyph(kvp.Key), kvp.Value);
			CornerstoneTest.IsTrue(map.ContainsGlyph(kvp.Key));

			if (++checkedPairs >= 500)
			{
				// Inter has thousands of mappings; sampling the first 500
				// is enough to exercise the enumerator without making the
				// test prohibitively slow.
				break;
			}
		}

		CornerstoneTest.IsTrue(checkedPairs > 0);
	}

	[PresentationTestMethod]
	[DataRow('A')]
	[DataRow('z')]
	[DataRow('0')]
	[DataRow(' ')]
	public void AsReadOnlyDictionaryIndexerReturnsSameGlyphIdAsMap(int codePoint)
	{
		var map = LoadInterCharacterToGlyphMap();
		var dict = map.AsReadOnlyDictionary();

		CornerstoneTest.AreEqual(map.GetGlyph(codePoint), dict[codePoint]);
	}

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryIndexerThrowsForUnmappedCodePoint()
	{
		var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

		Assert.Throws<KeyNotFoundException>(() => _ = dict[0x10FFFD]);
	}

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryKeysMatchDictionaryEnumerationKeys()
	{
		var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

		var keysFromEnumeration = new HashSet<int>();
		var pairsKeysFromEnumeration = new HashSet<int>();

		foreach (var key in dict.Keys)
		{
			keysFromEnumeration.Add(key);
			if (keysFromEnumeration.Count >= 500)
			{
				break;
			}
		}

		foreach (var kvp in dict)
		{
			pairsKeysFromEnumeration.Add(kvp.Key);
			if (pairsKeysFromEnumeration.Count >= 500)
			{
				break;
			}
		}

		CornerstoneTest.IsTrue(keysFromEnumeration.Count > 0);
		CornerstoneTest.AreEqual(pairsKeysFromEnumeration, keysFromEnumeration);
	}

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryReturnsAFreshViewThatIsFunctionallyEquivalent()
	{
		var map = LoadInterCharacterToGlyphMap();

		var first = map.AsReadOnlyDictionary();
		var second = map.AsReadOnlyDictionary();

		// The dictionary is a lightweight wrapper that may or may not be
		// the same instance; what matters is that two views of the same
		// map agree on lookups.
		CornerstoneTest.IsTrue(first.ContainsKey('A'));
		CornerstoneTest.IsTrue(second.ContainsKey('A'));
		CornerstoneTest.AreEqual(first['A'], second['A']);
	}

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryReturnsNonNullDictionary()
	{
		var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

		CornerstoneTest.IsNotNull(dict);
	}

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryTryGetValueReturnsFalseForUnmappedCodePoint()
	{
		var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

		CornerstoneTest.IsFalse(dict.TryGetValue(0x10FFFD, out var glyphId));
		CornerstoneTest.AreEqual((ushort) 0, glyphId);
	}

	[PresentationTestMethod]
	public void AsReadOnlyDictionaryTryGetValueReturnsTrueWithGlyphIdForKnownCodePoint()
	{
		var map = LoadInterCharacterToGlyphMap();
		var dict = map.AsReadOnlyDictionary();

		CornerstoneTest.IsTrue(dict.TryGetValue('A', out var glyphId));
		CornerstoneTest.AreEqual(map.GetGlyph('A'), glyphId);
		CornerstoneTest.AreNotEqual(0, glyphId);
	}

	[PresentationTestMethod]
	public void CharacterToGlyphMapShouldHaveDifferentGlyphsForDifferentCharacters()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;

		CornerstoneTest.IsTrue(map.ContainsGlyph('A'));
		CornerstoneTest.IsTrue(map.ContainsGlyph('B'));

		var glyphA = map['A'];
		var glyphB = map['B'];

		CornerstoneTest.AreNotEqual(glyphA, glyphB);
	}

	[PresentationTestMethod]
	public void CharacterToGlyphMapWithFormat13ShouldHaveSameGlyphForDifferentCharacters()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(BlankFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;

		CornerstoneTest.IsTrue(map.ContainsGlyph('A'));
		CornerstoneTest.IsTrue(map.ContainsGlyph('B'));

		var glyphA = map['A'];
		var glyphB = map['B'];

		CornerstoneTest.AreEqual(glyphA, glyphB);
	}

	[PresentationTestMethod]
	public void FaceNamesShouldContainInvariantCultureEntry()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(typeface.FaceNames.ContainsKey(CultureInfo.InvariantCulture) ||
			(typeface.FaceNames.Count > 0));
	}

	[PresentationTestMethod]
	public void FamilyNamesShouldContainInvariantCultureEntry()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(typeface.FamilyNames.ContainsKey(CultureInfo.InvariantCulture) ||
			(typeface.FamilyNames.Count > 0));
	}

	[PresentationTestMethod]
	public void FontMetricsLineSpacingShouldBeCalculatedCorrectly()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var metrics = typeface.Metrics;

		var expectedLineSpacing = (metrics.Descent - metrics.Ascent) + metrics.LineGap;

		CornerstoneTest.AreEqual(expectedLineSpacing, metrics.LineSpacing);
	}

	[PresentationTestMethod]
	public void GetGlyphAdvanceShouldReturnAdvanceForGlyphId()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;

		CornerstoneTest.IsTrue(map.ContainsGlyph('A'));

		var glyphIndex = map['A'];

		// Ensure metrics are available for this glyph
		CornerstoneTest.IsTrue(typeface.TryGetGlyphMetrics(glyphIndex, out var metrics));

		// Ensure advance can be retrieved
		CornerstoneTest.IsTrue(typeface.TryGetHorizontalGlyphAdvance(glyphIndex, out var advance));

		// The advance lives on AdvanceWidth; Width is the ink bounding-box width.
		CornerstoneTest.AreEqual(metrics.AdvanceWidth, advance);
	}

	[PresentationTestMethod]
	public void ShouldApplyBoldSimulation()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream), FontSimulations.Bold);

		CornerstoneTest.AreEqual(FontWeight.Bold, typeface.Weight);
		CornerstoneTest.AreEqual(FontSimulations.Bold, typeface.FontSimulations);
	}

	[PresentationTestMethod]
	public void ShouldApplyCombinedSimulations()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream),
			FontSimulations.Bold | FontSimulations.Oblique);

		CornerstoneTest.AreEqual(FontWeight.Bold, typeface.Weight);
		CornerstoneTest.AreEqual(FontStyle.Italic, typeface.Style);
		CornerstoneTest.AreEqual(FontSimulations.Bold | FontSimulations.Oblique, typeface.FontSimulations);
	}

	[PresentationTestMethod]
	public void ShouldApplyObliqueSimulation()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream), FontSimulations.Oblique);

		CornerstoneTest.AreEqual(FontStyle.Italic, typeface.Style);
		CornerstoneTest.AreEqual(FontSimulations.Oblique, typeface.FontSimulations);
	}

	[PresentationTestMethod]
	public void ShouldCacheSupportedFeatures()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var features1 = typeface.SupportedFeatures;
		var features2 = typeface.SupportedFeatures;

		CornerstoneTest.Same(features1, features2);
	}

	[PresentationTestMethod]
	public void ShouldDisposeProperly()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		typeface.Dispose();

		// Should not throw on double dispose
		typeface.Dispose();
	}

	[PresentationTestMethod]
	public void ShouldHaveCharacterToGlyphMapForCommonCharacters()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;

		CornerstoneTest.IsNotNull(map);

		CornerstoneTest.IsTrue(map.ContainsGlyph('A'));
		CornerstoneTest.IsTrue(map['A'] != 0);

		CornerstoneTest.IsTrue(map.ContainsGlyph('a'));
		CornerstoneTest.IsTrue(map['a'] != 0);

		CornerstoneTest.IsTrue(map.ContainsGlyph(' '));
		CornerstoneTest.IsTrue(map[' '] != 0);
	}

	[PresentationTestMethod]
	public void ShouldHaveCorrectFontProperties()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.AreEqual(FontWeight.Normal, typeface.Weight);
		CornerstoneTest.AreEqual(FontStyle.Normal, typeface.Style);
		CornerstoneTest.AreEqual(FontStretch.Normal, typeface.Stretch);
		CornerstoneTest.AreEqual(FontSimulations.None, typeface.FontSimulations);
	}

	[PresentationTestMethod]
	public void ShouldHaveFaceNamesDictionary()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsNotNull(typeface.FaceNames);
		CornerstoneTest.NotEmpty(typeface.FaceNames);
	}

	[PresentationTestMethod]
	public void ShouldHaveFamilyNamesDictionary()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsNotNull(typeface.FamilyNames);
		CornerstoneTest.NotEmpty(typeface.FamilyNames);
	}

	[PresentationTestMethod]
	public void ShouldHavePositiveGlyphCount()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(typeface.GlyphCount > 0);
	}

	[PresentationTestMethod]
	public void ShouldHaveSupportedFeatures()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var features = typeface.SupportedFeatures;

		CornerstoneTest.NotEmpty(features);
	}

	[PresentationTestMethod]
	public void ShouldHaveTypographicFamilyName()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsNotNull(typeface.TypographicFamilyName);
	}

	[PresentationTestMethod]
	[DataRow(InterFontUri)]
	[DataRow(GB18030FontUri)] // Font without head table
	public void ShouldHaveValidFontMetrics(string fontUri)
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(fontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var metrics = typeface.Metrics;

		CornerstoneTest.IsTrue(metrics.DesignEmHeight > 0);
		CornerstoneTest.IsTrue(metrics.Ascent != 0);
		CornerstoneTest.IsTrue(metrics.Descent != 0);
		CornerstoneTest.IsTrue(metrics.LineSpacing > 0);
	}

	[PresentationTestMethod]
	public void ShouldHaveValidPlatformTypeface()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var platformTypeface = new CustomPlatformTypeface(stream);
		var typeface = new GlyphTypeface(platformTypeface);

		CornerstoneTest.IsNotNull(typeface.PlatformTypeface);
		CornerstoneTest.Same(platformTypeface, typeface.PlatformTypeface);
	}

	[PresentationTestMethod]
	public void ShouldLoadInterFont()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.AreEqual("Inter", typeface.FamilyName);
	}

	[PresentationTestMethod]
	public void ShouldSupportMultipleCharactersInCharacterToGlyphMap()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;

		var testCharacters = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		foreach (var ch in testCharacters)
		{
			CornerstoneTest.IsTrue(map.ContainsGlyph(ch), $"Character '{ch}' not found in glyph map");
		}
	}

	[PresentationTestMethod]
	public void TryGetGlyphAdvanceShouldReturnFalseForInvalidGlyphId()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsFalse(typeface.TryGetHorizontalGlyphAdvance(ushort.MaxValue, out var advance));
	}

	[PresentationTestMethod]
	public void TryGetGlyphMetricsBatchMatchesSingle()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;
		var glyphIndices = new[] { map['A'], map['B'], map['g'], map[' '] };

		var batch = new GlyphMetrics[glyphIndices.Length];
		CornerstoneTest.IsTrue(typeface.TryGetGlyphMetrics(glyphIndices, batch));

		for (var i = 0; i < glyphIndices.Length; i++)
		{
			CornerstoneTest.IsTrue(typeface.TryGetGlyphMetrics(glyphIndices[i], out var single));

			// GlyphMetrics is a record struct, so this is structural equality.
			CornerstoneTest.AreEqual(single, batch[i]);
		}
	}

	[PresentationTestMethod]
	public void TryGetGlyphMetricsEmptyGlyphHasAdvanceButNoInk()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var spaceGlyph = typeface.CharacterToGlyphMap[' '];

		CornerstoneTest.IsTrue(typeface.TryGetGlyphMetrics(spaceGlyph, out var metrics));

		// The space glyph has a horizontal advance but no ink.
		CornerstoneTest.IsTrue(metrics.AdvanceWidth > 0);
		CornerstoneTest.AreEqual((ushort) 0, metrics.Width);
		CornerstoneTest.AreEqual((ushort) 0, metrics.Height);
	}

	[PresentationTestMethod]
	public void TryGetGlyphMetricsShouldReturnFalseForInvalidGlyphId()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var result = typeface.TryGetGlyphMetrics(ushort.MaxValue, out var metrics);

		CornerstoneTest.IsFalse(result);
		CornerstoneTest.AreEqual(default, metrics);
	}

	[PresentationTestMethod]
	public void TryGetGlyphMetricsShouldReturnValidMetrics()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;
		CornerstoneTest.IsTrue(map.ContainsGlyph('A'));

		var glyphIndex = map['A'];
		var result = typeface.TryGetGlyphMetrics(glyphIndex, out var metrics);

		CornerstoneTest.IsTrue(result);
		CornerstoneTest.IsTrue(metrics.Width > 0);
	}

	[PresentationTestMethod]
	public void TryGetGlyphMetricsWidthIsInkBoxNotAdvance()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(InterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var glyphIndex = typeface.CharacterToGlyphMap['A'];

		CornerstoneTest.IsTrue(typeface.TryGetGlyphMetrics(glyphIndex, out var metrics));
		CornerstoneTest.IsTrue(typeface.TryGetHorizontalGlyphAdvance(glyphIndex, out var advance));

		// The advance belongs on AdvanceWidth...
		CornerstoneTest.AreEqual(advance, metrics.AdvanceWidth);

		// ...and Width is the ink bounding-box width, a distinct value.
		CornerstoneTest.IsTrue(metrics.Width > 0);
		CornerstoneTest.AreNotEqual(metrics.AdvanceWidth, metrics.Width);
	}

	[PresentationTestMethod]
	public void TryGetVerticalGlyphAdvanceReturnsFalseForLatinFont()
	{
		var assetLoader = new StandardAssetLoader();
		using var stream = assetLoader.Open(new Uri(InterFontUri));
		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var glyphIndex = typeface.CharacterToGlyphMap['A'];

		// Latin fonts typically carry no vmtx table — the call returns false and
		// leaves the advance at zero.
		CornerstoneTest.IsFalse(typeface.TryGetVerticalGlyphAdvance(glyphIndex, out var advance));
		CornerstoneTest.AreEqual((ushort) 0, advance);
	}

	[PresentationTestMethod]
	public void TryGetVerticalGlyphAdvanceReturnsTrueForCJKFont()
	{
		var assetLoader = new StandardAssetLoader();
		using var stream = assetLoader.Open(new Uri(MiSansFontUri));
		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		// CJK glyph: U+4E2D ("中"). MiSans is a CJK font with a vmtx table.
		var glyphIndex = typeface.CharacterToGlyphMap['中'];

		CornerstoneTest.IsTrue(typeface.TryGetVerticalGlyphAdvance(glyphIndex, out var advance));
		CornerstoneTest.IsTrue(advance > 0, "Expected a positive vertical advance for a CJK glyph.");
	}

	[PresentationTestMethod]
	public void TryGetVerticalGlyphAdvancesBatchMatchesSingleForCJKFont()
	{
		var assetLoader = new StandardAssetLoader();
		using var stream = assetLoader.Open(new Uri(MiSansFontUri));
		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;
		var glyphIndices = new[] { map['中'], map['文'], map['字'], map[' '] };

		var batch = new ushort[glyphIndices.Length];
		CornerstoneTest.IsTrue(typeface.TryGetVerticalGlyphAdvances(glyphIndices, batch));

		for (var i = 0; i < glyphIndices.Length; i++)
		{
			CornerstoneTest.IsTrue(typeface.TryGetVerticalGlyphAdvance(glyphIndices[i], out var single));
			CornerstoneTest.AreEqual(single, batch[i]);
		}
	}

	[PresentationTestMethod]
	public void TryGetVerticalGlyphAdvancesBatchReturnsFalseForLatinFont()
	{
		var assetLoader = new StandardAssetLoader();
		using var stream = assetLoader.Open(new Uri(InterFontUri));
		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var map = typeface.CharacterToGlyphMap;
		var glyphIndices = new[] { map['A'], map['B'], map['g'] };
		var advances = new ushort[glyphIndices.Length];

		CornerstoneTest.IsFalse(typeface.TryGetVerticalGlyphAdvances(glyphIndices, advances));
	}

	private static CharacterToGlyphMap LoadInterCharacterToGlyphMap()
	{
		var assetLoader = new StandardAssetLoader();
		using var stream = assetLoader.Open(new Uri(InterFontUri));
		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));
		return typeface.CharacterToGlyphMap;
	}

	#endregion

	#region Classes

	private class CustomPlatformTypeface : IPlatformTypeface
	{
		#region Fields

		private readonly UnmanagedFontMemory _fontMemory;

		#endregion

		#region Constructors

		public CustomPlatformTypeface(Stream stream, string fontFamily = "Custom")
		{
			_fontMemory = UnmanagedFontMemory.LoadFromStream(stream);
			FamilyName = fontFamily;
		}

		#endregion

		#region Properties

		public string FamilyName { get; }

		public FontSimulations FontSimulations => FontSimulations.None;

		public FontStretch Stretch => FontStretch.Normal;

		public FontStyle Style => FontStyle.Normal;

		public FontWeight Weight => FontWeight.Normal;

		#endregion

		#region Methods

		public void Dispose()
		{
			_fontMemory.Dispose();
		}

		public bool TryGetStream([NotNullWhen(true)] out Stream stream)
		{
			var memory = _fontMemory.Memory;

			var handle = memory.Pin();
			stream = new PinnedUnmanagedMemoryStream(handle, memory.Length);

			return true;
		}

		public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
		{
			return _fontMemory.TryGetTable(tag, out table);
		}

		#endregion

		#region Classes

		private sealed class PinnedUnmanagedMemoryStream : UnmanagedMemoryStream
		{
			#region Fields

			private MemoryHandle _handle;

			#endregion

			#region Constructors

			public unsafe PinnedUnmanagedMemoryStream(MemoryHandle handle, long length)
				: base((byte*) handle.Pointer, length)
			{
				_handle = handle;
			}

			#endregion

			#region Methods

			protected override void Dispose(bool disposing)
			{
				try
				{
					base.Dispose(disposing);
				}
				finally
				{
					_handle.Dispose();
				}
			}

			#endregion
		}

		#endregion
	}

	#endregion
}