#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

/// <summary>
/// Verifies that <see cref="FontCollectionBase.TryMatchCharacter(int, FontStyle, FontWeight, FontStretch, string, CultureInfo, out Typeface)" /> resolves the same family
/// regardless of the order in which fonts were added to the collection, and is stable
/// across repeated invocations.
/// </summary>
[TestClass]
public class FontCollectionDeterminismTests
{
	#region Constants

	private const string AssetsNamespace = "Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts";

	#endregion

	#region Fields

	// A set of Latin-covering test fonts. All cover ASCII 'A'.
	private static readonly string[] slatinFontAssets =
	{
		$"{AssetsNamespace}.Inter-Regular.ttf",
		$"{AssetsNamespace}.Inter-Bold.ttf",
		$"{AssetsNamespace}.Manrope-Light.ttf",
		$"{AssetsNamespace}.NotoMono-Regular.ttf",
		$"{AssetsNamespace}.NotoSans-Italic.ttf",
		$"{AssetsNamespace}.SourceSerif4_36pt-Italic.ttf"
	};

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void TryMatchCharacterCachedScriptFallbackLookupReturnsSameFamily()
	{
		using var app = UnitTestApplication.Start(
			TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

		var collection = BuildCollection(slatinFontAssets);

		// First call populates the script/culture fallback cache; subsequent calls must hit
		// it and still return the same family.
		CornerstoneTest.IsTrue(collection.TryMatchCharacter(
			'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
			null, null, out var first));

		CornerstoneTest.IsTrue(collection.TryMatchCharacter(
			'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
			null, null, out var second));

		CornerstoneTest.AreEqual(ExtractFamilyName(first), ExtractFamilyName(second));
	}

	[PresentationTestMethod]
	public void TryMatchCharacterIsStableAcrossRepeatedInvocations()
	{
		using var app = UnitTestApplication.Start(
			TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

		var collection = BuildCollection(slatinFontAssets);

		CornerstoneTest.IsTrue(collection.TryMatchCharacter(
			'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
			null, null, out var firstMatch));

		var expected = ExtractFamilyName(firstMatch);

		for (var i = 0; i < 50; i++)
		{
			CornerstoneTest.IsTrue(collection.TryMatchCharacter(
				'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
				null, null, out var match));

			CornerstoneTest.AreEqual(expected, ExtractFamilyName(match));
		}
	}

	[PresentationTestMethod]
	public void TryMatchCharacterResultIsIndependentOfConcurrentCachePopulation()
	{
		using var app = UnitTestApplication.Start(
			TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

		// Touch a variety of typefaces in different orders before the fallback call,
		// so each collection's _glyphTypefaceCache has different ConcurrentDictionary
		// insertion / hash-bucket order.
		var resultsA = new List<string>();
		var resultsB = new List<string>();

		for (var i = 0; i < 5; i++)
		{
			var a = BuildCollection(slatinFontAssets);
			var b = BuildCollection(slatinFontAssets);

			// Different warm-up order on purpose.
			Warmup(a, new[] { "Inter", "Manrope Light", "Noto Mono", "Noto Sans", "Source Serif 4 36pt" });
			Warmup(b, new[] { "Source Serif 4 36pt", "Noto Sans", "Noto Mono", "Manrope Light", "Inter" });

			CornerstoneTest.IsTrue(a.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var ma));
			CornerstoneTest.IsTrue(b.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var mb));

			resultsA.Add(ExtractFamilyName(ma));
			resultsB.Add(ExtractFamilyName(mb));
		}

		// Every iteration of A produces the same family.
		CornerstoneTest.Single(resultsA.Distinct());

		// Every iteration of B produces the same family.
		CornerstoneTest.Single(resultsB.Distinct());

		// And the chosen family is the same for both warm-up orders.
		CornerstoneTest.AreEqual(resultsA[0], resultsB[0]);
	}

	[PresentationTestMethod]
	public void TryMatchCharacterReturnsSameFamilyRegardlessOfAddOrder()
	{
		using var app = UnitTestApplication.Start(
			TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

		var orderings = new[]
		{
			slatinFontAssets,
			slatinFontAssets.Reverse().ToArray(),
			Shuffle(slatinFontAssets, 1),
			Shuffle(slatinFontAssets, 17),
			Shuffle(slatinFontAssets, 42),
			Shuffle(slatinFontAssets, 1337)
		};

		string expected = null;

		foreach (var ordering in orderings)
		{
			var collection = BuildCollection(ordering);

			CornerstoneTest.IsTrue(collection.TryMatchCharacter(
				'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
				null, null, out var match));

			var familyName = ExtractFamilyName(match);

			if (expected is null)
			{
				expected = familyName;
			}
			else
			{
				CornerstoneTest.AreEqual(expected, familyName);
			}
		}
	}

	private static CustomFontCollection BuildCollection(IEnumerable<string> assetPaths)
	{
		var assetLoader = PresentationLocator.Current.GetRequiredService<IAssetLoader>();
		var collection = new CustomFontCollection(new Uri("fonts:determinism", UriKind.Absolute));

		foreach (var path in assetPaths)
		{
			var uri = new Uri($"resm:{path}?assembly=Cornerstone.Presentation.UnitTests", UriKind.Absolute);

			using var stream = assetLoader.Open(uri);

			CornerstoneTest.IsTrue(collection.TryAddGlyphTypeface(stream, out _));
		}

		return collection;
	}

	private static string ExtractFamilyName(Typeface typeface)
	{
		// Fallback Typefaces are built with a FontFamily of the form "<collection-key>#<familyName>".
		// The plain family name is what we want to compare across orderings.
		var name = typeface.FontFamily.Name;
		var hashIndex = name.LastIndexOf('#');
		return hashIndex >= 0 ? name[(hashIndex + 1)..] : name;
	}

	private static string[] Shuffle(string[] source, int seed)
	{
		var rng = new Random(seed);
		var copy = (string[]) source.Clone();

		for (var i = copy.Length - 1; i > 0; i--)
		{
			var j = rng.Next(i + 1);
			(copy[i], copy[j]) = (copy[j], copy[i]);
		}

		return copy;
	}

	private static void Warmup(CustomFontCollection collection, IEnumerable<string> familyNames)
	{
		foreach (var name in familyNames)
		{
			collection.TryGetGlyphTypeface(name, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _);
		}
	}

	#endregion

	#region Classes

	private sealed class CustomFontCollection(Uri key) : FontCollectionBase
	{
		#region Properties

		public override Uri Key { get; } = key;

		#endregion
	}

	#endregion
}