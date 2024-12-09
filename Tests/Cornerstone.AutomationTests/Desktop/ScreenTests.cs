#region References

using System;
using System.Linq;
using Cornerstone.UnitTests;
using Cornerstone.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests.Desktop;

[TestClass]
public class ScreenTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FromPoint()
	{
		var primaryScreen = Screen.PrimaryScreen;
		AreEqual(primaryScreen, Screen.FromPoint(0, 0));
		AreEqual(primaryScreen, Screen.FromPoint(primaryScreen.WorkingArea.Width - 1, 0));
		AreEqual(primaryScreen, Screen.FromPoint(0, primaryScreen.WorkingArea.Height - 1));
		AreEqual(primaryScreen, Screen.FromPoint(primaryScreen.WorkingArea.Width - 1, primaryScreen.WorkingArea.Height - 1));
		AreNotEqual(primaryScreen, Screen.FromPoint(primaryScreen.WorkingArea.Width, 0));
	}

	[TestMethod]
	public void MultiMonitorSupport()
	{
		if (!string.Equals(GetRuntimeInformation().DeviceName, "bobbys-rig", StringComparison.OrdinalIgnoreCase))
		{
			Assert.Inconclusive("Device not recognized");
		}

		IsTrue(Screen.MultipleScreenSupport);
		
		var actual = Screen.AllScreens.Skip(1).Take(1).First();
		AreEqual(1529, actual.Location.X);
		AreEqual(1440, actual.Location.Y);
		AreEqual(1280, actual.Size.Width);
		AreEqual(800, actual.Size.Height);
		AreEqual(1529, actual.ScreenArea.X);
		AreEqual(1440, actual.ScreenArea.Y);
		AreEqual(1280, actual.ScreenArea.Width);
		AreEqual(800, actual.ScreenArea.Height);
		AreEqual(1529, actual.WorkingArea.X);
		AreEqual(1440, actual.WorkingArea.Y);
		AreEqual(1280, actual.WorkingArea.Width);
		AreEqual(752, actual.WorkingArea.Height);
		AreEqual(@"\\.\DISPLAY2", actual.DeviceName);
	}

	[TestMethod]
	public void PrimaryScreenSize()
	{
		var actual = Screen.PrimaryScreen;
		AreEqual(0, actual.Location.X);
		AreEqual(0, actual.Location.Y);
		AreEqual(3440, actual.Size.Width);
		AreEqual(1440, actual.Size.Height);
		AreEqual(0, actual.ScreenArea.X);
		AreEqual(0, actual.ScreenArea.Y);
		AreEqual(3440, actual.ScreenArea.Width);
		AreEqual(1440, actual.ScreenArea.Height);
		AreEqual(0, actual.WorkingArea.X);
		AreEqual(0, actual.WorkingArea.Y);
		AreEqual(3440, actual.WorkingArea.Width);
		AreEqual(1392, actual.WorkingArea.Height);
		AreEqual(@"\\.\DISPLAY1", actual.DeviceName);
	}

	[TestMethod]
	public void SecondaryScreenSize()
	{
		if (!string.Equals(GetRuntimeInformation().DeviceName, "bobbys-rig", StringComparison.OrdinalIgnoreCase))
		{
			Assert.Inconclusive("Device not recognized");
		}

		var actual = Screen.AllScreens.FirstOrDefault(x => !x.IsPrimary);
		Assert.IsNotNull(actual);

		AreEqual(1529, actual.Location.X);
		AreEqual(1440, actual.Location.Y);
		AreEqual(1280, actual.Size.Width);
		AreEqual(800, actual.Size.Height);
		AreEqual(1529, actual.ScreenArea.X);
		AreEqual(1440, actual.ScreenArea.Y);
		AreEqual(1280, actual.ScreenArea.Width);
		AreEqual(800, actual.ScreenArea.Height);
		AreEqual(1529, actual.WorkingArea.X);
		AreEqual(1440, actual.WorkingArea.Y);
		AreEqual(1280, actual.WorkingArea.Width);
		AreEqual(752, actual.WorkingArea.Height);
		AreEqual("\\\\.\\DISPLAY2", actual.DeviceName);
	}

	[TestMethod]
	public void VirtualScreenSize()
	{
		if (!string.Equals(GetRuntimeInformation().DeviceName, "bobbys-rig", StringComparison.OrdinalIgnoreCase))
		{
			Assert.Inconclusive("Device not recognized");
		}

		var actual = Screen.VirtualScreenSize;
		AreEqual(0, actual.X);
		AreEqual(0, actual.Y);
		AreEqual(3440, actual.Width);
		AreEqual(2240, actual.Height);
	}

	#endregion
}