#region References

using Cornerstone.Presentation.Theme.Theming;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Themes;

[TestClass]
public class ThemeCssWriterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EmitsDensityFontSizes()
	{
		var css = ThemeCssWriter.Write();
		IsTrue(css.Contains(":root[data-density=\"compact\"]"));
		IsTrue(css.Contains(":root[data-density=\"normal\"]"));
		IsTrue(css.Contains(":root[data-density=\"large\"]"));
		IsTrue(css.Contains("--ControlFontSize: 12px;"));
		IsTrue(css.Contains("--ControlFontSizeSmall: 11px;"));
		IsTrue(css.Contains("--ControlFontSizeLarge: 14px;"));
		IsTrue(css.Contains("--ControlFontSize: 16px;"));
		IsTrue(css.Contains("--ControlFontSizeSmall: 14px;"));
		IsTrue(css.Contains("--ControlFontSizeLarge: 18px;"));
	}

	[TestMethod]
	public void EmitsWpfLightAndDarkBrushes()
	{
		var xaml = ThemeWpfWriter.Write();
		IsTrue(xaml.Contains("x:Key=\"Light.Background00\">#FFFFFF"));
		IsTrue(xaml.Contains("x:Key=\"Dark.Background00\">#000000"));
		IsTrue(xaml.Contains("x:Key=\"Light.Background00Brush\""));
		IsTrue(xaml.Contains("x:Key=\"Dark.ThemeBlueBrush\""));
	}

	[TestMethod]
	public void EmitsLightAndDarkBackgroundRamps()
	{
		var css = ThemeCssWriter.Write();
		IsTrue(css.Contains(":root, [data-theme=\"light\"]"));
		IsTrue(css.Contains("[data-theme=\"dark\"]"));
		IsTrue(css.Contains("--Background00: #FFFFFF;"));
		IsTrue(css.Contains("--Foreground00: #000000;"));
		IsTrue(css.Contains("--Background00: #000000;"));
		IsTrue(css.Contains("--Foreground00: #FFFFFF;"));
		IsTrue(css.Contains("--Theme-Blue:"));
		IsTrue(css.Contains("--Theme-Accent: var(--Theme-Blue)"));
		IsTrue(css.Contains(":root[data-theme-color=\"Blue\"]"));
		IsTrue(css.Contains(":root[data-density=\"compact\"]"));
		IsTrue(css.Contains("--BorderBrush:"));
	}

	#endregion
}