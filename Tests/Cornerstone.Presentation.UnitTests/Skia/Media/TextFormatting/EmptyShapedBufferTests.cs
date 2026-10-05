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

/// <summary>
/// A shaper hides the default ignorables it substitutes for line breaks behind the font's space
/// glyph. A font that has no space glyph leaves it no way to do that, so those glyphs are deleted
/// instead and a run holding nothing but a line break shapes to an empty glyph buffer. The run
/// still owns its characters and has to survive, otherwise the line covers no text at all.
/// </summary>
[TestClass]
public class EmptyShapedBufferTests
{
	#region Constants

	// The headless platform's default font: four glyphs, no space, no coverage for anything else.
	private const string GlyphlessFont = "Cornerstone.Presentation.UnitTests.Skia.Fonts.BareMinimum.ttf";

	#endregion

	#region Fields

	private static readonly Typeface stypeface = new("fonts:SystemFonts#BareMinimum");

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void LineBreakThatShapesToNoGlyphsKeepsItsCharacters()
	{
		using (Start())
		{
			var defaultProperties = new GenericTextRunProperties(stypeface, 12);

			var formatter = new TextFormatterImpl();

			var textLine = formatter.FormatLine(new SingleBufferTextSource("\r\nfoo", defaultProperties), 0, 100,
				new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap));

			CornerstoneTest.IsNotNull(textLine);

			var run = CornerstoneTest.IsType<ShapedTextRun>(CornerstoneTest.Single(textLine.TextRuns));

			CornerstoneTest.AreEqual(2, run.Length);
			CornerstoneTest.AreEqual(2, textLine.Length);

			// The premise of the test: shaping really did produce nothing for these characters.
			CornerstoneTest.Empty(run.ShapedBuffer);
		}
	}

	[PresentationTestMethod]
	public void ShouldWrapTextThatStartsWithALineBreak()
	{
		using (Start())
		{
			// MaxLines bounds the layout loop: a line that covers no text never advances the text
			// source, so without it a regression here hangs the test run instead of failing it.
			var layout = new TextLayout("\r\nPassword update failed", stypeface, 12, Brushes.Black,
				textWrapping: TextWrapping.Wrap, maxWidth: 290, maxLines: 5);

			CornerstoneTest.AreEqual(2, layout.TextLines.Count);

			CornerstoneTest.AreEqual(2, layout.TextLines[0].Length);
			CornerstoneTest.AreEqual(22, layout.TextLines[1].Length);
		}
	}

	private static IDisposable Start()
	{
		var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
			.With(renderInterface: new PlatformRenderInterface()));

		var fontManagerImpl = new CustomFontManagerImpl();

		PresentationLocator.CurrentMutable
			.Bind<IFontManagerImpl>().ToConstant(fontManagerImpl);

		var fontManager = new FontManager(fontManagerImpl);

		PresentationLocator.CurrentMutable
			.Bind<FontManager>().ToConstant(fontManager);

		fontManager.AddFontCollection(new GlyphlessSystemFontCollection());

		return disposable;
	}

	#endregion

	#region Classes

	private sealed class GlyphlessSystemFontCollection : FontCollectionBase
	{
		#region Constructors

		public GlyphlessSystemFontCollection()
		{
			TryAddFontSource(new Uri($"resm:{GlyphlessFont}?assembly=Cornerstone.Presentation.UnitTests"));
		}

		#endregion

		#region Properties

		public override Uri Key => FontManager.SystemFontsKey;

		#endregion
	}

	#endregion
}