#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class MouseDeviceTests
{
	#region Methods

	[HeadlessTestMethod]
	public void PointerCaptureCrossesTopLevelsWhenDeviceIsShared()
	{
		var popupChild = new Border { Width = 80, Height = 30, Background = Brushes.Blue };
		var target = new Border { Background = Brushes.Red };
		var popup = new Popup { PlacementTarget = target, Child = popupChild };
		var window = new Window
		{
			Width = 100,
			Height = 100,
			Content = new Panel { Children = { target, popup } }
		};
		window.Show();
		Dispatcher.UIThread.RunJobs();

		popup.Open();
		Dispatcher.UIThread.RunJobs();

		object moveTarget = null;
		target.PointerMoved += (s, _) => moveTarget = s;
		popupChild.PointerMoved += (s, _) => moveTarget = s;

		// Pressing captures the pointer implicitly on the window's border.
		window.MouseDown(new Point(50, 50), MouseButton.Left);

		var popupRoot = PopupTests.GetPopupTopLevel(popup);
		AssertHelper.NotNull(popupRoot);

		popupRoot.MouseMove(new Point(40, 15));

		AssertHelper.Same(TestApplication.UsesSharedMouseDevice ? target : popupChild, moveTarget);

		window.MouseUp(new Point(50, 50), MouseButton.Left);
		window.Close();
	}

	[HeadlessTestMethod]
	public void PointerIsSharedBetweenWindowsWhenRequested()
	{
		var firstWindow = CreateWindow(out var firstTarget);
		var secondWindow = CreateWindow(out var secondTarget);

		IPointer firstPointer = null, secondPointer = null;
		firstTarget.PointerPressed += (_, e) => firstPointer = e.Pointer;
		secondTarget.PointerPressed += (_, e) => secondPointer = e.Pointer;

		Click(firstWindow);
		Click(secondWindow);

		AssertHelper.NotNull(firstPointer);
		AssertHelper.NotNull(secondPointer);

		if (TestApplication.UsesSharedMouseDevice)
		{
			AssertHelper.Same(firstPointer, secondPointer);
		}
		else
		{
			AssertHelper.NotSame(firstPointer, secondPointer);
		}

		firstWindow.Close();
		secondWindow.Close();
	}

	private static void Click(Window window)
	{
		window.MouseDown(new Point(50, 50), MouseButton.Left);
		window.MouseUp(new Point(50, 50), MouseButton.Left);
	}

	private static Window CreateWindow(out Border target)
	{
		target = new Border { Background = Brushes.Red };
		var window = new Window { Width = 100, Height = 100, Content = target };
		window.Show();
		Dispatcher.UIThread.RunJobs();
		return window;
	}

	#endregion
}