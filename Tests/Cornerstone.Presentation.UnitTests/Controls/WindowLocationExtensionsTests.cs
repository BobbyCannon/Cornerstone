#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class WindowLocationExtensionsTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CaptureWindowLocationSkipsMinimizedOffScreenPosition()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var windowImpl = new StubWindowImpl();
			windowImpl.Position = new PixelPoint(120, 80);
			var window = new Window(windowImpl)
			{
				Width = 1100,
				Height = 700
			};
			var stored = new WindowLocation
			{
				Left = 120,
				Top = 80,
				Width = 1100,
				Height = 700,
				Maximized = false
			};

			window.WindowState = WindowState.Minimized;
			windowImpl.Position = new PixelPoint(-32000, -32000);
			window.CaptureWindowLocation(stored);

			CornerstoneTest.AreEqual(120, stored.Left);
			CornerstoneTest.AreEqual(80, stored.Top);
			CornerstoneTest.AreEqual(1100, stored.Width);
			CornerstoneTest.AreEqual(700, stored.Height);
			CornerstoneTest.AreEqual(false, stored.Maximized);
		}
	}

	[PresentationTestMethod]
	public void CaptureWindowLocationUpdatesWhenNormal()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var windowImpl = new StubWindowImpl();
			windowImpl.Position = new PixelPoint(40, 60);
			var window = new Window(windowImpl)
			{
				Width = 1280,
				Height = 720
			};
			var stored = new WindowLocation();

			window.CaptureWindowLocation(stored);

			CornerstoneTest.AreEqual(40, stored.Left);
			CornerstoneTest.AreEqual(60, stored.Top);
			CornerstoneTest.AreEqual(1280, stored.Width);
			CornerstoneTest.AreEqual(720, stored.Height);
			CornerstoneTest.AreEqual(false, stored.Maximized);
		}
	}

	[PresentationTestMethod]
	public void RestoreWindowLocationAppliesSizeAfterDefaults()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var workingArea = new PixelRect(0, 0, 1920, 1080);
			var screen = new MockScreen(1, workingArea, workingArea, true);
			var windowImpl = new StubWindowImpl
			{
				Screens = new StubScreenImpl(screen)
			};
			var window = new Window(windowImpl)
			{
				Width = 1100,
				Height = 700
			};
			var location = new WindowLocation
			{
				Left = 200,
				Top = 100,
				Width = 1400,
				Height = 900,
				Maximized = false
			};

			window.RestoreWindowLocation(location);

			CornerstoneTest.AreEqual(1400, window.Width);
			CornerstoneTest.AreEqual(900, window.Height);
			CornerstoneTest.AreEqual(new PixelPoint(200, 100), window.Position);
		}
	}

	#endregion
}