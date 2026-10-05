#region References

using System;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

[TestClass]
public class GlyphTypefaceShapingTests
{
	#region Constants

	private const string ArabicFont = "Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.NotoSansArabic-Regular.ttf";

	// A tiny Noto Sans Arabic subset with the GSUB/GPOS layout tables stripped: it keeps Arabic
	// cmap glyphs but cannot shape Arabic. Renamed so it doesn't collide with the full font.
	private const string ArabicNoLayoutFont = "Cornerstone.Presentation.UnitTests.Skia.Fonts.NotoSansArabic-NoLayout.ttf";
	private const string MonoFont = "Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.NotoMono-Regular.ttf";

	#endregion

	#region Methods

	// P0 — CanShapeScript gates complex scripts on GSUB/GPOS script coverage, not cmap. This is
	// the primitive the F3 capability fallback (Strategy A) builds on.
	[PresentationTestMethod]
	public void CanShapeScriptGatesComplexScriptsOnLayoutCoverageNotCmap()
	{
		using (Start(MonoFont, ArabicFont, ArabicNoLayoutFont))
		{
			var fontManager = FontManager.Current;

			CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(
				new Typeface("fonts:SystemFonts#Noto Mono"), out var mono));
			CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(
				new Typeface("fonts:SystemFonts#Noto Sans Arabic"), out var arabic));
			CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(
				new Typeface("fonts:SystemFonts#Noto Sans Arabic NoLayout"), out var arabicNoLayout));

			// Simple scripts never require layout tables — always true, regardless of the font.
			CornerstoneTest.IsTrue(mono.CanShapeScript(Script.Latin));
			CornerstoneTest.IsTrue(arabic.CanShapeScript(Script.Latin));

			// The real Arabic font declares the 'arab' GSUB script, so it can shape Arabic.
			CornerstoneTest.IsTrue(arabic.CanShapeScript(Script.Arabic));

			// A Latin-only font has no Arabic layout coverage.
			CornerstoneTest.IsFalse(mono.CanShapeScript(Script.Arabic));

			// The crux: the stripped font HAS Arabic cmap glyphs but no GSUB/GPOS, so it cannot
			// shape Arabic even though TryGetGlyph succeeds. cmap coverage is not shaping capability.
			CornerstoneTest.IsTrue(arabicNoLayout.CharacterToGlyphMap.TryGetGlyph(0x0627, out _)); // ا is mapped
			CornerstoneTest.IsFalse(arabicNoLayout.CanShapeScript(Script.Arabic));

			// A complex script the Arabic font does not declare is rejected too.
			CornerstoneTest.IsFalse(arabic.CanShapeScript(Script.Devanagari));
		}
	}

	private static IDisposable Start(params string[] fontResourceNames)
	{
		var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
			.With(renderInterface: new PlatformRenderInterface()));

		var fontManagerImpl = new CustomFontManagerImpl();

		PresentationLocator.CurrentMutable
			.Bind<IFontManagerImpl>().ToConstant(fontManagerImpl);

		var fontManager = new FontManager(fontManagerImpl);

		PresentationLocator.CurrentMutable
			.Bind<FontManager>().ToConstant(fontManager);

		fontManager.AddFontCollection(new CuratedSystemFontCollection(fontResourceNames));

		return disposable;
	}

	#endregion

	#region Classes

	private sealed class CuratedSystemFontCollection : FontCollectionBase
	{
		#region Constructors

		public CuratedSystemFontCollection(string[] fontResourceNames)
		{
			foreach (var name in fontResourceNames)
			{
				TryAddFontSource(new Uri($"resm:{name}?assembly=Cornerstone.Presentation.UnitTests"));
			}
		}

		#endregion

		#region Properties

		public override Uri Key => FontManager.SystemFontsKey;

		#endregion
	}

	#endregion
}