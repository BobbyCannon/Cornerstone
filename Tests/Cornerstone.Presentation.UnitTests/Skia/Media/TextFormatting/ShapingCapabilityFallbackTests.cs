#region References

using System;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

[TestClass]
public class ShapingCapabilityFallbackTests
{
	#region Constants

	private const string ArabicFont = "Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.NotoSansArabic-Regular.ttf";

	// Tiny Noto Sans Arabic subset with the layout tables stripped: it has the Arabic cmap glyphs
	// but no GSUB/GPOS, so it cannot actually shape Arabic. Renamed so it doesn't collide with the
	// full font.
	private const string ArabicNoLayoutFont = "Cornerstone.Presentation.UnitTests.Skia.Fonts.NotoSansArabic-NoLayout.ttf";
	private const string MonoFont = "Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.NotoMono-Regular.ttf";

	#endregion

	#region Methods

	// When no shaping-capable font for the script exists, the cmap-only font is kept (the capability
	// tier finds nothing, the cmap tier then accepts it) — we never reject more than before.
	[PresentationTestMethod]
	public void CmapOnlyComplexScriptPrimaryIsKeptWhenNoCapableFontExists()
	{
		CornerstoneTest.AreEqual("Noto Sans Arabic NoLayout", ResolveArabicRunFamily(MonoFont, ArabicNoLayoutFont));
	}

	// For a complex script, a primary that has the cmap glyphs but can't shape it (no GSUB/GPOS) is
	// upgraded to a shaping-capable font. This is unconditional — there is no longer a mode toggle.
	[PresentationTestMethod]
	public void CmapOnlyComplexScriptPrimaryIsUpgradedToAShapingCapableFont()
	{
		// A shaping-capable Arabic font is present, so the cmap-only primary is replaced by it.
		CornerstoneTest.AreEqual("Noto Sans Arabic", ResolveArabicRunFamily(MonoFont, ArabicFont, ArabicNoLayoutFont));
	}

	private static string ResolveArabicRunFamily(params string[] fontResourceNames)
	{
		using (Start(fontResourceNames))
		{
			var fontManager = FontManager.Current;

			// Primary run typeface: the cmap-only (no-layout) Arabic font.
			var defaultProperties = new GenericTextRunProperties(
				new Typeface("fonts:SystemFonts#Noto Sans Arabic NoLayout"));

			var text = char.ConvertFromUtf32(0x0627).AsMemory(); // U+0627 ARABIC LETTER ALEF

			var textCharacters = new TextCharacters(text, defaultProperties);

			var results = FormattingObjectPool.Instance.TextRunLists.Rent();

			try
			{
				TextRunProperties previousProperties = null;

				textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

				CornerstoneTest.Single(results);
				CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(results[0].Properties!.Typeface, out var runGlyphTypeface));

				return runGlyphTypeface.FamilyName;
			}
			finally
			{
				var toReturn = results;
				FormattingObjectPool.Instance.TextRunLists.Return(ref toReturn);
			}
		}
	}

	private static IDisposable Start(string[] fontResourceNames)
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