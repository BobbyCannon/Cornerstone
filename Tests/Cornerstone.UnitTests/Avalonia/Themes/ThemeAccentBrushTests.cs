#region References

using Avalonia.Media;
using Cornerstone.Avalonia.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Themes;

[TestClass]
public class ThemeAccentBrushTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetAccentBrushDefaultsToBlue()
	{
		var brush = Theme.GetAccentBrush() as ISolidColorBrush;
		IsTrue(brush != null);
		var expected = Color.Parse(ThemeColorPalette.Blue.Color.Color);
		AreEqual(expected, brush.Color);
	}

	#endregion
}
