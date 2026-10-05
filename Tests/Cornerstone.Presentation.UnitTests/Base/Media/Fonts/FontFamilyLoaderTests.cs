#region References

using System;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts;

[TestClass]
public class FontFamilyLoaderTests : IDisposable
{
	#region Constants

	private const string Assembly = "?assembly=Cornerstone.Presentation.Visuals.UnitTests";
	private const string AssetLocation = "resm:Cornerstone.Presentation.Visuals.UnitTests.Assets";
	private const string AssetLocationCsres = "csres://Cornerstone.Presentation.Visuals.UnitTests";
	private const string AssetMyFontRegular = AssetLocation + ".MyFont Regular.ttf" + Assembly + FontName;
	private const string AssetYourFileName = "/Assets/YourFont.ttf";
	private const string AssetYourFontCsres = AssetLocationCsres + AssetYourFileName;
	private const string FontName = "#MyFont";

	#endregion

	#region Fields

	private readonly IDisposable _testApplication;

	#endregion

	#region Constructors

	public FontFamilyLoaderTests()
	{
		const string AssetMyFontBold = AssetLocation + ".MyFont Bold.ttf" + Assembly + FontName;
		const string AssetYourFont = AssetLocation + ".YourFont.ttf" + Assembly + FontName;

		var fontAssets = new[]
		{
			(AssetMyFontRegular, "AssetData"),
			(AssetMyFontBold, "AssetData"),
			(AssetYourFont, "AssetData"),
			(AssetYourFontCsres, "AssetData")
		};

		_testApplication = StartWithResources(fontAssets);
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		_testApplication.Dispose();
	}

	[PresentationTestMethod]
	public void ShouldLoadEmbeddedFont()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var assetLoader = PresentationLocator.Current.GetRequiredService<IAssetLoader>();

			var source = new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Noto Mono", UriKind.RelativeOrAbsolute);

			var fontAssets = FontFamilyLoader.LoadFontAssets(source).ToArray();

			CornerstoneTest.NotEmpty(fontAssets);

			foreach (var fontAsset in fontAssets)
			{
				var stream = assetLoader.Open(fontAsset);

				CornerstoneTest.IsNotNull(stream);
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldLoadMatchingAssets()
	{
		var source = new Uri(AssetLocation + ".MyFont*.ttf" + Assembly + FontName, UriKind.RelativeOrAbsolute);

		var fontAssets = FontFamilyLoader.LoadFontAssets(source).ToArray();

		foreach (var fontAsset in fontAssets)
		{
			Debug.WriteLine(fontAsset);
		}

		CornerstoneTest.AreEqual(2, fontAssets.Length);
	}

	[PresentationTestMethod]
	public void ShouldLoadSingleFontAsset()
	{
		var source = new Uri(AssetMyFontRegular, UriKind.RelativeOrAbsolute);

		var fontAssets = FontFamilyLoader.LoadFontAssets(source);

		CornerstoneTest.Single(fontAssets);
	}

	[PresentationTestMethod]
	public void ShouldLoadSingleFontAssetCsresWithBaseUri()
	{
		var source = new Uri(AssetYourFileName, UriKind.RelativeOrAbsolute);
		var baseUri = new Uri(AssetLocationCsres);

		var fontAssets = FontFamilyLoader.LoadFontAssets(new Uri(baseUri, source));

		CornerstoneTest.Single(fontAssets);
	}

	[PresentationTestMethod]
	public void ShouldLoadSingleFontAssetCsresWithoutBaseUri()
	{
		var source = new Uri(AssetYourFontCsres);

		var fontAssets = FontFamilyLoader.LoadFontAssets(source);

		CornerstoneTest.Single(fontAssets);
	}

	private static IDisposable StartWithResources(params (string, string)[] assets)
	{
		var assetLoader = new MockAssetLoader(assets);
		var services = new TestServices(assetLoader, platform: new StandardRuntimePlatform());
		return UnitTestApplication.Start(services);
	}

	#endregion
}