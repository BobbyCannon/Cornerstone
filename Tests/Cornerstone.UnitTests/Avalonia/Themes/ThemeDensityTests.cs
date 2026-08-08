#region References

using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Themes;

[TestClass]
public class ThemeDensityTests : CornerstoneUnitTest
{
	#region Methods

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