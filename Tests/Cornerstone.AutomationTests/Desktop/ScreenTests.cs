#region References

using System.Linq;
using Cornerstone.Automation.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests.Desktop;

[TestClass]
public class ScreenTests
{
	#region Methods

	[TestMethod]
	public void FromPoint()
	{
		var primaryScreen = Screen.PrimaryScreen;
		Assert.AreEqual(primaryScreen, Screen.FromPoint(0, 0));
		Assert.AreEqual(primaryScreen, Screen.FromPoint(primaryScreen.WorkingArea.Width - 1, 0));
		Assert.AreEqual(primaryScreen, Screen.FromPoint(0, primaryScreen.WorkingArea.Height - 1));
		Assert.AreEqual(primaryScreen, Screen.FromPoint(primaryScreen.WorkingArea.Width - 1, primaryScreen.WorkingArea.Height - 1));
		Assert.AreNotEqual(primaryScreen, Screen.FromPoint(primaryScreen.WorkingArea.Width, 0));
	}

	[TestMethod]
	public void MultiMonitorSupport()
	{
		Assert.IsTrue(Screen.MultipleScreenSupport);

		var actual = Screen.AllScreens.Skip(1).Take(1).First();
		Assert.AreEqual(-1920, actual.Location.X);
		Assert.AreEqual(0, actual.Location.Y);
		Assert.AreEqual(1280, actual.Size.Width);
		Assert.AreEqual(800, actual.Size.Height);
		Assert.AreEqual(-1920, actual.ScreenArea.X);
		Assert.AreEqual(0, actual.ScreenArea.Y);
		Assert.AreEqual(1280, actual.ScreenArea.Width);
		Assert.AreEqual(800, actual.ScreenArea.Height);
		Assert.AreEqual(-1920, actual.WorkingArea.X);
		Assert.AreEqual(0, actual.WorkingArea.Y);
		Assert.AreEqual(1280, actual.WorkingArea.Width);
		Assert.AreEqual(752, actual.WorkingArea.Height);
		Assert.AreEqual(@"\\.\DISPLAY2", actual.DeviceName);
	}

	[TestMethod]
	public void PrimaryScreenSize()
	{
		var actual = Screen.PrimaryScreen;
		Assert.AreEqual(0, actual.Location.X);
		Assert.AreEqual(0, actual.Location.Y);
		Assert.AreEqual(3440, actual.Size.Width);
		Assert.AreEqual(1440, actual.Size.Height);
		Assert.AreEqual(0, actual.ScreenArea.X);
		Assert.AreEqual(0, actual.ScreenArea.Y);
		Assert.AreEqual(3440, actual.ScreenArea.Width);
		Assert.AreEqual(1440, actual.ScreenArea.Height);
		Assert.AreEqual(0, actual.WorkingArea.X);
		Assert.AreEqual(0, actual.WorkingArea.Y);
		Assert.AreEqual(3440, actual.WorkingArea.Width);
		Assert.AreEqual(1392, actual.WorkingArea.Height);
		Assert.AreEqual(@"\\.\DISPLAY1", actual.DeviceName);
	}

	[TestMethod]
	public void SecondaryScreenSize()
	{
		var actual = Screen.AllScreens.FirstOrDefault(x => !x.IsPrimary);
		Assert.IsNotNull(actual);
		Assert.AreEqual(-1920, actual.Location.X);
		Assert.AreEqual(0, actual.Location.Y);
		Assert.AreEqual(1280, actual.Size.Width);
		Assert.AreEqual(800, actual.Size.Height);
		Assert.AreEqual(-1920, actual.ScreenArea.X);
		Assert.AreEqual(0, actual.ScreenArea.Y);
		Assert.AreEqual(1280, actual.ScreenArea.Width);
		Assert.AreEqual(800, actual.ScreenArea.Height);
		Assert.AreEqual(-1920, actual.WorkingArea.X);
		Assert.AreEqual(0, actual.WorkingArea.Y);
		Assert.AreEqual(1280, actual.WorkingArea.Width);
		Assert.AreEqual(752, actual.WorkingArea.Height);
		Assert.AreEqual("\\\\.\\DISPLAY2", actual.DeviceName);
	}

	[TestMethod]
	public void VirtualScreenSize()
	{
		var actual = Screen.VirtualScreenSize;
		Assert.AreEqual(-1920, actual.X);
		Assert.AreEqual(0, actual.Y);
		Assert.AreEqual(5360, actual.Width);
		Assert.AreEqual(1440, actual.Height);
	}

	#endregion
}