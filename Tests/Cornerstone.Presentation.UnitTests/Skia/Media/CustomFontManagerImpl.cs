#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Platform;
using SkiaSharp;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

public class CustomFontManagerImpl : IFontManagerImpl, IDisposable
{
	#region Fields

	private readonly string[] _bcp47 = { CultureInfo.CurrentCulture.ThreeLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName };
	private readonly string _defaultFamilyName;
	private IFontCollection _systemFonts;

	#endregion

	#region Constructors

	public CustomFontManagerImpl()
	{
		_defaultFamilyName = FontManager.SystemFontsKey + "#Noto Mono";
	}

	#endregion

	#region Properties

	public IFontCollection SystemFonts
	{
		get
		{
			if (_systemFonts is null)
			{
				var source = new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests");

				_systemFonts = new EmbeddedFontCollection(FontManager.SystemFontsKey, source);
			}

			return _systemFonts;
		}
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		_systemFonts?.Dispose();
	}

	public string GetDefaultFontFamilyName()
	{
		return _defaultFamilyName;
	}

	public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
	{
		// Directly load from assets to avoid creating the full font collection
		try
		{
			var key = new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests");

			var assetLoader = PresentationLocator.Current.GetRequiredService<IAssetLoader>();

			var fontAssets = FontFamilyLoader.LoadFontAssets(key);
			var names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (var fontAsset in fontAssets)
			{
				try
				{
					using var stream = assetLoader.Open(fontAsset);
					using var sk = SKTypeface.FromStream(stream);

					if ((sk != null) && !string.IsNullOrEmpty(sk.FamilyName))
					{
						names.Add(sk.FamilyName);
					}
				}
				catch
				{
					// Ignore faulty assets
				}
			}

			return names.ToArray();
		}
		catch
		{
			return Array.Empty<string>();
		}
	}

	public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
		FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface platformTypeface)
	{
		if (SystemFonts.TryGetGlyphTypeface(familyName, style, weight, stretch, out var glyphTypeface))
		{
			platformTypeface = glyphTypeface.PlatformTypeface;

			return true;
		}

		var skTypeface = SKTypeface.FromFamilyName(familyName,
			(SKFontStyleWeight) weight, SKFontStyleWidth.Normal, (SKFontStyleSlant) style);

		platformTypeface = new SkiaTypeface(skTypeface, FontSimulations.None);

		return true;
	}

	public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IPlatformTypeface platformTypeface)
	{
		var skTypeface = SKTypeface.FromStream(stream);

		platformTypeface = new SkiaTypeface(skTypeface, fontSimulations);

		return true;
	}

	public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface> familyTypefaces)
	{
		if (SystemFonts.TryGetFamilyTypefaces(familyName, out familyTypefaces))
		{
			return true;
		}

		var set = SKFontManager.Default.GetFontStyles(familyName);

		if (set.Count == 0)
		{
			return false;
		}

		var typefaces = new List<Typeface>(set.Count);

		foreach (var fontStyle in set)
		{
			typefaces.Add(new Typeface(familyName, fontStyle.Slant.ToCornerstone(), (FontWeight) fontStyle.Weight, (FontStretch) fontStyle.Width));
		}

		familyTypefaces = typefaces;

		return true;
	}

	public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch,
		string familyName, CultureInfo culture, out IPlatformTypeface platformTypeface)
	{
		if (SystemFonts.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, out var glyphTypeface))
		{
			platformTypeface = glyphTypeface.GlyphTypeface.PlatformTypeface;

			return true;
		}

		var fallback = SKFontManager.Default.MatchCharacter(familyName, (SKFontStyleWeight) fontWeight,
			(SKFontStyleWidth) fontStretch, (SKFontStyleSlant) fontStyle, _bcp47, codepoint);

		platformTypeface = new SkiaTypeface(fallback, FontSimulations.None);

		return true;
	}

	#endregion
}