#region References

using System;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class FlyoutTests : ScopedTestBase
{
	#region Properties

	protected bool UseOverlayPopups { get; set; }

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CancelClosingKeepsFlyoutOpen()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var tracker = 0;
			var f = new Flyout();
			f.Closing += (s, e) =>
			{
				tracker++;
				e.Cancel = true;
			};
			f.ShowAt(window);
			f.Hide();

			CornerstoneTest.IsTrue(f.IsOpen);
			CornerstoneTest.AreEqual(1, tracker);
		}
	}

	[PresentationTestMethod]
	public void CancelLightDismissClosingKeepsFlyoutOpen()
	{
		using (CreateServicesWithFocus())
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

			window.Show();

			var tracker = 0;
			var f = new Flyout();
			f.Content = new Border { Width = 10, Height = 10 };
			f.Closing += (s, e) =>
			{
				tracker++;
				e.Cancel = true;
			};
			f.ShowAt(window);

			var e = CreatePointerPressedEventArgs(window, new Point(90, 90));
			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);
			overlay.RaiseEvent(e);

			CornerstoneTest.AreEqual(1, tracker);
			CornerstoneTest.IsTrue(f.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void ClosingRaisesSingleClosedEvent()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var tracker = 0;
			var f = new Flyout();
			f.Closed += (s, e) => { tracker++; };
			f.ShowAt(window);
			f.Hide();

			CornerstoneTest.AreEqual(1, tracker);
		}
	}

	[PresentationTestMethod]
	public void ClosingRaisesSingleClosingEvent()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var tracker = 0;
			var f = new Flyout();
			f.Closing += (s, e) => { tracker++; };
			f.ShowAt(window);
			f.Hide();

			CornerstoneTest.AreEqual(1, tracker);
		}
	}

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void ContextFlyoutCanBeSetInStyles()
	{
		using (CreateServicesWithFocus())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='ContextFlyout'>
                <MenuFlyout>
                    <MenuItem>Foo</MenuItem>
                </MenuFlyout>
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

			CornerstoneTest.IsNotNull(target1.ContextFlyout);
			CornerstoneTest.IsNotNull(target2.ContextFlyout);
			CornerstoneTest.Same(target1.ContextFlyout, target2.ContextFlyout);

			window.Show();

			var menu = target1.ContextFlyout;
			mouse.Click(target1, MouseButton.Right);
			CornerstoneTest.IsTrue(menu.IsOpen);
			mouse.Click(target2, MouseButton.Right);
			CornerstoneTest.IsTrue(menu.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void ContextRequestedOpensContextFlyout()
	{
		using (CreateServicesWithFocus())
		{
			var flyout = new Flyout();
			var target = new Panel
			{
				ContextFlyout = flyout
			};

			var window = PreparedWindow(target);
			window.Show();

			var openedCount = 0;

			flyout.Opened += (sender, args) => { openedCount++; };

			target.RaiseEvent(new ContextRequestedEventArgs());

			CornerstoneTest.IsTrue(flyout.IsOpen);
			CornerstoneTest.AreEqual(1, openedCount);
		}
	}

	[PresentationTestMethod]
	public void FlyoutHasUncancellableCloseBeforeShowingOnADifferentTarget()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			var target1 = new Button();
			var target2 = new Button();

			window.Content = new StackPanel
			{
				Children =
				{
					target1,
					target2
				}
			};
			window.Show();

			var closingFired = false;
			var closedFired = false;
			var f = new Flyout();
			f.Closing += (s, e) =>
			{
				closingFired = true; //This shouldn't happen
			};
			f.Closed += (s, e) => { closedFired = true; };

			f.ShowAt(target1);

			f.ShowAt(target2);

			CornerstoneTest.IsFalse(closingFired);
			CornerstoneTest.IsTrue(closedFired);
		}
	}

	[PresentationTestMethod]
	public void IsOpenButtonFlyoutRemovedClearsTarget()
	{
		using (CreateServicesWithFocus())
		{
			var button = new Button();
			var window = PreparedWindow(button);
			window.Show();

			var flyout = new TestFlyout();
			button.Flyout = flyout;

			button.Flyout = null;

			flyout.IsOpen = true;

			CornerstoneTest.IsFalse(flyout.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void IsOpenSetFalseCancelledClosingRevertsToTrue()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var flyout = new TestFlyout();
			flyout.Closing += (s, e) => e.Cancel = true;

			flyout.ShowAt(window);
			CornerstoneTest.IsTrue(flyout.IsOpen);

			flyout.IsOpen = false;

			CornerstoneTest.IsTrue(flyout.IsOpen);
			CornerstoneTest.IsTrue(flyout.Popup.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void IsOpenSetFalseClosesFlyout()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var flyout = new TestFlyout();
			var closedFired = false;
			flyout.Closed += (s, e) => closedFired = true;

			flyout.ShowAt(window);
			CornerstoneTest.IsTrue(flyout.IsOpen);

			flyout.IsOpen = false;

			CornerstoneTest.IsFalse(flyout.IsOpen);
			CornerstoneTest.IsFalse(flyout.Popup.IsOpen);
			CornerstoneTest.IsTrue(closedFired);
		}
	}

	[PresentationTestMethod]
	public void IsOpenSetTrueAfterTargetDetachedRevertsToFalse()
	{
		using (CreateServicesWithFocus())
		{
			var target = new Button();
			var window = PreparedWindow(target);
			window.Show();

			var flyout = new TestFlyout();
			flyout.ShowAt(target);
			CornerstoneTest.IsTrue(flyout.IsOpen);

			// Detach the target from the visual tree
			window.Content = null;
			CornerstoneTest.IsFalse(flyout.IsOpen);

			flyout.IsOpen = true;

			CornerstoneTest.IsFalse(flyout.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void IsOpenSetTrueCancelledOpeningRevertsToFalse()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var flyout = new TestFlyout();
			flyout.ShowAt(window);
			flyout.Hide();

			flyout.Opening += (s, e) =>
			{
				if (e is CancelEventArgs cancelArgs)
				{
					cancelArgs.Cancel = true;
				}
			};

			flyout.IsOpen = true;

			CornerstoneTest.IsFalse(flyout.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void IsOpenSetTrueOpensAtButtonFlyoutOwner()
	{
		using (CreateServicesWithFocus())
		{
			var button = new Button();
			var window = PreparedWindow(button);
			window.Show();

			var flyout = new TestFlyout();
			button.Flyout = flyout;

			flyout.IsOpen = true;

			CornerstoneTest.IsTrue(flyout.IsOpen);
			CornerstoneTest.IsTrue(flyout.Popup.IsOpen);
			CornerstoneTest.AreEqual(button, flyout.Popup.PlacementTarget);
		}
	}

	[PresentationTestMethod]
	public void IsOpenSetTrueReopensAtLastTarget()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var flyout = new TestFlyout();
			flyout.ShowAt(window);
			CornerstoneTest.IsTrue(flyout.IsOpen);

			flyout.Hide();
			CornerstoneTest.IsFalse(flyout.IsOpen);

			flyout.IsOpen = true;

			CornerstoneTest.IsTrue(flyout.IsOpen);
			CornerstoneTest.IsTrue(flyout.Popup.IsOpen);
			CornerstoneTest.AreEqual(window, flyout.Popup.PlacementTarget);
		}
	}

	[PresentationTestMethod]
	public void IsOpenSetTrueWithoutPreviousTargetRevertsToFalse()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var flyout = new TestFlyout();

			flyout.IsOpen = true;

			CornerstoneTest.IsFalse(flyout.IsOpen);
			CornerstoneTest.IsFalse(flyout.Popup.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void IsOpenTwoWayBindingSyncsWithSource()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var viewModel = new FlyoutViewModel();
			var flyout = new TestFlyout();
			flyout.Bind(FlyoutBase.IsOpenProperty, new Binding(nameof(FlyoutViewModel.IsOpen))
			{
				Source = viewModel,
				Mode = BindingMode.TwoWay
			});

			CornerstoneTest.IsFalse(viewModel.IsOpen);

			flyout.ShowAt(window);
			CornerstoneTest.IsTrue(viewModel.IsOpen);

			flyout.Hide();
			CornerstoneTest.IsFalse(viewModel.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void KeyUpRaisedOnFlyoutClosesOpenedContextFlyout()
	{
		using (CreateServicesWithFocus())
		{
			var flyoutContent = new Button();
			var flyout = new Flyout
			{
				Content = flyoutContent
			};
			var target = new Panel
			{
				ContextFlyout = flyout
			};

			var window = PreparedWindow(target);
			window.Show();

			target.RaiseEvent(new ContextRequestedEventArgs());

			CornerstoneTest.IsTrue(flyout.IsOpen);

			flyoutContent.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

			CornerstoneTest.IsFalse(flyout.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void KeyUpRaisedOnTargetClosesOpenedContextFlyout()
	{
		using (CreateServicesWithFocus())
		{
			var flyout = new Flyout();
			var target = new Panel
			{
				ContextFlyout = flyout
			};

			var window = PreparedWindow(target);
			window.Show();

			target.RaiseEvent(new ContextRequestedEventArgs());

			CornerstoneTest.IsTrue(flyout.IsOpen);

			target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

			CornerstoneTest.IsFalse(flyout.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void KeyUpRaisedOnTargetOpensContextFlyout()
	{
		using (CreateServicesWithFocus())
		{
			var flyout = new Flyout();
			var target = new Panel
			{
				ContextFlyout = flyout
			};
			var contextRequestedCount = 0;
			target.AddHandler(InputElement.ContextRequestedEvent, (s, a) => contextRequestedCount++, RoutingStrategies.Tunnel);

			var window = PreparedWindow(target);
			window.Show();

			target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

			CornerstoneTest.IsTrue(flyout.IsOpen);
			CornerstoneTest.AreEqual(1, contextRequestedCount);
		}
	}

	[PresentationTestMethod]
	public void LightDismissClosesFlyout()
	{
		using (CreateServicesWithFocus())
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

			window.Show();

			var f = new Flyout();
			f.Content = new Border { Width = 10, Height = 10 };
			f.ShowAt(window);

			var e = CreatePointerPressedEventArgs(window, new Point(90, 90));
			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);
			overlay.RaiseEvent(e);

			CornerstoneTest.IsFalse(f.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void LightDismissEventPassThroughToButton()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Width = 100;
			window.Height = 100;

			var buttonClicked = false;
			var button = new Button
			{
				ClickMode = ClickMode.Press
			};
			button.Click += (s, e) => { buttonClicked = true; };
			window.Content = button;

			window.Show();

			var f = new Flyout();
			f.OverlayDismissEventPassThrough = true; // Focus of test
			f.Content = new Border { Width = 10, Height = 10 };
			f.ShowAt(window);

			var hitTester = new StubHitTester();
			window.HitTesterOverride = hitTester;
			hitTester.SetHit(button);

			var e = CreatePointerPressedEventArgs(window, new Point(90, 90));
			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);
			overlay.RaiseEvent(e);

			CornerstoneTest.IsFalse(f.IsOpen);
			CornerstoneTest.IsTrue(buttonClicked); // Button is clicked
		}
	}

	[PresentationTestMethod]
	public void LightDismissNoEventPassThroughToButton()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Width = 100;
			window.Height = 100;

			var buttonClicked = false;
			var button = new Button
			{
				ClickMode = ClickMode.Press
			};
			button.Click += (s, e) => { buttonClicked = true; };
			window.Content = button;

			window.Show();

			var f = new Flyout();
			f.OverlayDismissEventPassThrough = false; // Focus of test
			f.Content = new Border { Width = 10, Height = 10 };
			f.ShowAt(window);

			var hitTester = new StubHitTester();
			window.HitTesterOverride = hitTester;
			hitTester.SetHit(button);

			var e = CreatePointerPressedEventArgs(window, new Point(90, 90));
			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);
			overlay.RaiseEvent(e);

			CornerstoneTest.IsFalse(f.IsOpen);
			CornerstoneTest.IsFalse(buttonClicked); // Button is NOT clicked
		}
	}

	[PresentationTestMethod]
	public void OpeningIsCancellable()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var tracker = 0;
			var f = new Flyout();
			f.Opening += (s, e) =>
			{
				tracker++;
				if (e is CancelEventArgs cancelEventArgs)
				{
					cancelEventArgs.Cancel = true;
				}
			};
			f.ShowAt(window);

			CornerstoneTest.AreEqual(1, tracker);
			CornerstoneTest.IsFalse(f.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void OpeningRaisesSingleOpenedEvent()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var tracker = 0;
			var f = new Flyout();
			f.Opened += (s, e) => { tracker++; };
			f.ShowAt(window);

			CornerstoneTest.AreEqual(1, tracker);
		}
	}

	[PresentationTestMethod]
	public void OpeningRaisesSingleOpeningEvent()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Show();

			var tracker = 0;
			var f = new Flyout();
			f.Opening += (s, e) => { tracker++; };
			f.ShowAt(window);

			CornerstoneTest.AreEqual(1, tracker);
			CornerstoneTest.IsTrue(f.IsOpen);
		}
	}

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void SettingFlyoutPresenterClassesSetsClassesOnFlyoutPresenter()
	{
		using (CreateServicesWithFocus())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='FlyoutPresenter.TestClass'>
            <Setter Property='Background' Value='Red' />
        </Style>
	</Window.Styles>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var flyoutPanel = new Panel();
			var button = new Button
			{
				Content = "Test",
				Flyout = new Flyout
				{
					Content = flyoutPanel
				}
			};
			window.Content = button;
			window.Show();

			((Flyout) button.Flyout).FlyoutPresenterClasses.Add("TestClass");

			button.Flyout.ShowAt(button);

			var presenter = flyoutPanel.GetVisualAncestors().OfType<FlyoutPresenter>().FirstOrDefault();
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual(Colors.Red, (presenter.Background as ISolidColorBrush)?.Color);
		}
	}

	[PresentationTestMethod]
	public void ShouldResetPopupParentOnTargetAttachFollowingDetach()
	{
		using (CreateServicesWithFocus())
		{
			var userControl = new UserControl();
			var window = PreparedWindow(userControl);
			window.Show();

			var flyout = new TestFlyout();
			flyout.ShowAt(userControl);

			var popup = CornerstoneTest.IsType<Popup>(flyout.Popup);
			CornerstoneTest.IsNotNull(popup.Parent);

			flyout.Hide();

			flyout.ShowAt(userControl);
			CornerstoneTest.IsNotNull(popup.Parent);
		}
	}

	[PresentationTestMethod]
	public void ShouldResetPopupParentOnTargetDetached()
	{
		using (CreateServicesWithFocus())
		{
			var userControl = new UserControl();
			var window = PreparedWindow(userControl);
			window.Show();

			var flyout = new TestFlyout();
			flyout.ShowAt(userControl);

			var popup = CornerstoneTest.IsType<Popup>(flyout.Popup);
			CornerstoneTest.IsNotNull(popup.Parent);

			window.Content = null;
			CornerstoneTest.IsNull(popup.Parent);
		}
	}

	[PresentationTestMethod]
	public void ShowModeStandardAttempsFocusFlyoutContent()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();

			var flyoutTextBox = new TextBox();
			var button = new Button
			{
				Flyout = new Flyout
				{
					ShowMode = FlyoutShowMode.Standard,
					Content = new Panel
					{
						Children =
						{
							flyoutTextBox
						}
					}
				}
			};

			window.Content = button;
			window.Show();

			button.Focus();
			CornerstoneTest.Same(button, window.FocusManager!.GetFocusedElement());
			button.Flyout.ShowAt(button);
			CornerstoneTest.IsFalse(button.IsFocused);
			CornerstoneTest.Same(flyoutTextBox, window.FocusManager!.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void ShowModeTransientDoesNotMoveFocusFromTarget()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();

			var flyoutTextBox = new TextBox();
			var button = new Button
			{
				Flyout = new Flyout
				{
					ShowMode = FlyoutShowMode.Transient,
					Content = new Panel
					{
						Children =
						{
							flyoutTextBox
						}
					}
				},
				Content = "Test"
			};

			window.Content = button;
			window.Show();

			button.Focus();
			CornerstoneTest.Same(button, window.FocusManager?.GetFocusedElement());
			button.Flyout.ShowAt(button);
			CornerstoneTest.Same(button, window.FocusManager?.GetFocusedElement());
		}
	}

	private static PointerPressedEventArgs CreatePointerPressedEventArgs(Window source, Point p)
	{
		var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
		return new PointerPressedEventArgs(
			source,
			pointer,
			source,
			p,
			0,
			new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
			KeyModifiers.None);
	}

	private IDisposable CreateServicesWithFocus()
	{
		return UnitTestApplication.Start(TestServices.StyledWindow.With(windowingPlatform:
			new MockWindowingPlatform(null,
				x => UseOverlayPopups ? null : MockWindowingPlatform.CreatePopupMock(x)),
			keyboardDevice: () => new KeyboardDevice()));
	}

	private static Window PreparedWindow(object content = null)
	{
		var platform = PresentationLocator.Current.GetRequiredService<IWindowingPlatform>();
		var windowImpl = platform.CreateWindow();

		var w = new Window(windowImpl) { Content = content };
		w.ApplyTemplate();
		return w;
	}

	#endregion

	#region Classes

	public class TestFlyout : Flyout
	{
		#region Properties

		public new Popup Popup => base.Popup;

		#endregion
	}

	private class FlyoutViewModel : INotifyPropertyChanged
	{
		#region Fields

		private bool _isOpen;

		#endregion

		#region Properties

		public bool IsOpen
		{
			get => _isOpen;
			set
			{
				if (_isOpen != value)
				{
					_isOpen = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
				}
			}
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	#endregion
}

[TestClass]
public class OverlayPopupFlyoutTests : FlyoutTests
{
	#region Constructors

	public OverlayPopupFlyoutTests()
	{
		UseOverlayPopups = true;
	}

	#endregion
}