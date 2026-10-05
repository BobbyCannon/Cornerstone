#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

[TestClass]
public class EmbeddedFontCollectionTests
{
	#region Constants

	private const string sfontAssets =
		"resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ShouldCacheNearestMatchForMiSans()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var source = new Uri(sfontAssets, UriKind.Absolute);

			var fontCollection = new TestEmbeddedFontCollection(source, source);

			// Font weight 304
			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("MiSans", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var regularGlyphTypeface));

			// Font weight regular (400)
			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("MiSans", FontStyle.Normal, FontWeight.Bold, FontStretch.Normal, out var boldGlyphTypeface));

			// Font weight 700
			CornerstoneTest.IsTrue(fontCollection.GlyphTypefaceCache.TryGetValue("MiSans", out var glyphTypefaces));

			CornerstoneTest.AreEqual(3, glyphTypefaces.Count);
		}
	}

	[PresentationTestMethod]
	public void ShouldCacheSyntheticGlyphTypeface()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
		{
			var key = new Uri("fonts:testFonts", UriKind.Absolute);
			var source = new Uri(sfontAssets, UriKind.Absolute);

			var fontCollection = new TestEmbeddedFontCollection(key, source, true);

			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("Manrope", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var glyphTypeface));

			CornerstoneTest.IsTrue(fontCollection.GlyphTypefaceCache.TryGetValue("Manrope", out var glyphTypefaces));

			CornerstoneTest.AreEqual(2, glyphTypefaces.Count);

			fontCollection.TryGetGlyphTypeface("Manrope", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var otherGlyphTypeface);

			CornerstoneTest.AreEqual(glyphTypeface, otherGlyphTypeface);
		}
	}

	[DataRow(FontWeight.SemiLight, FontStyle.Normal)]
	[DataRow(FontWeight.Bold, FontStyle.Italic)]
	[DataRow(FontWeight.Heavy, FontStyle.Oblique)]
	[PresentationTestMethod]
	public void ShouldGetNearMatchingTypeface(FontWeight fontWeight, FontStyle fontStyle)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
		{
			var key = new Uri("fonts:testFonts", UriKind.Absolute);
			var source = new Uri(sfontAssets, UriKind.Absolute);

			var fontCollection = new TestEmbeddedFontCollection(source, source);

			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("Noto Mono", fontStyle, fontWeight, FontStretch.Normal, out var glyphTypeface));

			var actual = glyphTypeface.FamilyName;

			CornerstoneTest.AreEqual("Noto Mono", actual);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTypefaceForPartialFamilyName()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
		{
			var key = new Uri("fonts:testFonts", UriKind.Absolute);
			var source = new Uri(sfontAssets, UriKind.Absolute);

			var fontCollection = new TestEmbeddedFontCollection(key, source);

			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("T", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var glyphTypeface));

			CornerstoneTest.AreEqual("Twitter Color Emoji", glyphTypeface.FamilyName);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetTypefaceForTypographicFamilyName()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
		{
			var key = new Uri("fonts:testFonts", UriKind.Absolute);
			var source = new Uri(sfontAssets, UriKind.Absolute);

			var fontCollection = new TestEmbeddedFontCollection(key, source);

			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("Manrope", FontStyle.Normal, FontWeight.Light, FontStretch.Normal, out var glyphTypeface));

			CornerstoneTest.AreEqual("Manrope Light", glyphTypeface.FamilyName);

			CornerstoneTest.AreEqual("Manrope", glyphTypeface.TypographicFamilyName);
		}
	}

	[PresentationTestMethod]
	public void ShouldNotGetTypefaceForInvalidFamilyName()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
		{
			var key = new Uri("fonts:testFonts", UriKind.Absolute);
			var source = new Uri(sfontAssets, UriKind.Absolute);

			var fontCollection = new TestEmbeddedFontCollection(key, source);

			CornerstoneTest.IsFalse(fontCollection.TryGetGlyphTypeface("ABC", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));
		}
	}

	#endregion

	#region Classes

	private class TestEmbeddedFontCollection : EmbeddedFontCollection
	{
		#region Fields

		private readonly bool _createSyntheticTypefaces;

		#endregion

		#region Constructors

		public TestEmbeddedFontCollection(Uri key, Uri source, bool createSyntheticTypefaces = false) : base(key, source)
		{
			_createSyntheticTypefaces = createSyntheticTypefaces;
		}

		#endregion

		#region Properties

		public IDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface>> GlyphTypefaceCache => _glyphTypefaceCache;

		#endregion

		#region Methods

		public override bool TryCreateSyntheticGlyphTypeface(
			GlyphTypeface glyphTypeface,
			FontStyle style,
			FontWeight weight,
			FontStretch stretch,
			[NotNullWhen(true)] out GlyphTypeface syntheticGlyphTypeface)
		{
			if (!_createSyntheticTypefaces)
			{
				syntheticGlyphTypeface = null;

				return false;
			}

			return base.TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, stretch, out syntheticGlyphTypeface);
		}

		#endregion
	}

	#endregion
}