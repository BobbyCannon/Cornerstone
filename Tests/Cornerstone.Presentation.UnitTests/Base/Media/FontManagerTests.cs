#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class FontManagerTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldCreateSingleInstanceTypeface()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var fontFamily = new FontFamily("MyFont");

			var typeface = new Typeface(fontFamily);

			CornerstoneTest.IsTrue(FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface));

			FontManager.Current.TryGetGlyphTypeface(typeface, out var other);

			CornerstoneTest.Same(glyphTypeface, other);
		}
	}

	[PresentationTestMethod]
	public void ShouldReturnFirstInstalledFontFamilyNameWhenDefaultFamilyNameIsNull()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
					.With(fontManagerImpl: new HeadlessFontManagerWithMultipleSystemFontsStub(
						["DejaVu", "Verdana"],
						null!))))
		{
			CornerstoneTest.AreEqual("DejaVu", FontManager.Current.DefaultFontFamily.Name);
		}
	}

	[PresentationTestMethod]
	public void ShouldThrowWhenDefaultFamilyNameIsNullAndInstalledFontFamilyNamesIsEmpty()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
					.With(fontManagerImpl: new HeadlessFontManagerWithMultipleSystemFontsStub(
						[],
						null!))))
		{
			Assert.Throws<InvalidOperationException>(() => FontManager.Current);
		}
	}

	[PresentationTestMethod]
	public void ShouldUseFontManagerOptionsDefaultFamilyName()
	{
		var options = new FontManagerOptions { DefaultFamilyName = "MyFont" };

		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
					.With(fontManagerImpl: new HeadlessFontManagerStub())))
		{
			PresentationLocator.CurrentMutable.Bind<FontManagerOptions>().ToConstant(options);

			CornerstoneTest.AreEqual("MyFont", FontManager.Current.DefaultFontFamily.Name);
		}
	}

	[PresentationTestMethod]
	public void ShouldUseFontManagerOptionsFontFallback()
	{
		var options = new FontManagerOptions
		{
			FontFallbacks = new[]
			{
				new FontFallback
				{
					FontFamily = new FontFamily("MyFont"), UnicodeRange = UnicodeRange.Default
				}
			}
		};

		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			PresentationLocator.CurrentMutable.Bind<FontManagerOptions>().ToConstant(options);

			FontManager.Current.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
				FontFamily.Default, null, out var typeface);

			CornerstoneTest.AreEqual("MyFont", typeface.FontFamily.Name);
		}
	}

	[PresentationTestMethod]
	public async Task TryGetGlyphTypefaceShouldBeThreadSafeForEmbeddedFonts()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var fontManager = FontManager.Current;

			const string fontUri =
				"resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Noto Mono";
			var collectionKey =
				new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests");

			// Warm up to validate the font URI is correct.
			CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(new Typeface(new FontFamily(fontUri)), out _));

			const int iterations = 50;
			var failures = 0;

			for (var i = 0; i < iterations; i++)
			{
				fontManager.RemoveFontCollection(collectionKey);

				using var barrier = new Barrier(2);
				bool r1 = false, r2 = false;

				var t1 = Task.Run(() =>
				{
					barrier.SignalAndWait();
					r1 = fontManager.TryGetGlyphTypeface(new Typeface(new FontFamily(fontUri)), out _);
				}, CancellationToken.None);

				var t2 = Task.Run(() =>
				{
					barrier.SignalAndWait();
					r2 = fontManager.TryGetGlyphTypeface(new Typeface(new FontFamily(fontUri)), out _);
				}, CancellationToken.None);

				await Task.WhenAll(t1, t2);

				if (!r1 || !r2)
				{
					Interlocked.Increment(ref failures);
				}
			}

			CornerstoneTest.AreEqual(0, failures);
		}
	}

	#endregion
}