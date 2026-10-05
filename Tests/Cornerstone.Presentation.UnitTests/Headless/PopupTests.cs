#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class PopupTests
{
	#region Methods

	[HeadlessTestMethod]
	public void CanClickButtonInsidePlatformPopup()
	{
		var clickCount = 0;
		var button = new Button { Width = 80, Height = 30 };
		button.Click += (_, _) => clickCount++;

		var target = new Border { Background = Brushes.Red };
		var popup = new Popup { PlacementTarget = target, Child = button };
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

		var popupRoot = GetPopupTopLevel(popup);
		AssertHelper.NotNull(popupRoot);

		popupRoot.MouseDown(new Point(40, 15), MouseButton.Left);
		popupRoot.MouseUp(new Point(40, 15), MouseButton.Left);

		AssertHelper.Equal(1, clickCount);

		window.Close();
	}

	[HeadlessTestMethod]
	public void NestedPopupIsOwnedByParentPopup()
	{
		var nestedTarget = new Border { Width = 20, Height = 20, Background = Brushes.Green };
		var nestedPopup = new Popup
		{
			PlacementTarget = nestedTarget,
			Child = new Border { Width = 10, Height = 10 }
		};
		var target = new Border { Background = Brushes.Red };
		var popup = new Popup
		{
			PlacementTarget = target,
			Child = new Panel { Children = { nestedTarget, nestedPopup } }
		};
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
		nestedPopup.Open();
		Dispatcher.UIThread.RunJobs();

		AssertHelper.Equal(1, window.OpenedPopups.Count);
		AssertHelper.Same(popup, window.OpenedPopups[0]);

		AssertHelper.Equal(1, popup.OpenedPopups.Count);
		AssertHelper.Same(nestedPopup, popup.OpenedPopups[0]);
		AssertHelper.Equal(0, nestedPopup.OpenedPopups.Count);

		// The nested popup is hosted in the parent popup's own top level.
		AssertHelper.Same(GetPopupTopLevel(popup), TopLevel.GetTopLevel(nestedTarget));

		nestedPopup.Close();
		Dispatcher.UIThread.RunJobs();

		AssertHelper.Equal(0, popup.OpenedPopups.Count);
		AssertHelper.Equal(1, window.OpenedPopups.Count);

		popup.Close();
		Dispatcher.UIThread.RunJobs();

		AssertHelper.Equal(0, window.OpenedPopups.Count);

		window.Close();
	}

	[HeadlessTestMethod]
	public void PointToScreenRespectsWindowPosition()
	{
		var window = new Window { Width = 100, Height = 100 };
		window.Position = new PixelPoint(100, 200);
		window.Show();
		Dispatcher.UIThread.RunJobs();

		AssertHelper.Equal(new PixelPoint(110, 220), window.PointToScreen(new Point(10, 20)));
		AssertHelper.Equal(new Point(10, 20), window.PointToClient(new PixelPoint(110, 220)));

		window.Close();
	}

	[HeadlessTestMethod]
	public void PopupPlacementRespectsWindowPosition()
	{
		var target = new Border
		{
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Background = Brushes.Red
		};
		var popup = new Popup
		{
			PlacementTarget = target,
			Placement = PlacementMode.Bottom,
			Child = new Border { Width = 20, Height = 20 }
		};
		var window = new Window
		{
			Width = 100,
			Height = 100,
			Content = new Panel { Children = { target, popup } }
		};
		window.Position = new PixelPoint(100, 200);
		window.Show();
		Dispatcher.UIThread.RunJobs();

		popup.Open();
		Dispatcher.UIThread.RunJobs();

		var popupRoot = GetPopupTopLevel(popup);
		AssertHelper.NotNull(popupRoot);

		var expected = target.PointToScreen(new Point(0, target.Bounds.Height));
		AssertHelper.Equal(expected, popupRoot.PointToScreen(default));

		window.Close();
	}

	[HeadlessTestMethod]
	public void PopupUsesDedicatedTopLevel()
	{
		var target = new Border { Background = Brushes.Red };
		var popup = new Popup
		{
			PlacementTarget = target,
			Child = new Border { Width = 20, Height = 20 }
		};
		var window = new Window
		{
			Width = 100,
			Height = 100,
			Content = new Panel { Children = { target, popup } }
		};
		window.Show();
		Dispatcher.UIThread.RunJobs();

		AssertHelper.False(popup.IsOpen);
		AssertHelper.False(popup.IsUsingOverlayLayer);
		AssertHelper.Null(GetPopupTopLevel(popup));

		popup.Open();
		Dispatcher.UIThread.RunJobs();

		AssertHelper.True(popup.IsOpen);
		AssertHelper.False(popup.IsUsingOverlayLayer);
		AssertHelper.True(GetPopupTopLevel(popup) is PopupRoot);

		window.Close();

		AssertHelper.False(popup.IsOpen);
		AssertHelper.False(popup.IsUsingOverlayLayer);
		AssertHelper.Null(GetPopupTopLevel(popup));
	}

	internal static TopLevel GetPopupTopLevel(Popup popup)
	{
		AssertHelper.NotNull(popup.Child);
		var topLevel = TopLevel.GetTopLevel(popup.Child);
		return topLevel;
	}

	#endregion
}