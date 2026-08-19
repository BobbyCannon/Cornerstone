#region References

using Cornerstone.Avalonia.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Themes;

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
		IsTrue(css.Contains("--ControlFontSize: 16px;"));
		IsTrue(css.Contains("--ControlFontSizeSmall: 14px;"));
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
