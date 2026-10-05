#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class TestFontManager : IFontManagerImpl
{
	#region Fields

	private static readonly Uri InterRegularUri = new(
		"resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.Inter-Regular.ttf?assembly=Cornerstone.Presentation.UnitTests");

	#endregion

	#region Properties

	public int TryCreateGlyphTypefaceCount { get; private set; }

	#endregion

	#region Methods

	public string GetDefaultFontFamilyName()
	{
		return "Inter";
	}

	public virtual bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
		FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface platformTypeface)
	{
		var assetLoader = new StandardAssetLoader();
		var stream = assetLoader.Open(InterRegularUri);
		platformTypeface = new HeadlessPlatformTypeface(stream, string.IsNullOrEmpty(familyName) ? "Inter" : familyName);
		TryCreateGlyphTypefaceCount++;
		return true;
	}

	public virtual bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
		[NotNullWhen(true)] out IPlatformTypeface platformTypeface)
	{
		platformTypeface = new HeadlessPlatformTypeface(stream);

		TryCreateGlyphTypefaceCount++;

		return true;
	}

	public bool TryGetFamilyTypefaces(string familyName,
		[NotNullWhen(true)] out IReadOnlyList<Typeface> familyTypefaces)
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
		out IPlatformTypeface platformTypeface)
	{
		return TryCreateGlyphTypeface(familyName ?? "Inter", fontStyle, fontWeight, fontStretch, out platformTypeface);
	}

	string[] IFontManagerImpl.GetInstalledFontFamilyNames(bool checkForUpdates)
	{
		return new[] { "Inter" };
	}

	#endregion
}