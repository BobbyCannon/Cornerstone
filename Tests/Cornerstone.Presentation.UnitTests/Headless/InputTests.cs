#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
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
public class InputTests : IDisposable
{
	#region Fields

	private readonly Application _setupApp;
	private readonly Window _window;

	#endregion

	#region Constructors

	public InputTests()
	{
		_setupApp = Application.Current;
		Dispatcher.UIThread.VerifyAccess();
		_window = new Window
		{
			Width = 100,
			Height = 100
		};
	}

	#endregion

	#region Methods

	[HeadlessTestMethod]
	public void ChangeWindowPosition()
	{
		var newWindowPosition = new PixelPoint(100, 150);
		_window.Position = newWindowPosition;
		_window.Show();
		AssertHelper.Equal(newWindowPosition, _window.Position);
	}

	public void Dispose()
	{
		AssertHelper.Same(_setupApp, Application.Current);

		Dispatcher.UIThread.VerifyAccess();
		_window.Close();
	}

	[HeadlessTestMethod]
	public void DisposingTouchPointerCancelsContact()
	{
		var captureLostCount = 0;
		var releasedCount = 0;

		var border = new Border { Background = Brushes.Red };
		border.PointerCaptureLost += (_, _) => captureLostCount++;
		border.PointerReleased += (_, _) => releasedCount++;

		_window.Content = border;
		_window.Show();

		using (_window.TouchBegin(new Point(50, 50)))
		{
		}

		AssertHelper.Equal(1, captureLostCount);
		AssertHelper.Equal(0, releasedCount);
	}

	[HeadlessTestMethod]
	public void MultipleTouchContactsAreDistinctPointers()
	{
		var pointerIds = new HashSet<int>();

		var border = new Border { Background = Brushes.Red };
		border.PointerPressed += (_, e) => pointerIds.Add(e.Pointer.Id);

		_window.Content = border;
		_window.Show();

		var touch1 = _window.TouchBegin(new Point(30, 30));
		var touch2 = _window.TouchBegin(new Point(70, 70));
		_window.TouchEnd(touch1, new Point(30, 30));
		_window.TouchEnd(touch2, new Point(70, 70));

		AssertHelper.Equal(2, pointerIds.Count);
	}

	[HeadlessTestMethod]
	public void ShouldClickButtonAfterExplicitRunJobs()
	{
		// Regression test for https://github.com/AvaloniaUI/Avalonia/issues/20309
		// Ensure that calling Threading.Dispatcher.UIThread.RunJobs() before MouseDown does not throw
		var button = new Button { Content = "Test content" };
		_window.Content = button;
		_window.Show();

		Dispatcher.UIThread.RunJobs();

		var clickCount = 0;
		button.Click += (_, _) => clickCount++;

		var point = new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
		var translatePoint = button.TranslatePoint(point, _window);

		// Move
		_window.MouseMove(translatePoint!.Value, RawInputModifiers.None);

		// Click
		_window.MouseDown(translatePoint.Value, MouseButton.Left, RawInputModifiers.None);
		_window.MouseUp(translatePoint.Value, MouseButton.Left, RawInputModifiers.None);

		AssertHelper.Equal(1, clickCount);
	}

	[HeadlessTestMethod]
	public void ShouldClickButtonOnWindow()
	{
		AssertHelper.Same(_setupApp, Application.Current);
		var buttonClicked = false;
		var button = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		button.Click += (_, _) => buttonClicked = true;

		_window.Content = button;
		_window.Show();

		_window.MouseDown(new Point(50, 50), MouseButton.Left);
		_window.MouseUp(new Point(50, 50), MouseButton.Left);

		AssertHelper.True(buttonClicked);
	}

	[HeadlessTestMethod]
	public void TouchContactRaisesTouchPointerEvents()
	{
		var pressedCount = 0;
		var movedCount = 0;
		var releasedCount = 0;
		PointerType pressedPointerType = default;

		var border = new Border { Background = Brushes.Red };
		border.PointerPressed += (_, e) =>
		{
			pressedCount++;
			pressedPointerType = e.Pointer.Type;
		};
		border.PointerMoved += (_, _) => movedCount++;
		border.PointerReleased += (_, _) => releasedCount++;

		_window.Content = border;
		_window.Show();

		var touch = _window.TouchBegin(new Point(50, 50));
		_window.TouchMove(touch, new Point(60, 60));
		_window.TouchEnd(touch, new Point(60, 60));

		AssertHelper.Equal(1, pressedCount);
		AssertHelper.Equal(1, movedCount);
		AssertHelper.Equal(1, releasedCount);
		AssertHelper.Equal(PointerType.Touch, pressedPointerType);
	}

	#endregion
}