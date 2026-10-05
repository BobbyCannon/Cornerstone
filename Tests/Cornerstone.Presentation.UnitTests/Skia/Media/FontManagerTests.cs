#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

[TestClass]
public class FontManagerTests
{
	#region Fields

	private static readonly string sfontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Noto Mono";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ShouldCacheMatchCharacter()
	{
		var fontManagerImpl = new CustomFontManagerImpl();

		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManagerImpl)))
		{
			var emoji = Codepoint.ReadAt("😀", 0, out _);

			CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(emoji, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var firstMatch));

			var firstGlyphTypeface = firstMatch.GlyphTypeface;

			CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(emoji, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var secondMatch));

			var secondGlyphTypeface = secondMatch.GlyphTypeface;

			CornerstoneTest.AreEqual(firstGlyphTypeface, secondGlyphTypeface);
		}
	}

	[PresentationTestMethod]
	public void ShouldCreateSyntheticTypeface()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				FontManager.Current.AddFontCollection(new EmbeddedFontCollection(FontManager.SystemFontsKey,
					new Uri(sfontUri, UriKind.Absolute)));

				CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono", FontStyle.Italic, FontWeight.Bold),
					out var italicBoldTypeface));

				CornerstoneTest.AreEqual("Noto Mono", italicBoldTypeface.FamilyName);

				CornerstoneTest.IsTrue(italicBoldTypeface.PlatformTypeface.FontSimulations.HasFlag(FontSimulations.Bold));

				CornerstoneTest.IsTrue(italicBoldTypeface.PlatformTypeface.FontSimulations.HasFlag(FontSimulations.Oblique));

				CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono", FontStyle.Normal, FontWeight.Normal),
					out var regularTypeface));

				CornerstoneTest.AreNotEqual(((SkiaTypeface) regularTypeface.PlatformTypeface).SKTypeface, ((SkiaTypeface) italicBoldTypeface.PlatformTypeface).SKTypeface);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldCreateTypefaceFromFallback()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var fontManager = FontManager.Current;

			var glyphTypeface = new Typeface(new FontFamily("A, B, " + FontFamily.DefaultFontFamilyName)).GlyphTypeface;

			CornerstoneTest.AreEqual(SKTypeface.Default.FamilyName, glyphTypeface.FamilyName);
		}
	}

	[PresentationTestMethod]
	public void ShouldCreateTypefaceFromFallbackBold()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var glyphTypeface = new Typeface(new FontFamily("A, B, Arial"), weight: FontWeight.Bold).GlyphTypeface;

			CornerstoneTest.IsTrue((int) glyphTypeface.Weight >= 600);
		}
	}

	[PresentationTestMethod]
	public void ShouldFallbackWhenFontFamilyIsEmpty()
	{
		using (UnitTestApplication.Start(
					TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				var typeface = new Typeface(string.Empty);
				CornerstoneTest.IsNotNull(typeface.FontFamily);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetFamilyTypefaces()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				FontManager.Current.AddFontCollection(new InterFontCollection());

				var familyTypefaces = FontManager.Current.GetFamilyTypefaces(new FontFamily("fonts:Inter#Inter"));

				CornerstoneTest.AreEqual(2, familyTypefaces.Count);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetFontFeatures()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				FontManager.Current.AddFontCollection(new InterFontCollection());

				CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("fonts:Inter#Inter"),
					out var glyphTypeface));

				CornerstoneTest.AreEqual("Inter", glyphTypeface.FamilyName);

				var features = glyphTypeface.SupportedFeatures;

				CornerstoneTest.NotEmpty(features);
			}
		}
	}

	[Win32TestMethod("Requires Windows Fonts")]
	public void ShouldGetGlyphTypefaceByLocalizedFamilyName()
	{
		using (UnitTestApplication.Start(
					TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("微軟正黑體"), out var glyphTypeface));

				CornerstoneTest.AreEqual("Microsoft JhengHei", glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetImplicitTypeface()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				FontManager.Current.AddFontCollection(new EmbeddedFontCollection(FontManager.SystemFontsKey,
					new Uri(sfontUri, UriKind.Absolute)));

				CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono Italic"),
					out var glyphTypeface));

				CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);

				CornerstoneTest.AreEqual(FontStyle.Italic, glyphTypeface.Style);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetNearestMatchForCustomSystemFont()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				FontManager.Current.AddFontCollection(new EmbeddedFontCollection(FontManager.SystemFontsKey,
					new Uri(sfontUri, UriKind.Absolute)));

				CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono", FontStyle.Italic), out var glyphTypeface));

				CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);
			}
		}
	}

	[Win32TestMethod("Windows specific font")]
	public void ShouldGetRegularFontAfterMatchingItalicFont()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter('こ', FontStyle.Italic, FontWeight.Normal, FontStretch.Normal, null, null, out var italicTypeface));

				CornerstoneTest.AreEqual(FontSimulations.None, italicTypeface.GlyphTypeface.FontSimulations);

				CornerstoneTest.AreEqual("Yu Gothic UI", italicTypeface.GlyphTypeface.FamilyName);

				CornerstoneTest.AreNotEqual(FontStyle.Normal, italicTypeface.Style);

				CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter('こ', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var regularTypeface));

				CornerstoneTest.AreEqual("Yu Gothic UI", regularTypeface.GlyphTypeface.FamilyName);

				CornerstoneTest.AreEqual(FontStyle.Normal, regularTypeface.Style);

				CornerstoneTest.AreNotEqual(((SkiaTypeface) italicTypeface.GlyphTypeface.PlatformTypeface).SKTypeface, ((SkiaTypeface) regularTypeface.GlyphTypeface.PlatformTypeface).SKTypeface);
			}
		}
	}

	[DataRow("Arial")]
	[DataRow("#Arial")]
	[Win32TestMethod("Windows specific font")]
	public void ShouldGetSystemFontWithBaseUri(string name)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				var fontFamily = new FontFamily(new Uri("csres://Cornerstone.Presentation.UnitTests.Skia/NotFound"), name);

				var glyphTypeface = new Typeface(fontFamily).GlyphTypeface;

				CornerstoneTest.AreEqual("Arial", glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldLoadEmbeddedDefaultFontFamily()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				PresentationLocator.CurrentMutable.BindToSelf(new FontManagerOptions { DefaultFamilyName = sfontUri });

				var result = FontManager.Current.TryGetGlyphTypeface(Typeface.Default, out var glyphTypeface);

				CornerstoneTest.IsTrue(result);
				CornerstoneTest.IsNotNull(glyphTypeface);
				CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldLoadEmbeddedFallbacks()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				var fontFamily = FontFamily.Parse("NotFound, " + sfontUri);

				var typeface = new Typeface(fontFamily);

				var glyphTypeface = typeface.GlyphTypeface;

				CornerstoneTest.IsNotNull(glyphTypeface);

				CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldLoadNearestMatchingFont()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var glyphTypeface = new Typeface(sfontUri, FontStyle.Italic, FontWeight.Black).GlyphTypeface;

			CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);
		}
	}

	[PresentationTestMethod]
	public void ShouldLoadTypefaceFromResource()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var glyphTypeface = new Typeface(sfontUri).GlyphTypeface;

			CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);
		}
	}

	[PresentationTestMethod]
	public void ShouldMapFontFamily()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				PresentationLocator.CurrentMutable.BindToSelf(new FontManagerOptions
				{
					DefaultFamilyName = sfontUri,
					FontFamilyMappings = new Dictionary<string, FontFamily>
					{
						{ "Segoe UI", new FontFamily("fonts:Inter#Inter") }
					}
				});

				FontManager.Current.AddFontCollection(new InterFontCollection());

				var result = FontManager.Current.TryGetGlyphTypeface(new Typeface("Abc, Segoe UI"), out var glyphTypeface);

				CornerstoneTest.IsTrue(result);
				CornerstoneTest.IsNotNull(glyphTypeface);
				CornerstoneTest.AreEqual("Inter", glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	[DataRow("NotFound, Unknown", null)] // system fonts
	[DataRow("/#NotFound, /#Unknown", "csres://some/path")] // embedded fonts
	public void ShouldMatchCharacterWithFallbacks(string familyName, string baseUri)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				var fontFamily = FontFamily.Parse(familyName, baseUri is null ? null : new Uri(baseUri));

				CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, fontFamily, null, out var typeface));

				var glyphTypeface = typeface.GlyphTypeface;

				CornerstoneTest.IsNotNull(glyphTypeface);

				CornerstoneTest.AreEqual(FontManager.Current.DefaultFontFamily.Name, glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldMatchChararcterFromSystemFonts()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var typeface));

				var glyphTypeface = typeface.GlyphTypeface;

				CornerstoneTest.IsNotNull(glyphTypeface);

				CornerstoneTest.AreEqual(FontManager.Current.DefaultFontFamily.Name, glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldMatchChararcterWidthEmbeddedFallbacks()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				var fontFamily = FontFamily.Parse("NotFound, " + sfontUri);

				CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, fontFamily, null, out var typeface));

				var glyphTypeface = typeface.GlyphTypeface;

				CornerstoneTest.IsNotNull(glyphTypeface);

				CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldOnlyTryToCreateGlyphTypefaceOnce()
	{
		var fontManagerImpl = new TestFontManager();

		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManagerImpl)))
		{
			CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(Typeface.Default, out _));

			var countBefore = fontManagerImpl.TryCreateGlyphTypefaceCount;

			for (var i = 0; i < 10; i++)
			{
				FontManager.Current.TryGetGlyphTypeface(new Typeface("Unknown"), out _);
			}

			CornerstoneTest.AreEqual(countBefore + 1, fontManagerImpl.TryCreateGlyphTypefaceCount);
		}
	}

	[PresentationTestMethod]
	public void ShouldReturnFalseForInvalidDefaultFontFamily()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				PresentationLocator.CurrentMutable.BindToSelf(new FontManagerOptions { DefaultFamilyName = "csres://resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Unknown" });

				var result = FontManager.Current.TryGetGlyphTypeface(Typeface.Default, out _);

				CornerstoneTest.IsFalse(result);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldReturnFalseForUnregisteredFontCollectionUri()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var result = FontManager.Current.TryGetGlyphTypeface(new Typeface("fonts:invalid#Something"), out _);

			CornerstoneTest.IsFalse(result);
		}
	}

	[PresentationTestMethod]
	public void ShouldThrowForInvalidCustomFont()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			Assert.Throws<InvalidOperationException>(() => new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Unknown").GlyphTypeface);
		}
	}

	[PresentationTestMethod]
	public void ShouldUseCustomSystemFont()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				FontManager.Current.AddFontCollection(new EmbeddedFontCollection(FontManager.SystemFontsKey,
					new Uri(sfontUri, UriKind.Absolute)));

				CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono"), out var glyphTypeface));

				CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldUseFontCollectionMatchCharacter()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				FontManager.Current.AddFontCollection(
					new EmbeddedFontCollection(
						new Uri("fonts:MyCollection"), //key
						new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests"))); //source

				var fontFamily = new FontFamily("fonts:MyCollection#Noto Mono");

				var character = "א";

				var codepoint = Codepoint.ReadAt(character, 0, out _);

				CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(codepoint, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, fontFamily, null, out var typeface));

				//Typeface should come from the font collection
				CornerstoneTest.IsNotNull(typeface.FontFamily.Key);

				CornerstoneTest.AreEqual("Noto Sans Hebrew", typeface.GlyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldUseLastResortFontLastMatchCharacter()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			using (PresentationLocator.EnterScope())
			{
				FontManager.Current.AddFontCollection(
					new EmbeddedFontCollection(
						new Uri("fonts:MyCollection"), //key
						new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests"))); //source

				var fontFamily = new FontFamily("fonts:MyCollection#Noto Sans");

				const string characters = "א𪜶";

				var codepoint1 = Codepoint.ReadAt(characters, 0, out _);
				CornerstoneTest.AreEqual(0x5D0, codepoint1); // א

				// Typeface should come from the font collection - falling back to Noto Sans Hebrew
				CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(codepoint1, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, fontFamily, null, out var typeface1));
				CornerstoneTest.IsNotNull(typeface1.FontFamily.Key);
				CornerstoneTest.AreEqual("Noto Sans Hebrew", typeface1.GlyphTypeface.FamilyName);

				var codepoint2 = Codepoint.ReadAt(characters, 1, out _);
				CornerstoneTest.AreEqual(0x2A736, codepoint2); // 𪜶

				// Typeface should come from the font collection - falling back to Adobe Blank 2 VF R as a last resort
				CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(codepoint2, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, fontFamily, null, out var typeface2));
				CornerstoneTest.IsNotNull(typeface2.FontFamily.Key);
				CornerstoneTest.AreEqual("Adobe Blank 2 VF R", typeface2.GlyphTypeface.FamilyName);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldYieldDefaultGlyphTypefaceForInvalidFamilyName()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var glyphTypeface = new Typeface(new FontFamily("Unknown")).GlyphTypeface;

			CornerstoneTest.AreEqual(FontManager.Current.DefaultFontFamily.Name, glyphTypeface.FamilyName);
		}
	}

	[PresentationTestMethod]
	public void TryGetGlyphTypefaceShouldCacheMatchedGlyphTypefaceUnderRequestedFamilyName()
	{
		var fontManagerImpl = new FamilyRemappingFontManagerImpl("NotInstalled", "Noto Mono");
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManagerImpl));

		// "NotInstalled" is not installed, so the platform substitutes it with a different
		// family ("Noto Mono"), much like requesting "Arial" yields "Liberation Sans" on some Linux distributions.
		CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("NotInstalled"), out var first));
		CornerstoneTest.AreEqual("Noto Mono", first.FamilyName);

		// The substitute should now be cached under the requested "NotInstalled" name, so a second
		// lookup must resolve from the cache instead of asking the platform again.
		CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("NotInstalled"), out var second));
		CornerstoneTest.Same(first, second);
		CornerstoneTest.AreEqual(1, fontManagerImpl.RequestedFamilyCreateCount);
	}

	[PresentationTestMethod]
	public void TryGetGlyphTypefaceShouldReturnFalseForFontWithoutSupportedCmap()
	{
		const string fontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.Fonts.TestFontNoCmap412.ttf?assembly=Cornerstone.Presentation.UnitTests#TestFontNoCmap412";

		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));
		using var scope = PresentationLocator.EnterScope();

		// Skia can load the font
		AssertCanCreatePlatformTypeface();

		// But Cornerstone can't, because it has no supported cmap subtable
		AssertCannotCreateGlyphTypeface();

		void AssertCanCreatePlatformTypeface()
		{
			var fontManagerImpl = PresentationLocator.Current.GetRequiredService<IFontManagerImpl>();
			var assetLoader = PresentationLocator.Current.GetRequiredService<IAssetLoader>();

			using var stream = assetLoader.Open(new Uri(fontUri));

			CornerstoneTest.IsTrue(fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface));
			CornerstoneTest.IsNotNull(platformTypeface);
			CornerstoneTest.AreEqual("TestFontNoCmap412", platformTypeface.FamilyName);
		}

		void AssertCannotCreateGlyphTypeface()
		{
			string loggedTemplate = null;
			object[] loggedValues = [];

			using var logSinkScope = TestLogSink.Start((level, area, _, template, values) =>
			{
				if ((level == LogEventLevel.Warning) && (area == LogArea.Fonts))
				{
					loggedTemplate = template;
					loggedValues = values;
				}
			});

			CornerstoneTest.IsFalse(FontManager.Current.TryGetGlyphTypeface(new Typeface(fontUri), out _));
			CornerstoneTest.AreEqual(loggedTemplate, "Could not create glyph typeface from platform typeface named {FamilyName} with simulations {Simulations}: {Exception}");
			CornerstoneTest.AreEqual(3, loggedValues.Length);
			CornerstoneTest.AreEqual("TestFontNoCmap412", CornerstoneTest.IsType<string>(loggedValues[0]));
			CornerstoneTest.AreEqual("No suitable cmap subtable found.", CornerstoneTest.IsType<InvalidOperationException>(loggedValues[2]).Message);
		}
	}

	[PresentationTestMethod]
	public void TryGetGlyphTypefaceShouldUsePerfectMatchInCollectionBeforeNearestMatch()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl()));
		using var scope = PresentationLocator.EnterScope();

		// Load bold font (Inter-Bold.ttf) first
		CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), out var boldGlyphTypeface));
		CornerstoneTest.IsNotNull(boldGlyphTypeface);
		CornerstoneTest.AreEqual("Inter", boldGlyphTypeface.FamilyName);
		CornerstoneTest.AreEqual(FontWeight.Bold, boldGlyphTypeface.Weight);

		// Normal font (Inter-Regular.ttf) should be loaded since it's a perfect match, instead of falling back
		CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), out var regularGlyphTypeface));
		CornerstoneTest.IsNotNull(regularGlyphTypeface);
		CornerstoneTest.NotSame(regularGlyphTypeface, boldGlyphTypeface);
		CornerstoneTest.AreEqual("Inter", regularGlyphTypeface.FamilyName);
		CornerstoneTest.AreEqual(FontWeight.Normal, regularGlyphTypeface.Weight);

		// Nearest match should still work (650 falls back to 700 Bold)
		CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(new Typeface("Inter", FontStyle.Normal, (FontWeight) 650), out var nearestMatchTypeface));
		CornerstoneTest.Same(boldGlyphTypeface, nearestMatchTypeface);
	}

	[PresentationTestMethod]
	[DataRow(FontStretch.Normal)]
	[DataRow(FontStretch.Condensed)]
	[DataRow(FontStretch.Expanded)]
	[DataRow(FontStretch.SemiCondensed)]
	[DataRow(FontStretch.SemiExpanded)]
	public void TryMatchCharacterShouldReturnCorrectStretch(FontStretch requestedStretch)
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));
		using var scope = PresentationLocator.EnterScope();

		FontManager.Current.AddFontCollection(new InterFontCollection());

		CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(
			'A', FontStyle.Normal, FontWeight.Normal, requestedStretch, new FontFamily("fonts:Inter#Inter"), null, out var typeface));

		CornerstoneTest.IsNotNull(typeface);
		CornerstoneTest.AreEqual("Inter", typeface.GlyphTypeface.FamilyName);
		CornerstoneTest.AreEqual(requestedStretch, typeface.Stretch);
	}

	[PresentationTestMethod]
	[DataRow(FontWeight.Normal, "Inter")]
	[DataRow(FontWeight.Bold, "Inter")]
	public void TryMatchCharacterShouldReturnCorrectWeight(FontWeight requestedWeight, string expectedFamilyName)
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));
		using var scope = PresentationLocator.EnterScope();

		FontManager.Current.AddFontCollection(new InterFontCollection());

		CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(
			'A', FontStyle.Normal, requestedWeight, FontStretch.Normal, new FontFamily("fonts:Inter#Inter"), null, out var typeface));

		CornerstoneTest.IsNotNull(typeface);
		CornerstoneTest.AreEqual(expectedFamilyName, typeface.GlyphTypeface.FamilyName);
		CornerstoneTest.AreEqual(requestedWeight, typeface.Weight);
	}

	#endregion

	#region Classes

	/// <summary>
	/// A font manager whose every by-name lookup resolves to a single matched font whose family name
	/// differs from the requested one.
	/// </summary>
	private sealed class FamilyRemappingFontManagerImpl(string requestedFamilyName, string matchedFamilyName)
		: IFontManagerImpl, IDisposable
	{
		#region Properties

		public int RequestedFamilyCreateCount { get; private set; }

		#endregion

		#region Methods

		public void Dispose()
		{
		}

		public string GetDefaultFontFamilyName()
		{
			return matchedFamilyName;
		}

		public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
		{
			return [matchedFamilyName];
		}

		public bool TryCreateGlyphTypeface(
			string familyName,
			FontStyle style,
			FontWeight weight,
			FontStretch stretch,
			[NotNullWhen(true)] out IPlatformTypeface platformTypeface)
		{
			if (string.Equals(familyName, requestedFamilyName, StringComparison.OrdinalIgnoreCase))
			{
				RequestedFamilyCreateCount++;
			}

			platformTypeface = new SkiaTypeface(CreateMatchedTypeface(), FontSimulations.None);
			return true;
		}

		public bool TryCreateGlyphTypeface(
			Stream stream,
			FontSimulations fontSimulations,
			[NotNullWhen(true)] out IPlatformTypeface platformTypeface)
		{
			platformTypeface = new SkiaTypeface(SKTypeface.FromStream(stream), fontSimulations);
			return true;
		}

		public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface> familyTypefaces)
		{
			familyTypefaces = null;
			return false;
		}

		public bool TryMatchCharacter(
			int codepoint,
			FontStyle fontStyle,
			FontWeight fontWeight,
			FontStretch fontStretch,
			string familyName,
			CultureInfo culture,
			[NotNullWhen(true)] out IPlatformTypeface platformTypeface)
		{
			platformTypeface = null;
			return false;
		}

		private SKTypeface CreateMatchedTypeface()
		{
			var assetLoader = PresentationLocator.Current.GetRequiredService<IAssetLoader>();

			// LoadFontAssets ignores the family fragment and returns every embedded font asset,
			// so pick the one whose family name matches the substitute we want to return.
			foreach (var fontAsset in FontFamilyLoader.LoadFontAssets(new Uri(sfontUri)))
			{
				var stream = assetLoader.Open(fontAsset);
				var typeface = SKTypeface.FromStream(stream);

				if (typeface is not null &&
					string.Equals(typeface.FamilyName, matchedFamilyName, StringComparison.OrdinalIgnoreCase))
				{
					return typeface;
				}

				typeface?.Dispose();
			}

			throw new InvalidOperationException($"Could not load the '{matchedFamilyName}' font asset.");
		}

		#endregion
	}

	private class InterFontCollection : EmbeddedFontCollection
	{
		#region Constructors

		public InterFontCollection()
			: base(
				new Uri("fonts:Inter", UriKind.Absolute),
				new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests", UriKind.Absolute))
		{
		}

		#endregion
	}

	#endregion
}