#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ContextMenuTests : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _mouse = new();
	private StubWindowImpl _popupImpl;

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CanSetClearContextMenuProperty()
	{
		using (Application())
		{
			var target = new ContextMenu();
			var control = new Panel();

			control.ContextMenu = target;
			control.ContextMenu = null;
		}
	}

	[PresentationTestMethod]
	public void CancelLightDismissClosingKeepsFlyoutOpen()
	{
		using (Application())
		{
			var window = PreparedWindow();
			window.Width = 100;
			window.Height = 100;

			var button = new Button
			{
				Height = 10,
				Width = 10,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			window.Content = button;

			window.ApplyTemplate();
			window.Show();

			var tracker = 0;

			var c = new ContextMenu();
			c.Closing += (s, e) =>
			{
				tracker++;
				e.Cancel = true;
			};
			button.ContextMenu = c;
			c.Open(button);

			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);
			_mouse.Down(overlay, MouseButton.Left, new Point(90, 90));
			_mouse.Up(button, MouseButton.Left, new Point(90, 90));

			CornerstoneTest.AreEqual(1, tracker);
			CornerstoneTest.IsTrue(c.IsOpen);

			_popupImpl.Calls.VerifyNotCalled("Hide");
			_popupImpl.Calls.VerifyCalled("Show", 1);
		}
	}

	[PresentationTestMethod]
	public void CancellingClosingLeavesContextMenuOpen()
	{
		using (Application())
		{
			var eventCalled = false;
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = PreparedWindow(target);
			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);

			sut.Closing += (c, e) =>
			{
				eventCalled = true;
				e.Cancel = true;
			};

			window.Show();

			_mouse.Click(target, MouseButton.Right);

			CornerstoneTest.IsTrue(sut.IsOpen);

			_mouse.Down(overlay, MouseButton.Right);
			_mouse.Up(target, MouseButton.Right);

			CornerstoneTest.IsTrue(eventCalled);
			CornerstoneTest.IsTrue(sut.IsOpen);

			_popupImpl.Calls.VerifyCalled("Show", 1);
			_popupImpl.Calls.VerifyNotCalled("Hide");
		}
	}

	[PresentationTestMethod]
	public void CancellingOpeningDoesNotShowContextMenu()
	{
		using (Application())
		{
			var eventCalled = false;
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};
			new Window { Content = target };

			sut.Opening += (c, e) =>
			{
				eventCalled = true;
				e.Cancel = true;
			};

			_mouse.Click(target, MouseButton.Right);

			CornerstoneTest.IsTrue(eventCalled);
			CornerstoneTest.IsFalse(sut.IsOpen);
			_popupImpl.Calls.VerifyNotCalled("Show");
		}
	}

	[PresentationTestMethod]
	public void ClickingOnControlTogglesContextMenu()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = PreparedWindow(target);
			window.Show();
			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);

			_mouse.Click(target, MouseButton.Right);

			CornerstoneTest.IsTrue(sut.IsOpen);

			_mouse.Down(overlay);
			_mouse.Up(target);

			CornerstoneTest.IsFalse(sut.IsOpen);
			_popupImpl.Calls.VerifyCalled("Show", 1);
			_popupImpl.Calls.VerifyCalled("Hide", 1);
		}
	}

	[PresentationTestMethod]
	public void ClosingRaisesSingleClosedEvent()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = new Window { Content = target };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			sut.Open(target);

			var closedCount = 0;

			sut.Closed += (sender, args) => { closedCount++; };

			sut.Close();

			CornerstoneTest.AreEqual(1, closedCount);
		}
	}

	[PresentationTestMethod]
	public void ClosingShouldRestoreFocus()
	{
		using (Application())
		{
			var item = new MenuItem();
			var sut = new ContextMenu
			{
				Items = { item }
			};

			var button = new Button();
			var target = new Panel
			{
				Children =
				{
					button
				},
				ContextMenu = sut
			};

			var window = PreparedWindow(target);
			var focusManager = CornerstoneTest.IsType<FocusManager>(window.FocusManager);

			// Show the window and focus the button.
			window.Show();
			button.Focus();
			CornerstoneTest.Same(button, focusManager.GetFocusedElement());

			// Click to show the context menu.
			_mouse.Click(target, MouseButton.Right);
			CornerstoneTest.IsTrue(sut.IsOpen);

			// Hover over the context menu item: this should focus it.
			_mouse.Enter(item);
			CornerstoneTest.Same(item, focusManager.GetFocusedElement());

			// Click the menu item to close the menu.
			_mouse.Click(item);
			CornerstoneTest.IsFalse(sut.IsOpen);

			// Focus should be restored to the button.
			CornerstoneTest.Same(button, focusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void ContextMenuCanBeSetInStyle()
	{
		using (Application())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='ContextMenu'>
                <ContextMenu>
                    <MenuItem>Foo</MenuItem>
                </ContextMenu>
            </Setter>
        </Style>
	</Window.Styles>

    <StackPanel>
        <TextBlock Name='target1'/>
        <TextBlock Name='target2'/>
    </StackPanel>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target1 = window.Get<TextBlock>("target1");
			var target2 = window.Get<TextBlock>("target2");
			var mouse = new MouseTestHelper();

			CornerstoneTest.IsNotNull(target1.ContextMenu);
			CornerstoneTest.IsNotNull(target2.ContextMenu);
			CornerstoneTest.Same(target1.ContextMenu, target2.ContextMenu);

			window.Show();

			var menu = target1.ContextMenu;
			mouse.Click(target1, MouseButton.Right);
			CornerstoneTest.IsTrue(menu.IsOpen);
			mouse.Click(target2, MouseButton.Right);
			CornerstoneTest.IsTrue(menu.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void ContextMenuCanBeSharedBetweenControlsEvenAfterAControlIsRemovedFromVisualTree()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target1 = new Panel
			{
				ContextMenu = sut
			};

			var target2 = new Panel
			{
				ContextMenu = sut
			};

			var sp = new StackPanel { Children = { target1, target2 } };
			var window = new Window { Content = sp };

			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			_mouse.Click(target1, MouseButton.Right);

			CornerstoneTest.IsTrue(sut.IsOpen);

			sp.Children.Remove(target1);

			CornerstoneTest.IsFalse(sut.IsOpen);

			_mouse.Click(target2, MouseButton.Right);

			CornerstoneTest.IsTrue(sut.IsOpen);
		}
	}

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void ContextMenuInResourcesCanBeShared()
	{
		using (Application())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ContextMenu x:Key='contextMenu'>
            <MenuItem>Foo</MenuItem>
        </ContextMenu>
	</Window.Resources>

    <StackPanel>
        <TextBlock Name='target1' ContextMenu='{StaticResource contextMenu}'/>
        <TextBlock Name='target2' ContextMenu='{StaticResource contextMenu}'/>
    </StackPanel>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target1 = window.Get<TextBlock>("target1");
			var target2 = window.Get<TextBlock>("target2");
			var mouse = new MouseTestHelper();

			CornerstoneTest.IsNotNull(target1.ContextMenu);
			CornerstoneTest.IsNotNull(target2.ContextMenu);
			CornerstoneTest.Same(target1.ContextMenu, target2.ContextMenu);

			window.Show();

			var menu = target1.ContextMenu;
			mouse.Click(target1, MouseButton.Right);
			CornerstoneTest.IsTrue(menu.IsOpen);
			mouse.Click(target2, MouseButton.Right);
			CornerstoneTest.IsTrue(menu.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void ContextMenuIsOpenedWhenContextFlyoutIsAlsoSet()
	{
		// We have this test for backwards compatability with the code that already sets custom ContextMenu.
		using (Application())
		{
			var sut = new ContextMenu();
			var flyout = new Flyout();
			var target = new Panel
			{
				ContextMenu = sut,
				ContextFlyout = flyout
			};

			var window = new Window { Content = target };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			target.RaiseEvent(new ContextRequestedEventArgs());

			CornerstoneTest.IsTrue(sut.IsOpen);
			CornerstoneTest.IsFalse(flyout.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void ContextRequestedOpensContextMenu()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = new Window { Content = target };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			var openedCount = 0;

			sut.Opened += (sender, args) => { openedCount++; };

			target.RaiseEvent(new ContextRequestedEventArgs());

			CornerstoneTest.IsTrue(sut.IsOpen);
			CornerstoneTest.AreEqual(1, openedCount);
		}
	}

	[PresentationTestMethod]
	public void KeyUpRaisedOnFlyoutClosesOpenedContextMenu()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = PreparedWindow(target);
			window.Show();

			target.RaiseEvent(new ContextRequestedEventArgs());

			CornerstoneTest.IsTrue(sut.IsOpen);

			sut.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

			CornerstoneTest.IsFalse(sut.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void KeyUpRaisedOnTargetOpensContextFlyout()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};
			var contextRequestedCount = 0;
			target.AddHandler(InputElement.ContextRequestedEvent, (s, a) => contextRequestedCount++, RoutingStrategies.Tunnel);

			var window = PreparedWindow(target);
			window.Show();

			target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

			CornerstoneTest.IsTrue(sut.IsOpen);
			CornerstoneTest.AreEqual(1, contextRequestedCount);
		}
	}

	[PresentationTestMethod]
	public void LightDismissClosesFlyout()
	{
		using (Application())
		{
			var window = PreparedWindow();
			window.Width = 100;
			window.Height = 100;

			var button = new Button
			{
				Height = 10,
				Width = 10,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			window.Content = button;

			window.ApplyTemplate();
			window.Show();

			var c = new ContextMenu();
			c.Placement = PlacementMode.Bottom;
			c.Open(button);

			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);
			_mouse.Down(overlay, MouseButton.Left, new Point(90, 90));
			_mouse.Up(button, MouseButton.Left, new Point(90, 90));

			CornerstoneTest.IsFalse(c.IsOpen);
			_popupImpl.Calls.VerifyCalled("Hide", 1);
			_popupImpl.Calls.VerifyCalled("Show", 1);
		}
	}

	[PresentationTestMethod]
	public void OpenShouldRaiseExceptionIfAlreadyDetached()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = new Window { Content = target };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			target.ContextMenu = null;

			CornerstoneTest.Throws<Exception>(() => sut.Open());
		}
	}

	[PresentationTestMethod]
	public void OpenShouldUseDefaultControl()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = new Window { Content = target };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			var opened = false;

			sut.Opened += (sender, args) => { opened = true; };

			sut.Open();

			CornerstoneTest.IsTrue(opened);
		}
	}

	[PresentationTestMethod]
	public void OpeningRaisesSingleOpenedEvent()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = new Window { Content = target };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			var openedCount = 0;

			sut.Opened += (sender, args) => { openedCount++; };

			sut.Open(target);

			CornerstoneTest.AreEqual(1, openedCount);
		}
	}

	[PresentationTestMethod]
	public void RightClickingOnControlTwiceReOpensContextMenu()
	{
		using (Application())
		{
			var sut = new ContextMenu();
			var target = new Panel
			{
				ContextMenu = sut
			};

			var window = PreparedWindow(target);
			window.Show();

			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);

			_mouse.Click(target, MouseButton.Right);
			CornerstoneTest.IsTrue(sut.IsOpen);

			_mouse.Down(overlay, MouseButton.Right);
			_mouse.Up(target, MouseButton.Right);

			CornerstoneTest.IsTrue(sut.IsOpen);
			_popupImpl.Calls.VerifyCalled("Hide", 1);
			_popupImpl.Calls.VerifyCalled("Show", 2);
		}
	}

	[PresentationTestMethod]
	public void ShouldResetPopupParentOnTargetDetached()
	{
		using (Application())
		{
			var userControl = new UserControl();
			var window = PreparedWindow(userControl);
			window.Show();

			var menu = new ContextMenu();
			userControl.ContextMenu = menu;
			menu.Open();

			var popup = CornerstoneTest.IsType<Popup>(menu.Parent);
			CornerstoneTest.IsNotNull(popup.Parent);

			window.Content = null;
			CornerstoneTest.IsNull(popup.Parent);
		}
	}

	[MemberNotNull(nameof(_popupImpl))]
	private IDisposable Application()
	{
		var screen = new PixelRect(new PixelPoint(), new PixelSize(100, 100));
		var screenImpl = new StubScreenImpl(new MockScreen(1, screen, screen, true));

		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		_popupImpl = MockWindowingPlatform.CreatePopupMock(windowImpl);
		windowImpl.CreatePopupHandler = () => _popupImpl;
		windowImpl.Screens = screenImpl;

		var services = TestServices.StyledWindow.With(
			keyboardDevice: () => new KeyboardDevice(),
			inputManager: new InputManager(),
			windowImpl: windowImpl,
			windowingPlatform: new MockWindowingPlatform(() => windowImpl, x => _popupImpl));

		return UnitTestApplication.Start(services);
	}

	private static Window PreparedWindow(object content = null)
	{
		var platform = PresentationLocator.Current.GetRequiredService<IWindowingPlatform>();
		var windowImpl = platform.CreateWindow();
		var w = new Window(windowImpl) { Content = content };
		w.ApplyStyling();
		w.ApplyTemplate();
		w.Presenter!.ApplyTemplate();
		return w;
	}

	#endregion
}