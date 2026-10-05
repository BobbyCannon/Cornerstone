#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class FontFamilyTests
{
	#region Constructors

	public FontFamilyTests()
	{
		AssetLoader.RegisterResUriParsers();
	}

	#endregion

	#region Methods

	[DataRow("Font A")]
	[DataRow("Font A, Font B")]
	[DataRow("resm: Cornerstone.Presentation.Visuals.UnitTests#MyFont")]
	[DataRow("csres://Cornerstone.Presentation.Visuals.UnitTests/Assets/Fonts#MyFont")]
	[PresentationTestMethod]
	public void ShouldBeEqual(string s)
	{
		var fontFamily = new FontFamily(s);

		CornerstoneTest.AreEqual(new FontFamily(s), fontFamily);
	}

	[PresentationTestMethod]
	[DataRow("resm:Cornerstone.Presentation.Visuals.UnitTests/Assets/Fonts#MyFont")]
	[DataRow("csres://Cornerstone.Presentation.Visuals.UnitTests/Assets/Fonts#MyFont")]
	public void ShouldCreateFontFamilyFromUri(string name)
	{
		var fontFamily = new FontFamily(name);

		CornerstoneTest.AreEqual("MyFont", fontFamily.Name);

		CornerstoneTest.IsNotNull(fontFamily.Key);
	}

	[PresentationTestMethod]
	[DataRow(null, "resm:Cornerstone.Presentation.Visuals.UnitTests.Assets.Fonts#MyFont")]
	[DataRow("csres://Cornerstone.Presentation.Visuals.UnitTests/Assets/Fonts", "/#MyFont")]
	[DataRow("csres://Cornerstone.Presentation.Visuals.UnitTests", "/Assets/Fonts#MyFont")]
	public void ShouldCreateFontFamilyFromUriWithBaseUri(string @base, string name)
	{
		var baseUri = @base != null ? new Uri(@base) : null;

		var fontFamily = new FontFamily(baseUri, name);

		CornerstoneTest.AreEqual("MyFont", fontFamily.Name);

		CornerstoneTest.IsNotNull(fontFamily.Key);
	}

	[DataRow("Font A")]
	[DataRow("Font A, Font B")]
	[DataRow("resm: Cornerstone.Presentation.Visuals.UnitTests#MyFont")]
	[DataRow("csres://Cornerstone.Presentation.Visuals.UnitTests/Assets/Fonts#MyFont")]
	[PresentationTestMethod]
	public void ShouldHaveEqualHash(string s)
	{
		var fontFamily = new FontFamily(s);

		CornerstoneTest.AreEqual(new FontFamily(s).GetHashCode(), fontFamily.GetHashCode());
	}

	[PresentationTestMethod]
	public void ShouldImplicitlyConvertStringToFontFamily()
	{
		FontFamily fontFamily = "Arial";

		CornerstoneTest.AreEqual(new FontFamily("Arial"), fontFamily);
	}

	[DataRow("Font A, Font B", "Font B, Font A")]
	[DataRow("Font A, Font B", "Font A, Font C")]
	[PresentationTestMethod]
	public void ShouldNotBeEqual(string a, string b)
	{
		var fontFamily = new FontFamily(b);

		CornerstoneTest.AreNotEqual(new FontFamily(a), fontFamily);
	}

	[DataRow("Font A, Font B", "Font B, Font A")]
	[DataRow("Font A, Font B", "Font A, Font C")]
	[PresentationTestMethod]
	public void ShouldNotHaveEqualHash(string a, string b)
	{
		var fontFamily = new FontFamily(b);

		CornerstoneTest.AreNotEqual(new FontFamily(a).GetHashCode(), fontFamily.GetHashCode());
	}

	[DataRow(null, "Arial", "Arial", null)]
	[DataRow(null, "resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Manrope", "Manrope", "resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests")]
	[DataRow(null, "csres://Cornerstone.Presentation.Fonts.Inter/Assets#Inter", "Inter", null)]
	[DataRow("csres://Cornerstone.Presentation.Fonts.Inter", "/Assets#Inter", "Inter", "csres://Cornerstone.Presentation.Fonts.Inter/Assets")]
	[DataRow("csres://ControlCatalog/MainWindow.xaml", "csres://Cornerstone.Presentation.Fonts.Inter/Assets#Inter", "Inter", "csres://Cornerstone.Presentation.Fonts.Inter/Assets")]
	[PresentationTestMethod]
	public void ShouldParseFontFamilyWithBaseUri(string baseUri, string s, string expectedName, string expectedUri)
	{
		var b = baseUri is not null ? new Uri(baseUri) : null;

		expectedUri = expectedUri is not null ? new Uri(expectedUri).AbsoluteUri : null;

		var fontFamily = FontFamily.Parse(s, b);

		CornerstoneTest.AreEqual(expectedName, fontFamily.Name);

		var key = fontFamily.Key;

		if (expectedUri is not null)
		{
			CornerstoneTest.IsNotNull(key);

			if (key.BaseUri is not null)
			{
				CornerstoneTest.IsTrue(key.BaseUri.IsAbsoluteUri);
			}

			if (key.BaseUri is null)
			{
				CornerstoneTest.IsNotNull(key.Source);
				CornerstoneTest.IsTrue(key.Source.IsAbsoluteUri);
			}

			var fontUri = key.BaseUri;

			if (key.Source is Uri sourceUri)
			{
				if (sourceUri.IsAbsoluteUri)
				{
					fontUri = sourceUri;
				}
				else
				{
					CornerstoneTest.IsNotNull(fontUri);
					fontUri = new Uri(fontUri, sourceUri);
				}
			}

			CornerstoneTest.IsNotNull(fontUri);
			CornerstoneTest.AreEqual(expectedUri, fontUri.AbsoluteUri);
		}
	}

	[PresentationTestMethod]
	public void ShouldParseFontFamilyWithFallbacks()
	{
		var fontFamily = FontFamily.Parse("Courier New, Times New Roman");

		CornerstoneTest.AreEqual("Courier New", fontFamily.Name);

		CornerstoneTest.AreEqual(2, fontFamily.FamilyNames.Count);

		CornerstoneTest.AreEqual("Times New Roman", fontFamily.FamilyNames.Last());
	}

	[PresentationTestMethod]
	public void ShouldParseFontFamilyWithResourceFilename()
	{
		var source = new Uri("resm:Cornerstone.Presentation.Visuals.UnitTests.MyFont.ttf#MyFont");

		var key = new FontFamilyKey(source);

		var fontFamily = FontFamily.Parse(source.OriginalString);

		CornerstoneTest.AreEqual("MyFont", fontFamily.Name);

		CornerstoneTest.AreEqual(key, fontFamily.Key);
	}

	[PresentationTestMethod]
	public void ShouldParseFontFamilyWithResourceFolder()
	{
		var source = new Uri("resm:Cornerstone.Presentation.Visuals.UnitTests#MyFont");

		var key = new FontFamilyKey(source);

		var fontFamily = FontFamily.Parse(source.OriginalString);

		CornerstoneTest.AreEqual("MyFont", fontFamily.Name);

		CornerstoneTest.AreEqual(key, fontFamily.Key);
	}

	[PresentationTestMethod]
	public void ShouldParseFontFamilyWithSystemFontName()
	{
		var fontFamily = FontFamily.Parse("Courier New");

		CornerstoneTest.AreEqual("Courier New", fontFamily.Name);
	}

	[DataRow("csres://MyAssembly/", "Some/Path/#FontName", "csres://MyAssembly/Some/Path/")]
	[DataRow("csres://MyAssembly/", "./Some/Path/#FontName", "csres://MyAssembly/Some/Path/")]
	[DataRow("csres://MyAssembly/sub/", "../Some/Path/#FontName", "csres://MyAssembly/Some/Path/")]
	[PresentationTestMethod]
	public void ShouldParseRelativePath(string baseUriString, string path, string expected)
	{
		var baseUri = new Uri(baseUriString, UriKind.Absolute);

		var fontFamily = FontFamily.Parse(path, baseUri);

		CornerstoneTest.IsNotNull(fontFamily.Key);

		CornerstoneTest.IsNotNull(fontFamily.Key.BaseUri);

		CornerstoneTest.IsNotNull(fontFamily.Key.Source);

		var actual = new Uri(fontFamily.Key.BaseUri, fontFamily.Key.Source);

		CornerstoneTest.AreEqual(expected, actual.AbsoluteUri);
	}

	#endregion
}