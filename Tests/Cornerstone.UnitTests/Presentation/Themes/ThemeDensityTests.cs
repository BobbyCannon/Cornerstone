#region References

using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Theme.Theming;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Themes;

[TestClass]
public class ThemeDensityTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetControlFontSizeLargeMatchesPresets()
	{
		AreEqual(14d, CornerstoneTheme.GetControlFontSizeLarge(ThemeDensity.Compact));
		AreEqual(16d, CornerstoneTheme.GetControlFontSizeLarge(ThemeDensity.Normal));
		AreEqual(18d, CornerstoneTheme.GetControlFontSizeLarge(ThemeDensity.Large));
	}

	[TestMethod]
	public void GetControlFontSizeMatchesPresets()
	{
		AreEqual(12d, CornerstoneTheme.GetControlFontSize(ThemeDensity.Compact));
		AreEqual(14d, CornerstoneTheme.GetControlFontSize(ThemeDensity.Normal));
		AreEqual(16d, CornerstoneTheme.GetControlFontSize(ThemeDensity.Large));
	}

	[TestMethod]
	public void GetControlFontSizeSmallMatchesPresets()
	{
		AreEqual(11d, CornerstoneTheme.GetControlFontSizeSmall(ThemeDensity.Compact));
		AreEqual(12d, CornerstoneTheme.GetControlFontSizeSmall(ThemeDensity.Normal));
		AreEqual(14d, CornerstoneTheme.GetControlFontSizeSmall(ThemeDensity.Large));
	}

	[TestMethod]
	public void NormalizeUnknownFallsBackToNormal()
	{
		AreEqual(ThemeDensity.Normal, CornerstoneTheme.NormalizeThemeDensity((ThemeDensity) 99));
		AreEqual(ThemeDensity.Compact, CornerstoneTheme.NormalizeThemeDensity(ThemeDensity.Compact));
	}

	#endregion
}