#region References

using System;
using System.IO;
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

[TestClass]
public class CustomFontCollectionTests
{
	#region Constants

	private const string AssetFonts = $"resm:{AssetsNamespace}?assembly=Cornerstone.Presentation.UnitTests";
	private const string AssetsNamespace = "Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ShouldAddFontSourceFromFile()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var fontManager = FontManager.Current;
			var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));
			fontManager.AddFontCollection(fontCollection);

			// Path to the test font
			var fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");
			CornerstoneTest.IsTrue(File.Exists(fontPath));

			var fontUri = new Uri(fontPath, UriKind.Absolute);

			// Add the font file
			CornerstoneTest.IsTrue(fontCollection.TryAddFontSource(fontUri));

			// Check if the font was loaded
			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("Inter", FontStyle.Normal, FontWeight.Regular, FontStretch.Normal, out var glyphTypeface));
			CornerstoneTest.AreEqual("Inter", glyphTypeface.FamilyName);

			// Check if the FontManager can find the font
			CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(new Typeface("fonts:custom#Inter"), out var glyphTypeface2));
			CornerstoneTest.AreEqual(glyphTypeface, glyphTypeface2);
		}
	}

	[PresentationTestMethod]
	public void ShouldAddFontSourceFromFolder()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var fontManager = FontManager.Current;
			var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));
			fontManager.AddFontCollection(fontCollection);

			// Path to the test fonts
			var fontsFolder = Path.Combine(AppContext.BaseDirectory, "Assets");
			CornerstoneTest.IsTrue(Directory.Exists(fontsFolder));

			var folderUri = new Uri(fontsFolder + Path.DirectorySeparatorChar, UriKind.Absolute);

			// Add the fonts
			CornerstoneTest.IsTrue(fontCollection.TryAddFontSource(folderUri));

			// Check if the font was loaded
			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("Inter", FontStyle.Normal, FontWeight.Regular, FontStretch.Normal, out var glyphTypeface));
			CornerstoneTest.AreEqual("Inter", glyphTypeface.FamilyName);

			// Check if the FontManager can find the font
			CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(new Typeface("fonts:custom#Inter"), out var glyphTypeface2));
			CornerstoneTest.AreEqual(glyphTypeface, glyphTypeface2);
		}
	}

	[PresentationTestMethod]
	public void ShouldAddFontSourceFromResource()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var fontManager = FontManager.Current;
			var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));
			fontManager.AddFontCollection(fontCollection);

			var allFontsUri = new Uri(AssetFonts, UriKind.Absolute);

			// Add the font resource
			CornerstoneTest.IsTrue(fontCollection.TryAddFontSource(allFontsUri));

			// Get the loaded family names
			var families = fontCollection.ToArray();

			CornerstoneTest.NotEmpty(families);

			// Try to get a GlyphTypeface
			CornerstoneTest.IsTrue(fontCollection.TryGetGlyphTypeface("Noto Mono", FontStyle.Normal, FontWeight.Regular, FontStretch.Normal, out var glyphTypeface));
			CornerstoneTest.AreEqual("Noto Mono", glyphTypeface.FamilyName);

			// Check if the FontManager can find the font
			CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(new Typeface("fonts:custom#Noto Mono"), out var glyphTypeface2));
			CornerstoneTest.AreEqual(glyphTypeface, glyphTypeface2);
		}
	}

	[PresentationTestMethod]
	public void ShouldAddGlyphTypefaceByStream()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var fontManager = FontManager.Current;

			var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));

			fontManager.AddFontCollection(fontCollection);

			var assetLoader = PresentationLocator.Current.GetRequiredService<IAssetLoader>();

			var infos = new[]
			{
				new FontAssetInfo($"{AssetsNamespace}.AdobeBlank2VF.ttf", "Adobe Blank 2 VF R", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.Inter-Bold.ttf", "Inter", FontWeight.Bold),
				new FontAssetInfo($"{AssetsNamespace}.Inter-Regular.ttf", "Inter", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.Manrope-Light.ttf", "Manrope Light", FontWeight.Light),
				new FontAssetInfo($"{AssetsNamespace}.MiSans-Normal.ttf", "MiSans Normal", (FontWeight) 305),
				new FontAssetInfo($"{AssetsNamespace}.NISC18030.ttf", "GB18030 Bitmap", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.NotoMono-Regular.ttf", "Noto Mono", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.NotoSans-Italic.ttf", "Noto Sans", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.NotoSansArabic-Regular.ttf", "Noto Sans Arabic", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.NotoSansDeseret-Regular.ttf", "Noto Sans Deseret", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.NotoSansHebrew-Regular.ttf", "Noto Sans Hebrew", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.NotoSansMiao-Regular.ttf", "Noto Sans Miao", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.NotoSansTamil-Regular.ttf", "Noto Sans Tamil", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.SourceSerif4_36pt-Italic.ttf", "Source Serif 4 36pt", FontWeight.Normal),
				new FontAssetInfo($"{AssetsNamespace}.TwitterColorEmoji-SVGinOT.ttf", "Twitter Color Emoji", FontWeight.Normal)
			};

			var assets = assetLoader.GetAssets(new Uri(AssetFonts, UriKind.Absolute), null)
				.OrderBy(uri => uri.AbsoluteUri, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			CornerstoneTest.AreEqual(infos.Length, assets.Length);

			var glyphTypefaces = new GlyphTypeface[infos.Length];

			// Load fonts
			for (var i = 0; i < infos.Length; ++i)
			{
				var info = infos[i];
				var asset = assets[i];

				CornerstoneTest.AreEqual(info.Path, asset.AbsolutePath);

				using var fontStream = assetLoader.Open(asset);
				CornerstoneTest.IsNotNull(fontStream);

				CornerstoneTest.IsTrue(fontCollection.TryAddGlyphTypeface(fontStream, out var glyphTypeface));
				CornerstoneTest.AreEqual(info.FamilyName, glyphTypeface.FamilyName);
				CornerstoneTest.AreEqual(info.Weight, glyphTypeface.Weight);

				glyphTypefaces[i] = glyphTypeface;
			}

			// Check against the custom collection
			for (var i = 0; i < infos.Length; ++i)
			{
				var info = infos[i];
				var glyphTypeface = glyphTypefaces[i];

				CornerstoneTest.IsTrue(fontManager.TryGetGlyphTypeface(new Typeface($"fonts:custom#{info.FamilyName}", weight: info.Weight), out var secondGlyphTypeface));
				CornerstoneTest.Same(glyphTypeface, secondGlyphTypeface);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldEnumerateFontFamilies()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
		{
			var fontManager = FontManager.Current;

			var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));

			fontManager.AddFontCollection(fontCollection);

			var assetLoader = PresentationLocator.Current.GetRequiredService<IAssetLoader>();

			var assets = assetLoader.GetAssets(new Uri(AssetFonts, UriKind.Absolute), null).Where(x => x.AbsolutePath.EndsWith(".ttf")).ToArray();

			foreach (var asset in assets)
			{
				fontCollection.TryAddGlyphTypeface(assetLoader.Open(asset), out _);
			}

			var families = fontCollection.ToArray();

			CornerstoneTest.IsTrue(families.Length >= assets.Length);

			var other = new CustomFontCollection(new Uri("fonts:other", UriKind.Absolute));

			foreach (var family in families)
			{
				var familyTypefaces = family.FamilyTypefaces;

				foreach (var typeface in familyTypefaces)
				{
					other.TryAddGlyphTypeface(typeface.GlyphTypeface);
				}
			}

			CornerstoneTest.AreEqual(families.Length, other.Count);

			for (var i = 0; i < families.Length; i++)
			{
				CornerstoneTest.AreEqual(families[i].Name, other[i].Name);
			}
		}
	}

	#endregion

	#region Classes

	private class CustomFontCollection(Uri key) : FontCollectionBase
	{
		#region Properties

		public override Uri Key { get; } = key;

		#endregion
	}

	#endregion

	#region Records

	private record struct FontAssetInfo(string Path, string FamilyName, FontWeight Weight);

	#endregion
}