#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

[TestClass]
public class FontCollectionTests
{
	#region Constants

	private const string NotoMono =
		"resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests";

	#endregion

	#region Methods

	[Win32TestMethod("Relies on some installed font family")]
	public void ShouldCacheNearestMatch()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var fontCollection = new TestSystemFontCollection(FontManager.Current.PlatformImpl);

			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("Arial", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var glyphTypeface));

			CornerstoneTest.IsTrue(fontCollection.GlyphTypefaceCache.TryGetValue("Arial", out var glyphTypefaces));

			CornerstoneTest.AreEqual(2, glyphTypefaces.Count);

			CornerstoneTest.IsTrue(glyphTypefaces.ContainsKey(new FontCollectionKey(FontStyle.Normal, FontWeight.Black, FontStretch.Normal)));

			fontCollection.TryGetGlyphTypeface("Arial", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var otherGlyphTypeface);

			CornerstoneTest.AreEqual(glyphTypeface, otherGlyphTypeface);
		}
	}

	[PresentationTestMethod]
	public void ShouldCacheSyntheticMatchUnderRequestedFamilyName()
	{
		var fontManager = new AliasFontManagerImpl("MyAlias");

		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
		{
			var fontCollection = new TestSystemFontCollection(fontManager);
			var blackKey = new FontCollectionKey(FontStyle.Normal, FontWeight.Black, FontStretch.Normal);

			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface(
				"MyAlias", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface(
				"MyAlias", FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var first));

			CornerstoneTest.AreEqual(FontSimulations.Bold, first.FontSimulations);

			var creationsAfterFirstCall = fontManager.StreamTypefaceCreations;

			for (var i = 0; i < 10; i++)
			{
				CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface(
					"MyAlias", FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var next));

				CornerstoneTest.Same(first, next);
			}

			CornerstoneTest.AreEqual(creationsAfterFirstCall, fontManager.StreamTypefaceCreations);

			CornerstoneTest.IsTrue(fontCollection.GlyphTypefaceCache.TryGetValue("MyAlias", out var cached));
			CornerstoneTest.IsTrue(cached.ContainsKey(blackKey));
		}
	}

	[PresentationTestMethod]
	public void ShouldIgnoreFontFamily()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
		{
			var key = new Uri(NotoMono, UriKind.Absolute);

			var ignorable = new FontFamily(new Uri(NotoMono, UriKind.Absolute), "Noto Mono");

			var fontCollection = new CustomizableFontCollection(key, key, null, new[] { ignorable });

			var typeface = new Typeface(ignorable);

			var glyphTypeface = typeface.GlyphTypeface;

			CornerstoneTest.IsFalse(fontCollection.TryCreateSyntheticGlyphTypeface(
				typeface.GlyphTypeface,
				FontStyle.Italic,
				FontWeight.DemiBold,
				FontStretch.Normal,
				out var syntheticGlyphTypeface));
		}
	}

	[PresentationTestMethod]
	public void ShouldUseFallback()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
		{
			var source = new Uri(NotoMono, UriKind.Absolute);

			var fallback = new FontFallback { FontFamily = new FontFamily("Arial"), UnicodeRange = new UnicodeRange('A', 'A') };

			var fontCollection = new CustomizableFontCollection(source, source, new[] { fallback });

			CornerstoneTest.IsTrue(fontCollection.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var match));

			CornerstoneTest.AreEqual("Arial", match.FontFamily.Name);
		}
	}

	#endregion

	#region Classes

	/// <summary>
	/// Font manager whose MyAlias family resolves through the platform but is absent from
	/// the installed family list.
	/// </summary>
	private sealed class AliasFontManagerImpl : IFontManagerImpl
	{
		#region Constants

		private const string BackingFontUri =
			"resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.NotoMono-Regular.ttf?assembly=Cornerstone.Presentation.UnitTests";

		#endregion

		#region Fields

		private readonly string _alias;

		private readonly IFontManagerImpl _inner = new FontManagerImpl();

		#endregion

		#region Constructors

		public AliasFontManagerImpl(string alias)
		{
			_alias = alias;
		}

		#endregion

		#region Properties

		public int StreamTypefaceCreations { get; private set; }

		#endregion

		#region Methods

		public string GetDefaultFontFamilyName()
		{
			return _inner.GetDefaultFontFamilyName();
		}

		public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
		{
			return Array.Empty<string>();
		}

		public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
			FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface platformTypeface)
		{
			if (string.Equals(familyName, _alias, StringComparison.OrdinalIgnoreCase))
			{
				using var stream = OpenBackingFont();

				return _inner.TryCreateGlyphTypeface(stream, FontSimulations.None, out platformTypeface);
			}

			platformTypeface = null;

			return false;
		}

		public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
			[NotNullWhen(true)] out IPlatformTypeface platformTypeface)
		{
			StreamTypefaceCreations++;

			return _inner.TryCreateGlyphTypeface(stream, fontSimulations, out platformTypeface);
		}

		public bool TryGetFamilyTypefaces(string familyName,
			[NotNullWhen(true)] out IReadOnlyList<Typeface> familyTypefaces)
		{
			return _inner.TryGetFamilyTypefaces(familyName, out familyTypefaces);
		}

		public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
			FontStretch fontStretch, string familyName, CultureInfo culture,
			[NotNullWhen(true)] out IPlatformTypeface platformTypeface)
		{
			return _inner.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName,
				culture, out platformTypeface);
		}

		private static Stream OpenBackingFont()
		{
			var assetLoader = PresentationLocator.Current.GetRequiredService<IAssetLoader>();

			return assetLoader.Open(new Uri(BackingFontUri, UriKind.Absolute));
		}

		#endregion
	}

	private class CustomizableFontCollection : EmbeddedFontCollection
	{
		#region Fields

		private readonly IReadOnlyList<FontFallback> _fallbacks;
		private readonly IReadOnlyList<FontFamily> _ignorables;

		#endregion

		#region Constructors

		public CustomizableFontCollection(Uri key, Uri source, IReadOnlyList<FontFallback> fallbacks = null, IReadOnlyList<FontFamily> ignorables = null) : base(key, source)
		{
			_fallbacks = fallbacks;
			_ignorables = ignorables;
		}

		#endregion

		#region Methods

		public override bool TryCreateSyntheticGlyphTypeface(
			GlyphTypeface glyphTypeface,
			FontStyle style,
			FontWeight weight,
			FontStretch stretch,
			[NotNullWhen(true)] out GlyphTypeface syntheticGlyphTypeface)
		{
			syntheticGlyphTypeface = null;

			if (_ignorables is not null)
			{
				foreach (var ignorable in _ignorables)
				{
					if ((glyphTypeface.FamilyName == ignorable.Name) || (glyphTypeface.TypographicFamilyName == ignorable.Name))
					{
						return false;
					}
				}
			}

			return base.TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, stretch, out syntheticGlyphTypeface);
		}

		public override bool TryMatchCharacter(
			int codepoint,
			FontStyle style,
			FontWeight weight,
			FontStretch stretch,
			string familyName,
			CultureInfo culture,
			out Typeface match)
		{
			if (_fallbacks is not null)
			{
				foreach (var fallback in _fallbacks)
				{
					if (fallback.UnicodeRange.IsInRange(codepoint))
					{
						match = new Typeface(fallback.FontFamily, style, weight, stretch);

						return true;
					}
				}
			}

			return base.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out match);
		}

		#endregion
	}

	private class TestSystemFontCollection : SystemFontCollection
	{
		#region Constructors

		public TestSystemFontCollection(IFontManagerImpl platformImpl) : base(platformImpl)
		{
		}

		#endregion

		#region Properties

		public IDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface>> GlyphTypefaceCache => _glyphTypefaceCache;

		#endregion
	}

	#endregion
}