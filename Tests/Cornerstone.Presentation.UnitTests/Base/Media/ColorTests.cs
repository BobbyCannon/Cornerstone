#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class ColorTests
{
	#region Methods

	[PresentationTestMethod]
	public void ColorToStringDefaultReturnsHexForUnknown()
	{
		var color = new Color(0x40, 0xFF, 0x88, 0x44);

		CornerstoneTest.AreEqual("#40ff8844", color.ToString());
	}

	// =====================================================================
	// IFormattable unified format specifier tests
	//
	// All three color types (Color, HslColor, HsvColor) support ALL
	// format specifiers. Cross-model formats auto-convert.
	//
	// Convention:
	//   Uppercase = include alpha, a-suffixed prefix (rgba, hsla, hsva)
	//   Lowercase = exclude alpha, plain prefix (rgb, hsl, hsv)
	//   "%" suffix = percent mode
	// =====================================================================

	[PresentationTestMethod]
	public void ColorToStringDefaultReturnsKnownName()
	{
		var red = new Color(0xFF, 0xFF, 0x00, 0x00);

		CornerstoneTest.AreEqual("Red", red.ToString());
	}

	[PresentationTestMethod]
	public void ColorToStringEmptyFormatMatchesDefault()
	{
		var color = new Color(0x40, 0xFF, 0x88, 0x44);

		CornerstoneTest.AreEqual(color.ToString(), color.ToString("", null));
	}

	[PresentationTestMethod]
	[DataRow(0xFF, 0xFF, 0x88, 0x44, "#FF8844FF")]
	[DataRow(0x40, 0xFF, 0x88, 0x44, "#FF884440")]
	[DataRow(0x00, 0x00, 0x00, 0x00, "#00000000")]
	public void ColorToStringHReturnsHtmlHexWithAlpha(int a, int r, int g, int b, string expected)
	{
		var color = new Color((byte) a, (byte) r, (byte) g, (byte) b);

		CornerstoneTest.AreEqual(expected, color.ToString("H", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void ColorToStringIFormatProviderIsIgnored()
	{
		var color = new Color(0x80, 0xFF, 0x88, 0x44);
		var french = CultureInfo.GetCultureInfo("fr-FR");

		CornerstoneTest.AreEqual(color.ToString("R", CultureInfo.InvariantCulture), color.ToString("R", french));
	}

	[PresentationTestMethod]
	public void ColorToStringInvalidFormatThrows()
	{
		var color = new Color(0xFF, 0xFF, 0x00, 0x00);

		Assert.Throws<FormatException>(() => color.ToString("Z", null));
	}

	[PresentationTestMethod]
	public void ColorToStringLConvertsToHsla()
	{
		// Pure red: RGB(255, 0, 0) = HSL(0, 100%, 50%)
		var color = new Color(0xFF, 0xFF, 0x00, 0x00);

		CornerstoneTest.AreEqual("hsla(0, 100%, 50%, 1.00)", color.ToString("L", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void ColorToStringNullFormatMatchesDefault()
	{
		var color = new Color(0x40, 0xFF, 0x88, 0x44);

		CornerstoneTest.AreEqual(color.ToString(), color.ToString(null, null));
	}

	[PresentationTestMethod]
	[DataRow(0xFF, 0xFF, 0x80, 0x00, "rgba(100%, 50%, 0%, 100%)")]
	[DataRow(0x80, 0xFF, 0x80, 0x00, "rgba(100%, 50%, 0%, 50%)")]
	[DataRow(0x00, 0x00, 0x00, 0x00, "rgba(0%, 0%, 0%, 0%)")]
	public void ColorToStringRPctReturnsRgbaPercent(int a, int r, int g, int b, string expected)
	{
		var color = new Color((byte) a, (byte) r, (byte) g, (byte) b);

		CornerstoneTest.AreEqual(expected, color.ToString("R%", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(0xFF, 0xFF, 0x88, 0x44, "rgba(255, 136, 68, 1.00)")]
	[DataRow(0x80, 0xFF, 0x88, 0x44, "rgba(255, 136, 68, 0.50)")]
	[DataRow(0x00, 0x00, 0x00, 0x00, "rgba(0, 0, 0, 0.00)")]
	public void ColorToStringRReturnsRgbaWithAlpha(int a, int r, int g, int b, string expected)
	{
		var color = new Color((byte) a, (byte) r, (byte) g, (byte) b);

		CornerstoneTest.AreEqual(expected, color.ToString("R", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow("C")]
	[DataRow("c")]
	[DataRow("A")]
	[DataRow("a")]
	[DataRow("P")]
	[DataRow("h")]
	public void ColorToStringReservedAndRemovedSpecifiersThrow(string format)
	{
		var color = new Color(0xFF, 0xFF, 0x00, 0x00);

		Assert.Throws<FormatException>(() => color.ToString(format, null));
	}

	[PresentationTestMethod]
	public void ColorToStringVConvertsToHsva()
	{
		// Pure red: RGB(255, 0, 0) = HSV(0, 100%, 100%)
		var color = new Color(0xFF, 0xFF, 0x00, 0x00);

		CornerstoneTest.AreEqual("hsva(0, 100%, 100%, 1.00)", color.ToString("V", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(0xFF, 0xFF, 0x88, 0x44, "#FFFF8844")]
	[DataRow(0x40, 0xFF, 0x88, 0x44, "#40FF8844")]
	[DataRow(0xFF, 0x00, 0x00, 0x00, "#FF000000")]
	[DataRow(0x00, 0x00, 0x00, 0x00, "#00000000")]
	public void ColorToStringXReturnsXamlHexWithAlpha(int a, int r, int g, int b, string expected)
	{
		var color = new Color((byte) a, (byte) r, (byte) g, (byte) b);

		CornerstoneTest.AreEqual(expected, color.ToString("X", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void ColorToStringlConvertsToHsl()
	{
		var color = new Color(0xFF, 0xFF, 0x00, 0x00);

		CornerstoneTest.AreEqual("hsl(0, 100%, 50%)", color.ToString("l", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(0xFF, 0xFF, 0x80, 0x00, "rgb(100%, 50%, 0%)")]
	[DataRow(0x80, 0xFF, 0x80, 0x00, "rgb(100%, 50%, 0%)")]
	[DataRow(0xFF, 0x00, 0x00, 0x00, "rgb(0%, 0%, 0%)")]
	public void ColorToStringrPctReturnsRgbPercent(int a, int r, int g, int b, string expected)
	{
		var color = new Color((byte) a, (byte) r, (byte) g, (byte) b);

		CornerstoneTest.AreEqual(expected, color.ToString("r%", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(0xFF, 0xFF, 0x88, 0x44, "rgb(255, 136, 68)")]
	[DataRow(0x80, 0xFF, 0x88, 0x44, "rgb(255, 136, 68)")]
	[DataRow(0xFF, 0x00, 0x00, 0x00, "rgb(0, 0, 0)")]
	public void ColorToStringrReturnsRgbWithoutAlpha(int a, int r, int g, int b, string expected)
	{
		var color = new Color((byte) a, (byte) r, (byte) g, (byte) b);

		CornerstoneTest.AreEqual(expected, color.ToString("r", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(0xFF, 0xFF, 0x88, 0x44, "#FF8844")]
	[DataRow(0x40, 0xFF, 0x88, 0x44, "#FF8844")]
	[DataRow(0x00, 0x00, 0x00, 0x00, "#000000")]
	public void ColorToStringxReturnsHexWithoutAlpha(int a, int r, int g, int b, string expected)
	{
		var color = new Color((byte) a, (byte) r, (byte) g, (byte) b);

		CornerstoneTest.AreEqual(expected, color.ToString("x", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HslColorToStringIFormatProviderIsIgnored()
	{
		var color = new HslColor(0.5, 180, 0.5, 0.5);
		var french = CultureInfo.GetCultureInfo("fr-FR");

		CornerstoneTest.AreEqual(color.ToString("L", CultureInfo.InvariantCulture), color.ToString("L", french));
	}

	[PresentationTestMethod]
	public void HslColorToStringInvalidFormatThrows()
	{
		var color = new HslColor(1.0, 0, 0, 0);

		Assert.Throws<FormatException>(() => color.ToString("Z", null));
	}

	[PresentationTestMethod]
	[DataRow(1.0, 180, 0.5, 0.5, "hsla(50%, 50%, 50%, 100%)")]
	[DataRow(0.5, 90, 1.0, 1.0, "hsla(25%, 100%, 100%, 50%)")]
	[DataRow(1.0, 0, 0.0, 0.0, "hsla(0%, 0%, 0%, 100%)")]
	public void HslColorToStringLPctReturnsHslaAllPercent(double a, double h, double s, double l, string expected)
	{
		var color = new HslColor(a, h, s, l);

		CornerstoneTest.AreEqual(expected, color.ToString("L%", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(1.0, 180, 0.5, 0.5, "hsla(180, 50%, 50%, 1.00)")]
	[DataRow(0.5, 240, 0.8, 0.2, "hsla(240, 80%, 20%, 0.50)")]
	[DataRow(0.0, 0, 0.0, 0.0, "hsla(0, 0%, 0%, 0.00)")]
	public void HslColorToStringLReturnsHslaWithAlpha(double a, double h, double s, double l, string expected)
	{
		var color = new HslColor(a, h, s, l);

		CornerstoneTest.AreEqual(expected, color.ToString("L", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HslColorToStringNullFormatMatchesDefault()
	{
		var color = new HslColor(0.8, 200, 0.6, 0.4);

		CornerstoneTest.AreEqual(color.ToString(), color.ToString(null, null));
	}

	[PresentationTestMethod]
	public void HslColorToStringRConvertsToRgba()
	{
		// Pure blue: HSL(240, 1, 0.5) = RGB(0, 0, 255)
		var hsl = new HslColor(1.0, 240, 1.0, 0.5);

		CornerstoneTest.AreEqual("rgba(0, 0, 255, 1.00)", hsl.ToString("R", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HslColorToStringRPctConvertsToRgbaPercent()
	{
		// Pure red: HSL(0, 1, 0.5) = RGB(255, 0, 0)
		var hsl = new HslColor(1.0, 0, 1.0, 0.5);

		CornerstoneTest.AreEqual("rgba(100%, 0%, 0%, 100%)", hsl.ToString("R%", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow("C")]
	[DataRow("c")]
	public void HslColorToStringReservedCThrows(string format)
	{
		var color = new HslColor(1.0, 0, 1.0, 0.5);

		Assert.Throws<FormatException>(() => color.ToString(format, null));
	}

	[PresentationTestMethod]
	public void HslColorToStringVConvertsToHsva()
	{
		// Pure red: HSL(0, 1, 0.5) = HSV(0, 100%, 100%)
		var hsl = new HslColor(1.0, 0, 1.0, 0.5);

		CornerstoneTest.AreEqual("hsva(0, 100%, 100%, 1.00)", hsl.ToString("V", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HslColorToStringXConvertsToRgbHex()
	{
		// Pure red: HSL(0, 1, 0.5) = RGB(255, 0, 0)
		var hsl = new HslColor(1.0, 0, 1.0, 0.5);

		CornerstoneTest.AreEqual("#FFFF0000", hsl.ToString("X", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(1.0, 180, 0.5, 0.5, "hsl(50%, 50%, 50%)")]
	[DataRow(0.5, 90, 1.0, 1.0, "hsl(25%, 100%, 100%)")]
	[DataRow(1.0, 0, 0.0, 0.0, "hsl(0%, 0%, 0%)")]
	public void HslColorToStringlPctReturnsHslAllPercent(double a, double h, double s, double l, string expected)
	{
		var color = new HslColor(a, h, s, l);

		CornerstoneTest.AreEqual(expected, color.ToString("l%", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(1.0, 180, 0.5, 0.5, "hsl(180, 50%, 50%)")]
	[DataRow(0.5, 240, 0.8, 0.2, "hsl(240, 80%, 20%)")]
	[DataRow(0.0, 0, 0.0, 0.0, "hsl(0, 0%, 0%)")]
	public void HslColorToStringlReturnsHslWithoutAlpha(double a, double h, double s, double l, string expected)
	{
		var color = new HslColor(a, h, s, l);

		CornerstoneTest.AreEqual(expected, color.ToString("l", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HslColorToStringxConvertsToRgbHex()
	{
		var hsl = new HslColor(1.0, 0, 1.0, 0.5);

		CornerstoneTest.AreEqual("#FF0000", hsl.ToString("x", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HsvColorToStringInvalidFormatThrows()
	{
		var color = new HsvColor(1.0, 0, 0, 0);

		Assert.Throws<FormatException>(() => color.ToString("Z", null));
	}

	[PresentationTestMethod]
	public void HsvColorToStringLConvertsToHsla()
	{
		// Pure red: HSV(0, 1, 1) = HSL(0, 100%, 50%)
		var hsv = new HsvColor(1.0, 0, 1.0, 1.0);

		CornerstoneTest.AreEqual("hsla(0, 100%, 50%, 1.00)", hsv.ToString("L", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HsvColorToStringNullFormatMatchesDefault()
	{
		var color = new HsvColor(0.8, 200, 0.6, 0.4);

		CornerstoneTest.AreEqual(color.ToString(), color.ToString(null, null));
	}

	[PresentationTestMethod]
	[DataRow("C")]
	[DataRow("c")]
	public void HsvColorToStringReservedCThrows(string format)
	{
		var color = new HsvColor(1.0, 0, 1.0, 1.0);

		Assert.Throws<FormatException>(() => color.ToString(format, null));
	}

	[PresentationTestMethod]
	[DataRow(1.0, 180, 0.5, 0.5, "hsva(50%, 50%, 50%, 100%)")]
	[DataRow(0.5, 90, 1.0, 1.0, "hsva(25%, 100%, 100%, 50%)")]
	[DataRow(1.0, 0, 0.0, 0.0, "hsva(0%, 0%, 0%, 100%)")]
	public void HsvColorToStringVPctReturnsHsvaAllPercent(double a, double h, double s, double v, string expected)
	{
		var color = new HsvColor(a, h, s, v);

		CornerstoneTest.AreEqual(expected, color.ToString("V%", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(1.0, 180, 0.5, 0.5, "hsva(180, 50%, 50%, 1.00)")]
	[DataRow(0.5, 240, 0.8, 0.2, "hsva(240, 80%, 20%, 0.50)")]
	[DataRow(0.0, 0, 0.0, 0.0, "hsva(0, 0%, 0%, 0.00)")]
	public void HsvColorToStringVReturnsHsvaWithAlpha(double a, double h, double s, double v, string expected)
	{
		var color = new HsvColor(a, h, s, v);

		CornerstoneTest.AreEqual(expected, color.ToString("V", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HsvColorToStringXConvertsToRgbHex()
	{
		// Pure red: HSV(0, 1, 1) = RGB(255, 0, 0)
		var hsv = new HsvColor(1.0, 0, 1.0, 1.0);

		CornerstoneTest.AreEqual("#FFFF0000", hsv.ToString("X", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HsvColorToStringrConvertsToRgb()
	{
		// Pure red: HSV(0, 1, 1) = RGB(255, 0, 0)
		var hsv = new HsvColor(1.0, 0, 1.0, 1.0);

		CornerstoneTest.AreEqual("rgb(255, 0, 0)", hsv.ToString("r", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(1.0, 180, 0.5, 0.5, "hsv(50%, 50%, 50%)")]
	[DataRow(0.5, 90, 1.0, 1.0, "hsv(25%, 100%, 100%)")]
	[DataRow(1.0, 0, 0.0, 0.0, "hsv(0%, 0%, 0%)")]
	public void HsvColorToStringvPctReturnsHsvAllPercent(double a, double h, double s, double v, string expected)
	{
		var color = new HsvColor(a, h, s, v);

		CornerstoneTest.AreEqual(expected, color.ToString("v%", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	[DataRow(1.0, 180, 0.5, 0.5, "hsv(180, 50%, 50%)")]
	[DataRow(0.5, 240, 0.8, 0.2, "hsv(240, 80%, 20%)")]
	[DataRow(0.0, 0, 0.0, 0.0, "hsv(0, 0%, 0%)")]
	public void HsvColorToStringvReturnsHsvWithoutAlpha(double a, double h, double s, double v, string expected)
	{
		var color = new HsvColor(a, h, s, v);

		CornerstoneTest.AreEqual(expected, color.ToString("v", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HsvColorToStringxConvertsToRgbHex()
	{
		var hsv = new HsvColor(1.0, 0, 1.0, 1.0);

		CornerstoneTest.AreEqual("#FF0000", hsv.ToString("x", CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void HsvToFromHslConversion()
	{
		// Note that conversion of values more representative of actual colors is not done due to rounding error
		// It would be necessary to introduce a different equality comparison that accounts for rounding differences in values
		// This is a result of the math in the conversion itself
		// RGB doesn't have this problem because it uses whole numbers
		var data = new[]
		{
			Tuple.Create(new HsvColor(1.0, 0.0, 0.0, 0.0), new HslColor(1.0, 0.0, 0.0, 0.0)),
			Tuple.Create(new HsvColor(1.0, 359.0, 1.0, 1.0), new HslColor(1.0, 359.0, 1.0, 0.5)),

			Tuple.Create(new HsvColor(1.0, 128.0, 0.0, 0.0), new HslColor(1.0, 128.0, 0.0, 0.0)),
			Tuple.Create(new HsvColor(1.0, 128.0, 0.0, 1.0), new HslColor(1.0, 128.0, 0.0, 1.0)),
			Tuple.Create(new HsvColor(1.0, 128.0, 1.0, 1.0), new HslColor(1.0, 128.0, 1.0, 0.5)),

			Tuple.Create(new HsvColor(0.23, 0.5, 1.0, 1.0), new HslColor(0.23, 0.5, 1.0, 0.5))
		};

		foreach (var dataPoint in data)
		{
			var convertedHsl = dataPoint.Item1.ToHsl();
			var convertedHsv = dataPoint.Item2.ToHsv();

			CornerstoneTest.AreEqual(convertedHsv, dataPoint.Item1);
			CornerstoneTest.AreEqual(convertedHsl, dataPoint.Item2);
		}
	}

	[PresentationTestMethod]
	public void ParseHexValueDoesntAcceptInvalidNumber()
	{
		Assert.Throws<FormatException>(() => Color.Parse("#ff808g80"));
	}

	[PresentationTestMethod]
	public void ParseHexValueDoesntAcceptTooFewChars()
	{
		Assert.Throws<FormatException>(() => Color.Parse("#ff"));
	}

	[PresentationTestMethod]
	public void ParseHexValueDoesntAcceptTooManyChars()
	{
		Assert.Throws<FormatException>(() => Color.Parse("#ff5555555"));
	}

	[PresentationTestMethod]
	public void ParseParsesARGBHashColor()
	{
		var result = Color.Parse("#40ff8844");

		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x88, result.G);
		CornerstoneTest.AreEqual(0x44, result.B);
		CornerstoneTest.AreEqual(0x40, result.A);
	}

	[PresentationTestMethod]
	public void ParseParsesARGBHashShorthandColor()
	{
		var result = Color.Parse("#4f84");

		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x88, result.G);
		CornerstoneTest.AreEqual(0x44, result.B);
		CornerstoneTest.AreEqual(0x44, result.A);
	}

	[PresentationTestMethod]
	public void ParseParsesNamedColorLowercase()
	{
		var result = Color.Parse("red");

		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x00, result.G);
		CornerstoneTest.AreEqual(0x00, result.B);
		CornerstoneTest.AreEqual(0xff, result.A);
	}

	[PresentationTestMethod]
	public void ParseParsesNamedColorUppercase()
	{
		var result = Color.Parse("RED");

		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x00, result.G);
		CornerstoneTest.AreEqual(0x00, result.B);
		CornerstoneTest.AreEqual(0xff, result.A);
	}

	[PresentationTestMethod]
	public void ParseParsesRGBHashColor()
	{
		var result = Color.Parse("#ff8844");

		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x88, result.G);
		CornerstoneTest.AreEqual(0x44, result.B);
		CornerstoneTest.AreEqual(0xff, result.A);
	}

	[PresentationTestMethod]
	public void ParseParsesRGBHashShorthandColor()
	{
		var result = Color.Parse("#f84");

		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x88, result.G);
		CornerstoneTest.AreEqual(0x44, result.B);
		CornerstoneTest.AreEqual(0xff, result.A);
	}

	[PresentationTestMethod]
	public void ParseThrowsArgumentNullExceptionForNullInput()
	{
		Assert.Throws<ArgumentNullException>(() => Color.Parse(null!));
	}

	[PresentationTestMethod]
	public void ParseThrowsFormatExceptionForInvalidInput()
	{
		Assert.Throws<FormatException>(() => Color.Parse(string.Empty));
	}

	[PresentationTestMethod]
	public void TryParseAllFormatsWithConversion()
	{
		// Inline data requires constants, so the data is handled internally here
		var data = new[]
		{
			// RGB
			Tuple.Create("White", new Color(0xff, 0xff, 0xff, 0xff)),
			Tuple.Create("#123456", new Color(0xff, 0x12, 0x34, 0x56)),

			Tuple.Create("rgb(100, 30, 45)", new Color(255, 100, 30, 45)),
			Tuple.Create("rgba(100, 30, 45, 0.9)", new Color(230, 100, 30, 45)),
			Tuple.Create("rgba(100, 30, 45, 90%)", new Color(230, 100, 30, 45)),

			Tuple.Create("rgb(255,0,0)", new Color(255, 255, 0, 0)),
			Tuple.Create("rgb(0,255,0)", new Color(255, 0, 255, 0)),
			Tuple.Create("rgb(0,0,255)", new Color(255, 0, 0, 255)),

			Tuple.Create("rgb(100%, 0, 0)", new Color(255, 255, 0, 0)),
			Tuple.Create("rgb(0, 100%, 0)", new Color(255, 0, 255, 0)),
			Tuple.Create("rgb(0, 0, 100%)", new Color(255, 0, 0, 255)),

			Tuple.Create("rgba(0, 0, 100%, 50%)", new Color(128, 0, 0, 255)),
			Tuple.Create("rgba(50%, 10%, 80%, 50%)", new Color(128, 128, 26, 204)),
			Tuple.Create("rgba(50%, 10%, 80%, 0.5)", new Color(128, 128, 26, 204)),

			// HSL
			Tuple.Create("hsl(296, 85%, 12%)", new Color(255, 53, 5, 57)),
			Tuple.Create("hsla(296, 0.85, 0.12, 0.9)", new Color(230, 53, 5, 57)),
			Tuple.Create("hsla(296, 85%, 12%, 90%)", new Color(230, 53, 5, 57)),

			// HSV
			Tuple.Create("hsv(240, 83%, 78%)", new Color(255, 34, 34, 199)),
			Tuple.Create("hsva(240, 0.83, 0.78, 0.9)", new Color(230, 34, 34, 199)),
			Tuple.Create("hsva(240, 83%, 78%, 90%)", new Color(230, 34, 34, 199))
		};

		foreach (var dataPoint in data)
		{
			CornerstoneTest.IsTrue(Color.TryParse(dataPoint.Item1, out var parsedColor));
			CornerstoneTest.IsTrue(dataPoint.Item2 == parsedColor);
		}
	}

	[PresentationTestMethod]
	public void TryParseHexValueDoesntAcceptInvalidNumber()
	{
		CornerstoneTest.IsFalse(Color.TryParse("#ff808g80", out _));
	}

	[PresentationTestMethod]
	public void TryParseHexValueDoesntAcceptTooFewChars()
	{
		CornerstoneTest.IsFalse(Color.TryParse("#ff", out _));
	}

	[PresentationTestMethod]
	public void TryParseHexValueDoesntAcceptTooManyChars()
	{
		CornerstoneTest.IsFalse(Color.TryParse("#ff5555555", out _));
	}

	[PresentationTestMethod]
	public void TryParseHslColor()
	{
		// Inline data requires constants, so the data is handled internally here
		var data = new[]
		{
			// HSV
			Tuple.Create("hsl(0, 0, 0)", new HslColor(1, 0, 0, 0)),
			Tuple.Create("hsl(0, 0%, 0%)", new HslColor(1, 0, 0, 0)),
			Tuple.Create("hsl(180, 0.5, 0.5)", new HslColor(1, 180, 0.5, 0.5)),
			Tuple.Create("hsl(180, 50%, 50%)", new HslColor(1, 180, 0.5, 0.5)),
			Tuple.Create("hsl(360, 1.0, 1.0)", new HslColor(1, 0, 1, 1)), // Wraps Hue to zero
			Tuple.Create("hsl(360, 100%, 100%)", new HslColor(1, 0, 1, 1)), // Wraps Hue to zero

			Tuple.Create("hsl(-1000, -1000, -1000)", new HslColor(1, 0, 0, 0)), // Clamps to min
			Tuple.Create("hsl(-1000, -1000%, -1000%)", new HslColor(1, 0, 0, 0)), // Clamps to min
			Tuple.Create("hsl(1000, 1000, 1000)", new HslColor(1, 0, 1, 1)), // Clamps to max (Hue wraps to zero)
			Tuple.Create("hsl(1000, 1000%, 1000%)", new HslColor(1, 0, 1, 1)), // Clamps to max (Hue wraps to zero)

			Tuple.Create("hsl(300, 0.8, 0.2)", new HslColor(1.0, 300, 0.8, 0.2)),
			Tuple.Create("hsl(300, 80%, 20%)", new HslColor(1.0, 300, 0.8, 0.2)),

			// HSVA
			Tuple.Create("hsla(0, 0, 0, 0)", new HslColor(0, 0, 0, 0)),
			Tuple.Create("hsla(0, 0%, 0%, 0%)", new HslColor(0, 0, 0, 0)),
			Tuple.Create("hsla(180, 0.5, 0.5, 0.5)", new HslColor(0.5, 180, 0.5, 0.5)),
			Tuple.Create("hsla(180, 50%, 50%, 50%)", new HslColor(0.5, 180, 0.5, 0.5)),
			Tuple.Create("hsla(360, 1.0, 1.0, 1.0)", new HslColor(1, 0, 1, 1)), // Wraps Hue to zero
			Tuple.Create("hsla(360, 100%, 100%, 100%)", new HslColor(1, 0, 1, 1)), // Wraps Hue to zero

			Tuple.Create("hsla(-1000, -1000, -1000, -1000)", new HslColor(0, 0, 0, 0)), // Clamps to min
			Tuple.Create("hsla(-1000, -1000%, -1000%, -1000%)", new HslColor(0, 0, 0, 0)), // Clamps to min
			Tuple.Create("hsla(1000, 1000, 1000, 1000)", new HslColor(1, 0, 1, 1)), // Clamps to max (Hue wraps to zero)
			Tuple.Create("hsla(1000, 1000%, 1000%, 1000%)", new HslColor(1, 0, 1, 1)), // Clamps to max (Hue wraps to zero)

			Tuple.Create("hsla(300, 0.9, 0.2, 0.8)", new HslColor(0.8, 300, 0.9, 0.2)),
			Tuple.Create("hsla(300, 90%, 20%, 0.8)", new HslColor(0.8, 300, 0.9, 0.2))
		};

		foreach (var dataPoint in data)
		{
			CornerstoneTest.IsTrue(HslColor.TryParse(dataPoint.Item1, out var parsedHslColor));
			CornerstoneTest.IsTrue(dataPoint.Item2 == parsedHslColor);
		}
	}

	[PresentationTestMethod]
	public void TryParseHsvColor()
	{
		// Inline data requires constants, so the data is handled internally here
		var data = new[]
		{
			// HSV
			Tuple.Create("hsv(0, 0, 0)", new HsvColor(1, 0, 0, 0)),
			Tuple.Create("hsv(0, 0%, 0%)", new HsvColor(1, 0, 0, 0)),
			Tuple.Create("hsv(180, 0.5, 0.5)", new HsvColor(1, 180, 0.5, 0.5)),
			Tuple.Create("hsv(180, 50%, 50%)", new HsvColor(1, 180, 0.5, 0.5)),
			Tuple.Create("hsv(360, 1.0, 1.0)", new HsvColor(1, 0, 1, 1)), // Wraps Hue to zero
			Tuple.Create("hsv(360, 100%, 100%)", new HsvColor(1, 0, 1, 1)), // Wraps Hue to zero

			Tuple.Create("hsv(-1000, -1000, -1000)", new HsvColor(1, 0, 0, 0)), // Clamps to min
			Tuple.Create("hsv(-1000, -1000%, -1000%)", new HsvColor(1, 0, 0, 0)), // Clamps to min
			Tuple.Create("hsv(1000, 1000, 1000)", new HsvColor(1, 0, 1, 1)), // Clamps to max (Hue wraps to zero)
			Tuple.Create("hsv(1000, 1000%, 1000%)", new HsvColor(1, 0, 1, 1)), // Clamps to max (Hue wraps to zero)

			Tuple.Create("hsv(300, 0.8, 0.2)", new HsvColor(1.0, 300, 0.8, 0.2)),
			Tuple.Create("hsv(300, 80%, 20%)", new HsvColor(1.0, 300, 0.8, 0.2)),

			// HSVA
			Tuple.Create("hsva(0, 0, 0, 0)", new HsvColor(0, 0, 0, 0)),
			Tuple.Create("hsva(0, 0%, 0%, 0%)", new HsvColor(0, 0, 0, 0)),
			Tuple.Create("hsva(180, 0.5, 0.5, 0.5)", new HsvColor(0.5, 180, 0.5, 0.5)),
			Tuple.Create("hsva(180, 50%, 50%, 50%)", new HsvColor(0.5, 180, 0.5, 0.5)),
			Tuple.Create("hsva(360, 1.0, 1.0, 1.0)", new HsvColor(1, 0, 1, 1)), // Wraps Hue to zero
			Tuple.Create("hsva(360, 100%, 100%, 100%)", new HsvColor(1, 0, 1, 1)), // Wraps Hue to zero

			Tuple.Create("hsva(-1000, -1000, -1000, -1000)", new HsvColor(0, 0, 0, 0)), // Clamps to min
			Tuple.Create("hsva(-1000, -1000%, -1000%, -1000%)", new HsvColor(0, 0, 0, 0)), // Clamps to min
			Tuple.Create("hsva(1000, 1000, 1000, 1000)", new HsvColor(1, 0, 1, 1)), // Clamps to max (Hue wraps to zero)
			Tuple.Create("hsva(1000, 1000%, 1000%, 1000%)", new HsvColor(1, 0, 1, 1)), // Clamps to max (Hue wraps to zero)

			Tuple.Create("hsva(300, 0.9, 0.2, 0.8)", new HsvColor(0.8, 300, 0.9, 0.2)),
			Tuple.Create("hsva(300, 90%, 20%, 0.8)", new HsvColor(0.8, 300, 0.9, 0.2))
		};

		foreach (var dataPoint in data)
		{
			CornerstoneTest.IsTrue(HsvColor.TryParse(dataPoint.Item1, out var parsedHsvColor));
			CornerstoneTest.IsTrue(dataPoint.Item2 == parsedHsvColor);
		}
	}

	[PresentationTestMethod]
	public void TryParseParsesARGBHashColor()
	{
		var success = Color.TryParse("#40ff8844", out var result);

		CornerstoneTest.IsTrue(success);
		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x88, result.G);
		CornerstoneTest.AreEqual(0x44, result.B);
		CornerstoneTest.AreEqual(0x40, result.A);
	}

	[PresentationTestMethod]
	public void TryParseParsesARGBHashShorthandColor()
	{
		var success = Color.TryParse("#4f84", out var result);

		CornerstoneTest.IsTrue(success);
		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x88, result.G);
		CornerstoneTest.AreEqual(0x44, result.B);
		CornerstoneTest.AreEqual(0x44, result.A);
	}

	[PresentationTestMethod]
	public void TryParseParsesNamedColorLowercase()
	{
		var success = Color.TryParse("red", out var result);

		CornerstoneTest.IsTrue(success);
		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x00, result.G);
		CornerstoneTest.AreEqual(0x00, result.B);
		CornerstoneTest.AreEqual(0xff, result.A);
	}

	[PresentationTestMethod]
	public void TryParseParsesNamedColorUppercase()
	{
		var success = Color.TryParse("RED", out var result);

		CornerstoneTest.IsTrue(success);
		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x00, result.G);
		CornerstoneTest.AreEqual(0x00, result.B);
		CornerstoneTest.AreEqual(0xff, result.A);
	}

	[PresentationTestMethod]
	public void TryParseParsesRGBHashColor()
	{
		var success = Color.TryParse("#ff8844", out var result);

		CornerstoneTest.IsTrue(success);
		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x88, result.G);
		CornerstoneTest.AreEqual(0x44, result.B);
		CornerstoneTest.AreEqual(0xff, result.A);
	}

	[PresentationTestMethod]
	public void TryParseParsesRGBHashShorthandColor()
	{
		var success = Color.TryParse("#f84", out var result);

		CornerstoneTest.IsTrue(success);
		CornerstoneTest.AreEqual(0xff, result.R);
		CornerstoneTest.AreEqual(0x88, result.G);
		CornerstoneTest.AreEqual(0x44, result.B);
		CornerstoneTest.AreEqual(0xff, result.A);
	}

	[PresentationTestMethod]
	[DataRow("")]
	[DataRow(null)]
	public void TryParseReturnsFalseForInvalidInput(string input)
	{
		CornerstoneTest.IsFalse(Color.TryParse(input, out _));
	}

	#endregion
}