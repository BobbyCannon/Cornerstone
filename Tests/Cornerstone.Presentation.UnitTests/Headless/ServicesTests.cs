#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class ServicesTests
{
	#region Methods

	[HeadlessTestMethod]
	public void CanAccessScreens()
	{
		var window = new Window();
		var screens = window.Screens;
		AssertHelper.NotNull(screens);

		var currentScreenFromWindow = screens.ScreenFromWindow(window);
		var currentScreenFromVisual = screens.ScreenFromVisual(window);

		AssertHelper.Same(currentScreenFromWindow, currentScreenFromVisual);
	}

	#endregion
}