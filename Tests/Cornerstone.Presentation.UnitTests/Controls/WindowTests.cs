#region References

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class WindowTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public async Task CallingShowDialogOnClosedWindowShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			var windowImpl = CreateImpl();

			parent.Show();

			var target = new Window(windowImpl);
			var task = target.ShowDialog<bool>(parent);

			windowImpl.Closed!();
			await task;

			var openedRaised = false;
			target.Opened += (s, e) => openedRaised = true;

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog<bool>(parent));
			CornerstoneTest.AreEqual("Cannot re-show a closed window.", ex.Message);
			CornerstoneTest.IsFalse(openedRaised);
		}
	}

	[PresentationTestMethod]
	public async Task CallingShowDialogWithClosedParentWindowShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			var target = new Window();

			parent.Close();

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog(parent));
			CornerstoneTest.AreEqual("Cannot show a window with a closed owner.", ex.Message);
		}
	}

	[PresentationTestMethod]
	public async Task CallingShowDialogWithInvisibleParentWindowShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			var target = new Window();

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog(parent));
			CornerstoneTest.AreEqual("Cannot show window with non-visible owner.", ex.Message);
		}
	}

	[PresentationTestMethod]
	public async Task CallingShowDialogWithSelfAsParentWindowShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new Window();

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog(target));
			CornerstoneTest.AreEqual("A Window cannot be its own owner.", ex.Message);
		}
	}

	[PresentationTestMethod]
	public void CallingShowOnClosedWindowShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new Window();

			target.Show();
			target.Close();

			var openedRaised = false;
			target.Opened += (s, e) => openedRaised = true;

			var ex = Assert.Throws<InvalidOperationException>(() => target.Show());
			CornerstoneTest.AreEqual("Cannot re-show a closed window.", ex.Message);
			CornerstoneTest.IsFalse(openedRaised);
		}
	}

	[PresentationTestMethod]
	public void CallingShowWithClosedParentWindowShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			var target = new Window();

			parent.Close();

			var ex = Assert.Throws<InvalidOperationException>(() => target.Show(parent));
			CornerstoneTest.AreEqual("Cannot show a window with a closed owner.", ex.Message);
		}
	}

	[PresentationTestMethod]
	public void CallingShowWithInvisibleParentWindowShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			var target = new Window();

			var ex = Assert.Throws<InvalidOperationException>(() => target.Show(parent));
			CornerstoneTest.AreEqual("Cannot show window with non-visible owner.", ex.Message);
		}
	}

	[PresentationTestMethod]
	public void CallingShowWithSelfAsParentWindowShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new Window();

			var ex = Assert.Throws<InvalidOperationException>(() => target.Show(target));
			CornerstoneTest.AreEqual("A Window cannot be its own owner.", ex.Message);
		}
	}

	[PresentationTestMethod]
	public void CanMaximizeShouldBeFalseIfCanResizeIsFalse()
	{
		var windowImpl = MockWindowingPlatform.CreateWindowMock();

		using var app = UnitTestApplication.Start(TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() => windowImpl)));

		var window = new Window();

		CornerstoneTest.IsTrue(window.CanMaximize);

		window.CanResize = false;

		CornerstoneTest.IsFalse(window.CanMaximize);
	}

	[PresentationTestMethod]
	public void CenterOnScreenUsesWorkingAreaOriginAndScaling()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var workingArea = new PixelRect(1920, 0, 1920, 1080);
			var screen = new MockScreen(2, workingArea, workingArea, true);
			var windowImpl = new StubWindowImpl
			{
				Screens = new StubScreenImpl(screen),
				DesktopScaling = 2
			};
			var window = new Window(windowImpl)
			{
				Width = 400,
				Height = 300
			};

			window.CenterOnScreen();

			CornerstoneTest.AreEqual(new PixelPoint(2480, 240), window.Position);
		}
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ChildwindowsmustnotclosebeforeparenthaschancetoCancelOSCloseButton(bool programmaticClose)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var child = new Window();

			var count = 0;
			var windowClosing = 0;
			var childClosing = 0;
			var windowClosed = 0;
			var childClosed = 0;

			window.Closing += (sender, e) =>
			{
				count++;
				windowClosing = count;
				e.Cancel = true;
			};

			child.Closing += (sender, e) =>
			{
				count++;
				childClosing = count;
			};

			window.Closed += (sender, e) =>
			{
				count++;
				windowClosed = count;
			};

			child.Closed += (sender, e) =>
			{
				count++;
				childClosed = count;
			};

			window.Show();
			child.Show(window);

			if (programmaticClose)
			{
				window.Close();
			}
			else
			{
				var cancel = window.PlatformImpl!.Closing!(WindowCloseReason.WindowClosing);

				CornerstoneTest.AreEqual(true, cancel);
			}

			CornerstoneTest.AreEqual(2, windowClosing);
			CornerstoneTest.AreEqual(1, childClosing);
			CornerstoneTest.AreEqual(0, windowClosed);
			CornerstoneTest.AreEqual(0, childClosed);
		}
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void Childwindowsshouldbeclosedbeforeparent(bool programmaticClose)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var child = new Window();

			var count = 0;
			var windowClosing = 0;
			var childClosing = 0;
			var windowClosed = 0;
			var childClosed = 0;

			window.Closing += (sender, e) =>
			{
				CornerstoneTest.AreEqual(WindowCloseReason.WindowClosing, e.CloseReason);
				CornerstoneTest.AreEqual(programmaticClose, e.IsProgrammatic);
				count++;
				windowClosing = count;
			};

			child.Closing += (sender, e) =>
			{
				CornerstoneTest.AreEqual(WindowCloseReason.OwnerWindowClosing, e.CloseReason);
				CornerstoneTest.AreEqual(programmaticClose, e.IsProgrammatic);
				count++;
				childClosing = count;
			};

			window.Closed += (sender, e) =>
			{
				count++;
				windowClosed = count;
			};

			child.Closed += (sender, e) =>
			{
				count++;
				childClosed = count;
			};

			window.Show();
			child.Show(window);

			if (programmaticClose)
			{
				window.Close();
			}
			else
			{
				var cancel = window.PlatformImpl!.Closing!(WindowCloseReason.WindowClosing);

				CornerstoneTest.AreEqual(false, cancel);
			}

			CornerstoneTest.AreEqual(2, windowClosing);
			CornerstoneTest.AreEqual(1, childClosing);
			CornerstoneTest.AreEqual(4, windowClosed);
			CornerstoneTest.AreEqual(3, childClosed);
		}
	}

	[PresentationTestMethod]
	public void ClosingShouldOnlyBeInvokedOnce()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var count = 0;

			window.Closing +=
				(sender, e) => { count++; };

			window.Show();
			window.Close();

			CornerstoneTest.AreEqual(1, count);
		}
	}

	[PresentationTestMethod]
	public void ExtendingClientAreaToDecorationsWhenAttachedToVisualTreeWorks()
	{
		var extended = false;

		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		windowImpl.NeedsManagedDecorationsGetter = () => extended;
		windowImpl.RequestedDrawnDecorations = PlatformRequestedDrawnDecoration.TitleBar;

		using var app = UnitTestApplication.Start(TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() => windowImpl)));

		var border = new Border();

		var window = new Window
		{
			Content = border
		};

		border.AttachedToVisualTree +=
			(_, _) =>
			{
				extended = true;
				windowImpl.ExtendClientAreaToDecorationsChanged?.Invoke(true);
			};

		window.Show();
	}

	[PresentationTestMethod]
	public void FlowDirectionRTLShouldNotResultInMirroredHost()
	{
		var windowImpl = MockWindowingPlatform.CreateWindowMock();

		using var app = UnitTestApplication.Start(TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() => windowImpl)));

		var window = new Window
		{
			FlowDirection = FlowDirection.RightToLeft
		};

		var visualRoot = window.GetVisualRoot();
		CornerstoneTest.IsType<TopLevelHost>(visualRoot);

		CornerstoneTest.IsFalse(window.HasMirrorTransform);
		CornerstoneTest.IsFalse(visualRoot.HasMirrorTransform);
	}

	[PresentationTestMethod]
	public void HidingParentWindowShouldCloseChildren()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var parent = new Window();
			var child = new Window();

			parent.Show();
			child.Show(parent);

			parent.Hide();

			CornerstoneTest.IsFalse(parent.IsVisible);
			CornerstoneTest.IsFalse(child.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void HidingParentWindowShouldCloseDialogChildren()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var parent = new Window();
			var child = new Window();

			parent.Show();
			child.ShowDialog(parent);

			parent.Hide();

			CornerstoneTest.IsFalse(parent.IsVisible);
			CornerstoneTest.IsFalse(child.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void HidingShouldStopRenderer()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new Window(CreateImpl());

			target.Show();
			target.Hide();
			CornerstoneTest.IsFalse(MediaContext.Instance.IsTopLevelActive(target));
		}
	}

	[PresentationTestMethod]
	public void IsOnScreenTreatsMissingScreensAsOnScreen()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var windowImpl = new StubWindowImpl();
			windowImpl.Screens = new StubScreenImpl();
			var window = new Window(windowImpl)
			{
				Width = 800,
				Height = 600,
				Position = new PixelPoint(100, 80)
			};

			CornerstoneTest.IsTrue(window.IsOnScreen());
		}
	}

	[PresentationTestMethod]
	public void IsOnScreenUsesDesktopScalingForPixelSize()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var workingArea = new PixelRect(0, 0, 1920, 1080);
			var screen = new MockScreen(2, workingArea, workingArea, true);
			var windowImpl = new StubWindowImpl
			{
				Screens = new StubScreenImpl(screen),
				DesktopScaling = 2
			};
			var window = new Window(windowImpl)
			{
				Width = 400,
				Height = 300,
				Position = new PixelPoint(1600, 0)
			};

			CornerstoneTest.IsTrue(window.IsOnScreen());
		}
	}

	[PresentationTestMethod]
	public void IsVisibleSetterShouldAffectMeasurementsInsideWindowDrawnDecorationsContent()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		windowImpl.NeedsManagedDecorations = true;
		windowImpl.RequestedDrawnDecorations = PlatformRequestedDrawnDecoration.TitleBar;

		var window = new Window(windowImpl);

		var stackPanel = new StackPanel
		{
			Width = 32,
			Spacing = 2,
			Children =
			{
				new Control { Height = 32 },
				new Control
				{
					Height = 32,
					Classes = { "hidden-by-style" }
				}
			}
		};

		var contentControl = new ContentControl
		{
			Content = new Control
			{
				Height = 32,
				Width = 32,
				Classes = { "hidden-by-style" }
			}
		};

		var content = new WindowDrawnDecorationsContent
		{
			Overlay = new ContentControl
			{
				Content = new Panel
				{
					Children = { stackPanel, contentControl }
				}
			}
		};

		var template = new WindowDrawnDecorationsTemplate
		{
			Content = (IServiceProvider _) => new TemplateResult<WindowDrawnDecorationsContent>(content, new NameScope())
		};

		var theme = new ControlTheme(typeof(WindowDrawnDecorations))
		{
			Setters =
			{
				new Setter(WindowDrawnDecorations.TemplateProperty, template)
			}
		};

		var style = new Style(x => x.Is<WindowDrawnDecorations>().Template().OfType<Control>().Class("hidden-by-style"))
		{
			Setters =
			{
				new Setter(Visual.IsVisibleProperty, false)
			}
		};

		window.WindowDecorationsTheme = theme;
		window.Styles.Add(style);
		window.Show();
		window.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(new Size(), contentControl.DesiredSize);
		CornerstoneTest.AreEqual(new Size(32, 32), stackPanel.DesiredSize);
	}

	[PresentationTestMethod]
	public void IsVisibleShouldBeFalseAfterClose()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();

			window.Show();
			window.Close();

			CornerstoneTest.IsFalse(window.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldBeFalseAfterHide()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();

			window.Show();
			window.Hide();

			CornerstoneTest.IsFalse(window.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldBeFalseAfterImplSignalsClose()
	{
		var windowImpl = CreateImpl();

		var services = TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() => windowImpl));

		using (UnitTestApplication.Start(services))
		{
			var window = new Window();

			window.Show();
			CornerstoneTest.IsTrue(window.IsVisible);

			windowImpl.Closed!();

			CornerstoneTest.IsFalse(window.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldBeTrueAfterShow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();

			window.Show();

			CornerstoneTest.IsTrue(window.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldBeTrueAfterShowDialog()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			parent.Show();
			var window = new Window();

			var task = window.ShowDialog(parent);

			CornerstoneTest.IsTrue(window.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldInitiallyBeFalse()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var window = new Window();

			CornerstoneTest.IsFalse(window.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void SettingTitleShouldSetImplTitle()
	{
		var windowImpl = new StubWindowImpl();
		var windowingPlatform = new MockWindowingPlatform(() => windowImpl);

		using (UnitTestApplication.Start(new TestServices(windowingPlatform: windowingPlatform)))
		{
			var target = new Window();

			target.Title = "Hello World";

			windowImpl.Calls.VerifyLastPrefix("SetTitle", "Hello World");
		}
	}

	[PresentationTestMethod]
	public void ShowDialogShouldRaiseOpened()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			var target = new Window();
			var raised = false;

			parent.Show();
			target.Opened += (s, e) => raised = true;

			target.ShowDialog<object>(parent);

			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void ShowDialogShouldStartRenderer()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			var target = new Window(CreateImpl());

			parent.Show();
			target.ShowDialog<object>(parent);

			CornerstoneTest.IsTrue(MediaContext.Instance.IsTopLevelActive(target));
		}
	}

	[PresentationTestMethod]
	public async Task ShowDialogWithValueTypeReturnsDefaultWhenClosed()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parent = new Window();
			var windowImpl = CreateImpl();

			parent.Show();
			var target = new Window(windowImpl);
			var task = target.ShowDialog<bool>(parent);

			windowImpl.Closed!();

			var result = await task;
			CornerstoneTest.IsFalse(result);
		}
	}

	[PresentationTestMethod]
	public void ShowShouldApplyDefaultIconWhenNoCustomIconIsSet()
	{
		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		var windowingPlatform = new MockWindowingPlatform(() => windowImpl);

		using (UnitTestApplication.Start(TestServices.StyledWindow.With(windowingPlatform: windowingPlatform)))
		{
			var target = new Window();

			// Clear any SetIcon calls from construction.
			windowImpl.Calls.Clear();

			target.Show();

			// ShowCore should apply the default icon when no custom icon was set.
			windowImpl.Calls.VerifyCalledAtLeastOnce("SetIcon");
		}
	}

	[PresentationTestMethod]
	public void ShowingShouldStartRenderer()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new Window(CreateImpl());

			target.Show();
			CornerstoneTest.IsTrue(MediaContext.Instance.IsTopLevelActive(target));
		}
	}

	[PresentationTestMethod]
	public void WindowDecorationsThemeShouldApplyToDecorations()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		windowImpl.NeedsManagedDecorations = true;
		windowImpl.RequestedDrawnDecorations =
			PlatformRequestedDrawnDecoration.TitleBar | PlatformRequestedDrawnDecoration.Border;

		var window = new Window(windowImpl);

		var (theme1, content1) = CreateTheme();
		window.WindowDecorationsTheme = theme1;
		window.Show();

		var decorations = window.TopLevelHost.Decorations;
		CornerstoneTest.IsNotNull(decorations);
		CornerstoneTest.Same(theme1, decorations.Theme);
		CornerstoneTest.Same(content1, decorations.Content);

		var (theme2, content2) = CreateTheme();
		window.WindowDecorationsTheme = theme2;

		CornerstoneTest.Same(theme2, decorations.Theme);
		CornerstoneTest.Same(content2, decorations.Content);

		static (ControlTheme theme, WindowDrawnDecorationsContent content) CreateTheme()
		{
			var content = new WindowDrawnDecorationsContent();

			var template = new WindowDrawnDecorationsTemplate
			{
				Content = (IServiceProvider _) => new TemplateResult<WindowDrawnDecorationsContent>(content, new NameScope())
			};

			var theme = new ControlTheme(typeof(WindowDrawnDecorations))
			{
				Setters =
				{
					new Setter(WindowDrawnDecorations.TemplateProperty, template)
				}
			};

			return (theme, content);
		}
	}

	[PresentationTestMethod]
	public void WindowShouldBeCenteredRelativeToOwnerWhenWindowStartupLocationIsCenterOwner()
	{
		var parentWindowImpl = MockWindowingPlatform.CreateWindowMock();
		parentWindowImpl.ClientSize = new Size(800, 480);
		parentWindowImpl.MaxAutoSizeHint = new Size(1920, 1080);

		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		windowImpl.ClientSize = new Size(320, 200);
		windowImpl.MaxAutoSizeHint = new Size(1920, 1080);

		var parentWindowServices = TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() => parentWindowImpl));

		var windowServices = TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() => windowImpl));

		using (UnitTestApplication.Start(parentWindowServices))
		{
			var parentWindow = new Window();
			parentWindow.Position = new PixelPoint(60, 40);

			parentWindow.Show();

			using (UnitTestApplication.Start(windowServices))
			{
				var window = new Window();
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				window.Position = new PixelPoint(60, 40);

				window.ShowDialog(parentWindow);

				var expectedPosition = new PixelPoint(
					(int) ((parentWindow.Position.X + (parentWindow.ClientSize.Width / 2)) - (window.ClientSize.Width / 2)),
					(int) ((parentWindow.Position.Y + (parentWindow.ClientSize.Height / 2)) - (window.ClientSize.Height / 2)));

				CornerstoneTest.AreEqual(window.Position, expectedPosition);
			}
		}
	}

	[PresentationTestMethod]
	public void WindowShouldBeCenteredWhenWindowStartupLocationIsCenterScreen()
	{
		var screen1 = new MockScreen(1.0, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 1040)), true);
		var screen2 = new MockScreen(1.0, new PixelRect(new PixelSize(1366, 768)), new PixelRect(new PixelSize(1366, 728)), false);

		var screens = new StubScreenImpl(screen1, screen2);
		screens.SetScreenFromPoint(screen1);

		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		windowImpl.ClientSize = new Size(800, 480);
		windowImpl.Screens = screens;

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window(windowImpl);
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			window.Position = new PixelPoint(60, 40);

			window.Show();

			var expectedPosition = new PixelPoint(
				(int) ((screen1.WorkingArea.Size.Width / 2) - (window.ClientSize.Width / 2)),
				(int) ((screen1.WorkingArea.Size.Height / 2) - (window.ClientSize.Height / 2)));

			CornerstoneTest.AreEqual(window.Position, expectedPosition);
		}
	}

	[PresentationTestMethod]
	public void WindowShouldBeSizedToMinSizeIfInitialSizeLessThanMinSize()
	{
		var screen1 = new MockScreen(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
		var screens = new StubScreenImpl(screen1);
		screens.SetScreenFromPoint(screen1);

		var windowImpl = MockWindowingPlatform.CreateWindowMock(400, 300);
		windowImpl.DesktopScaling = 1.75;
		windowImpl.RenderScaling = 1.75;
		windowImpl.Screens = screens;

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window(windowImpl);
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			window.MinWidth = 720;
			window.MinHeight = 480;

			window.Show();

			CornerstoneTest.AreEqual(new PixelPoint(330, 63), window.Position);
			CornerstoneTest.AreEqual(new Size(720, 480), window.Bounds.Size);
		}
	}

	[PresentationTestMethod]
	public void WindowShouldNotBeCenteredWhenWindowStartupLocationIsCenterScreenAndWindowIsHiddenAndShown()
	{
		var screen1 = new MockScreen(1.0, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 1040)), true);

		var screens = new StubScreenImpl(screen1);
		screens.SetScreenFromPoint(screen1);

		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		windowImpl.ClientSize = new Size(800, 480);
		windowImpl.Screens = screens;

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window(windowImpl)
			{
				WindowStartupLocation = WindowStartupLocation.CenterScreen
			};

			window.Show();

			var expected = new PixelPoint(150, 400);
			window.Position = expected;

			window.IsVisible = false;
			window.IsVisible = true;

			CornerstoneTest.AreEqual(expected, window.Position);
		}
	}

	[PresentationTestMethod]
	public void WindowStateNonUsableGetterSetterUpdatesImmediately()
	{
		var windowImpl = MockWindowingPlatform.CreateWindowMock();

		// Legacy behavior: WindowStateGetterIsUsable = false
		windowImpl.WindowStateGetterIsUsable = false;

		var windowingPlatform = new MockWindowingPlatform(() => windowImpl);
		using (UnitTestApplication.Start(new TestServices(windowingPlatform: windowingPlatform)))
		{
			var target = new Window();
			target.Show();

			var raised = new List<WindowState>();
			target.GetObservable(Window.WindowStateProperty).Skip(1).Subscribe(s => raised.Add(s));

			// Set to Maximized - should update immediately regardless of platform behavior
			target.WindowState = WindowState.Maximized;

			CornerstoneTest.AreEqual(WindowState.Maximized, target.WindowState);
			CornerstoneTest.Contains(raised, WindowState.Maximized);

			// Verify the setter was forwarded to the platform impl without reading the getter
			windowImpl.Calls.VerifyCalled("set_WindowState");
			CornerstoneTest.AreEqual(0, windowImpl.WindowStateGetCount);
		}
	}

	[PresentationTestMethod]
	public void WindowStateUsableGetterSetterRaisesSyntheticNotificationWhenPlatformRefuses()
	{
		var windowImpl = MockWindowingPlatform.CreateWindowMock();

		// Simulate a platform where the getter is usable but refuses state change requests.
		// Start in Maximized state, then refuse a request to go Normal.
		var platformState = WindowState.Normal;
		windowImpl.WindowStateGetterIsUsable = true;
		windowImpl.WindowStateGetter = () => platformState;
		windowImpl.WindowStateSetHandler = v =>
		{
			if (v == WindowState.Maximized)
			{
				platformState = v;
				windowImpl.WindowStateChanged?.Invoke(v);
			}
		};

		var windowingPlatform = new MockWindowingPlatform(() => windowImpl);
		using (UnitTestApplication.Start(new TestServices(windowingPlatform: windowingPlatform)))
		{
			var target = new Window();
			target.Show();

			// First, go to Maximized (accepted by platform)
			target.WindowState = WindowState.Maximized;
			CornerstoneTest.AreEqual(WindowState.Maximized, target.WindowState);

			var raised = new List<PresentationPropertyChangedEventArgs>();
			target.PropertyChanged += (_, e) =>
			{
				if (e.Property == Window.WindowStateProperty)
				{
					raised.Add(e);
				}
			};

			// Now try to go to FullScreen - platform refuses, stays Maximized
			target.WindowState = WindowState.FullScreen;

			// The getter should still return Maximized because the platform refused
			CornerstoneTest.AreEqual(WindowState.Maximized, target.WindowState);

			// A synthetic notification should have been raised so data bindings can recover
			CornerstoneTest.NotEmpty(raised);
			CornerstoneTest.AreEqual(WindowState.Maximized, raised[^1].GetNewValue<WindowState>());
		}
	}

	[PresentationTestMethod]
	public void WindowStateUsableGetterSetterUpdatesOnlyAfterPlatformCallback()
	{
		var windowImpl = MockWindowingPlatform.CreateWindowMock();

		// Simulate a platform where the getter is usable and the setter is accepted
		var platformState = WindowState.Normal;
		windowImpl.WindowStateGetterIsUsable = true;
		windowImpl.WindowStateGetter = () => platformState;
		windowImpl.WindowStateSetHandler = v =>
		{
			platformState = v;
			windowImpl.WindowStateChanged?.Invoke(v);
		};

		var windowingPlatform = new MockWindowingPlatform(() => windowImpl);
		using (UnitTestApplication.Start(new TestServices(windowingPlatform: windowingPlatform)))
		{
			var target = new Window();
			target.Show();

			var raised = new List<WindowState>();
			target.GetObservable(Window.WindowStateProperty).Skip(1).Subscribe(s => raised.Add(s));

			// Set to Maximized - platform accepts and fires callback
			target.WindowState = WindowState.Maximized;
			CornerstoneTest.AreEqual(WindowState.Maximized, target.WindowState);
			CornerstoneTest.Contains(raised, WindowState.Maximized);

			// Set to FullScreen - platform accepts and fires callback
			raised.Clear();
			target.WindowState = WindowState.FullScreen;
			CornerstoneTest.AreEqual(WindowState.FullScreen, target.WindowState);
			CornerstoneTest.Contains(raised, WindowState.FullScreen);
		}
	}

	[PresentationTestMethod]
	public void WindowTopmostByDefaultShouldConfigurePlatformImplWhenConstructed()
	{
		var windowImpl = MockWindowingPlatform.CreateWindowMock();

		var windowServices = TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() => windowImpl));

		using (UnitTestApplication.Start(windowServices))
		{
			var window = new TopmostWindow();

			CornerstoneTest.IsTrue(window.Topmost);
			windowImpl.Calls.VerifyLastPrefix("SetTopmost", true);
		}
	}

	private static StubWindowImpl CreateImpl()
	{
		var screen1 = new MockScreen(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
		var screens = new StubScreenImpl(screen1);
		var windowImpl = new StubWindowImpl();
		windowImpl.Screens = screens;

		return windowImpl;
	}

	#endregion

	#region Classes

	public class DialogSizingTests : SizingTests
	{
		#region Methods

		protected override void Show(Window window)
		{
			var owner = new Window();
			owner.Show();
			window.ShowDialog(owner);
		}

		#endregion
	}

	[TestClass]
	public class ForcedDecorationSizingTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ChildShouldBeMeasuredWithContentSize()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = CreateForcedCsdWindowMock();
				var child = new ChildControl();
				var target = new Window(windowImpl)
				{
					Width = 400,
					Height = 300,
					SizeToContent = SizeToContent.Manual,
					Content = child
				};

				target.Show();

				CornerstoneTest.AreEqual(1, child.MeasureSizes.Count);
				CornerstoneTest.AreEqual(new Size(400, 300), child.MeasureSizes[0]);
			}
		}

		[PresentationTestMethod]
		public void ClientSizeShouldExcludeDecorationInset()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = CreateForcedCsdWindowMock();
				var target = new Window(windowImpl)
				{
					SizeToContent = SizeToContent.Manual
				};

				// Verify mock setup
				CornerstoneTest.IsTrue(windowImpl.NeedsManagedDecorations);

				target.Show();

				var host = target.TopLevelHost;
				var decorations = host.Decorations;

				// Debug: verify decorations were created
				CornerstoneTest.IsNotNull(decorations);
				CornerstoneTest.IsTrue(decorations!.TitleBarHeight > 0, $"TitleBarHeight was {decorations.TitleBarHeight}");

				var inset = host.DecorationInset;
				CornerstoneTest.AreNotEqual(default, inset);

				var expectedClientSize = new Size(
					800 - inset.Left - inset.Right,
					600 - inset.Top - inset.Bottom);
				CornerstoneTest.AreEqual(expectedClientSize, target.ClientSize);
			}
		}

		[PresentationTestMethod]
		public void HandleResizedShouldSubtractInsetFromPlatformSize()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = CreateForcedCsdWindowMock();
				var target = new Window(windowImpl)
				{
					SizeToContent = SizeToContent.Manual
				};

				target.Show();

				var inset = target.TopLevelHost.DecorationInset;

				// Simulate a platform resize (e.g. user resize)
				target.PlatformImpl!.Resized!.Invoke(new Size(1000, 700), WindowResizeReason.User);

				var expectedClientSize = new Size(
					1000 - inset.Left - inset.Right,
					700 - inset.Top - inset.Bottom);
				CornerstoneTest.AreEqual(expectedClientSize, target.ClientSize);
			}
		}

		[PresentationTestMethod]
		public void SettingWidthShouldResizeWindowImplWithInsetAdded()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = CreateForcedCsdWindowMock();
				var target = new Window(windowImpl)
				{
					Width = 400,
					Height = 300,
					SizeToContent = SizeToContent.Manual
				};

				target.Show();

				var inset = target.TopLevelHost.DecorationInset;

				target.Width = 500;
				target.LayoutManager.ExecuteLayoutPass();

				// Platform should receive full frame size (content + inset)
				var expectedPlatformSize = new Size(
					500 + inset.Left + inset.Right,
					300 + inset.Top + inset.Bottom);
				windowImpl.Calls.VerifyLastPrefix("Resize", expectedPlatformSize, WindowResizeReason.Layout);
			}
		}

		[PresentationTestMethod]
		public void SizeToContentShouldWorkInForcedMode()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = CreateForcedCsdWindowMock();
				var child = new Canvas
				{
					Width = 400,
					Height = 300
				};

				var target = new Window(windowImpl)
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = child
				};

				target.Show();

				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(300, target.Height);
				CornerstoneTest.AreEqual(SizeToContent.WidthAndHeight, target.SizeToContent);
			}
		}

		[PresentationTestMethod]
		public void UserResizeShouldResetSizeToContent()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = CreateForcedCsdWindowMock();
				var child = new Canvas
				{
					Width = 400,
					Height = 300
				};

				var target = new Window(windowImpl)
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = child
				};

				target.Show();
				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(300, target.Height);

				var inset = target.TopLevelHost.DecorationInset;

				// Platform fires resize with full frame size
				var newPlatformWidth = 500 + inset.Left + inset.Right;
				var newPlatformHeight = 300 + inset.Top + inset.Bottom;
				windowImpl.Resized?.Invoke(
					new Size(newPlatformWidth, newPlatformHeight),
					WindowResizeReason.User);

				CornerstoneTest.AreEqual(500, target.Width);
				CornerstoneTest.AreEqual(300, target.Height);
				CornerstoneTest.AreEqual(SizeToContent.Height, target.SizeToContent);
			}
		}

		[PresentationTestMethod]
		public void WidthHeightShouldNotBeNaNAfterShow()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = CreateForcedCsdWindowMock();
				var target = new Window(windowImpl)
				{
					SizeToContent = SizeToContent.Manual
				};

				target.Show();

				CornerstoneTest.IsFalse(double.IsNaN(target.Width));
				CornerstoneTest.IsFalse(double.IsNaN(target.Height));

				var inset = target.TopLevelHost.DecorationInset;
				CornerstoneTest.AreEqual(800 - inset.Left - inset.Right, target.Width);
				CornerstoneTest.AreEqual(600 - inset.Top - inset.Bottom, target.Height);
			}
		}

		[PresentationTestMethod]
		public void WindowDecorationMarginShouldBeZeroInForcedMode()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = CreateForcedCsdWindowMock();
				var target = new Window(windowImpl)
				{
					SizeToContent = SizeToContent.Manual
				};

				target.Show();

				CornerstoneTest.AreEqual(default, target.WindowDecorationMargin);
			}
		}

		/// <summary>
		/// Creates a mock IWindowImpl that simulates forced CSD mode:
		/// NeedsManagedDecorations = true, RequestedDrawnDecorations includes TitleBar + Border,
		/// but IsClientAreaExtendedToDecorations = false.
		/// </summary>
		private static StubWindowImpl CreateForcedCsdWindowMock(
			double initialWidth = 800, double initialHeight = 600)
		{
			var windowImpl = MockWindowingPlatform.CreateWindowMock(initialWidth, initialHeight);

			windowImpl.NeedsManagedDecorations = true;
			windowImpl.RequestedDrawnDecorations =
				PlatformRequestedDrawnDecoration.TitleBar | PlatformRequestedDrawnDecoration.Border;

			return windowImpl;
		}

		#endregion
	}

	[TestClass]
	public class SizingTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ChildShouldBeMeasuredWithClientSizeIfSizeToContentIsManualAndNoWidthHeightSpecified()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = MockWindowingPlatform.CreateWindowMock();
				windowImpl.ClientSize = new Size(550, 450);

				var child = new ChildControl();
				var target = new Window(windowImpl)
				{
					SizeToContent = SizeToContent.Manual,
					Content = child
				};

				Show(target);

				CornerstoneTest.AreEqual(1, child.MeasureSizes.Count);
				CornerstoneTest.AreEqual(new Size(550, 450), child.MeasureSizes[0]);
			}
		}

		[PresentationTestMethod]
		public void ChildShouldBeMeasuredWithMaxAutoSizeHintIfSizeToContentIsWidthAndHeight()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = MockWindowingPlatform.CreateWindowMock();
				windowImpl.MaxAutoSizeHint = new Size(1200, 1000);

				var child = new ChildControl();
				var target = new Window(windowImpl)
				{
					Width = 100,
					Height = 50,
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = child
				};

				target.Show();

				CornerstoneTest.AreEqual(1, child.MeasureSizes.Count);
				CornerstoneTest.AreEqual(new Size(1200, 1000), child.MeasureSizes[0]);
			}
		}

		[PresentationTestMethod]
		public void ChildShouldBeMeasuredWithWidthAndHeightIfSizeToContentIsManual()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var child = new ChildControl();
				var target = new Window
				{
					Width = 100,
					Height = 50,
					SizeToContent = SizeToContent.Manual,
					Content = child
				};

				// Verify that the child is initially measured with our Width/Height.
				Show(target);

				CornerstoneTest.AreEqual(1, child.MeasureSizes.Count);
				CornerstoneTest.AreEqual(new Size(100, 50), child.MeasureSizes[0]);

				// Now change the bounds: verify that we are using the new Width/Height, and not the old ClientSize.
				child.MeasureSizes.Clear();
				child.InvalidateMeasure();

				target.Width = 120;
				target.Height = 70;

				Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

				CornerstoneTest.AreEqual(1, child.MeasureSizes.Count);
				CornerstoneTest.AreEqual(new Size(120, 70), child.MeasureSizes[0]);
			}
		}

		[PresentationTestMethod]
		public void HidingDialogWindowShouldCompleteTask()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var parent = new Window();
				parent.Show();

				var target = new Window();

				var task = target.ShowDialog<bool>(parent);

				target.IsVisible = false;

				CornerstoneTest.IsTrue(task.IsCompletedSuccessfully);
			}
		}

		[PresentationTestMethod]
		public void IsVisibleShouldOpenWindow()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var target = new Window();
				var raised = false;

				target.Opened += (s, e) => raised = true;
				target.IsVisible = true;

				CornerstoneTest.IsTrue(raised);
			}
		}

		[PresentationTestMethod]
		public void MaxWidthAndMaxHeightShouldBeRespectedWithSizeToContentWidthAndHeight()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var child = new ChildControl();

				var target = new Window
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					MaxWidth = 300,
					MaxHeight = 700,
					Content = child
				};

				Show(target);

				CornerstoneTest.AreEqual(new[] { new Size(300, 700) }, child.MeasureSizes);
			}
		}

		[PresentationTestMethod]
		public void SettingWidthShouldResizeWindowImpl()
		{
			// Issue #3796
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var target = new Window
				{
					Width = 400,
					Height = 800
				};

				Show(target);

				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);

				target.Width = 410;
				target.LayoutManager.ExecuteLayoutPass();

				var windowImpl = (StubWindowImpl) target.PlatformImpl!;
				CornerstoneTest.IsTrue(windowImpl.Calls.WasCalled("Resize", new Size(410, 800),
					WindowResizeReason.Application));
				CornerstoneTest.AreEqual(410, target.Width);
			}
		}

		[PresentationTestMethod]
		public void ShouldNotHaveOffsetOnBoundsWhenContentLargerThanMaxWindowSize()
		{
			// Issue #3784.
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var windowImpl = MockWindowingPlatform.CreateWindowMock();
				var clientSize = new Size(200, 200);
				var maxClientSize = new Size(480, 480);

				windowImpl.ResizeHandler = (size, reason) =>
				{
					clientSize = size.Constrain(maxClientSize);
					windowImpl.Resized?.Invoke(clientSize, reason);
				};
				windowImpl.ClientSizeGetter = () => clientSize;

				var child = new Canvas
				{
					Width = 400,
					Height = 800
				};
				var target = new Window(windowImpl)
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = child
				};

				Show(target);

				CornerstoneTest.AreEqual(new Size(400, 480), target.Bounds.Size);

				// Issue #3784 causes this to be (0, 160) which makes no sense as Window has no
				// parent control to be offset against.
				CornerstoneTest.AreEqual(new Point(0, 0), target.Bounds.Position);
			}
		}

		[PresentationTestMethod]
		public void ShowWorksWhenMinDimensionGreaterThanMax()
		{
			using var app = UnitTestApplication.Start(TestServices.StyledWindow);

			var target = new Window
			{
				MinWidth = 100,
				MaxWidth = 80,
				MinHeight = 200,
				MaxHeight = 180
			};

			Show(target);

			CornerstoneTest.AreEqual(100, target.Width);
			CornerstoneTest.AreEqual(200, target.Height);
		}

		[PresentationTestMethod]
		public void SizeToContentShouldNotBeLostOnScalingChange()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var child = new Canvas
				{
					Width = 209,
					Height = 117
				};

				var target = new Window
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = child
				};

				Show(target);

				// Size before and after DPI change is a real-world example, with size after DPI
				// change coming from Win32 WM_DPICHANGED.
				target.PlatformImpl!.ScalingChanged!(1.5);
				target.PlatformImpl!.Resized!(
					new Size(210.66666666666666, 118.66666666666667),
					WindowResizeReason.DpiChange);

				CornerstoneTest.AreEqual(SizeToContent.WidthAndHeight, target.SizeToContent);
			}
		}

		[PresentationTestMethod]
		public void SizeToContentShouldNotBeLostOnShow()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var child = new Canvas
				{
					Width = 400,
					Height = 800
				};

				var target = new Window
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = child
				};

				Show(target);

				CornerstoneTest.AreEqual(SizeToContent.WidthAndHeight, target.SizeToContent);
			}
		}

		[PresentationTestMethod]
		public void UserResizeOfWindowHeightShouldResetSizeToContent()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var target = new Window
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = new Canvas
					{
						Width = 400,
						Height = 800
					}
				};

				Show(target);
				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);

				target.PlatformImpl!.Resized!(new Size(400, 810), WindowResizeReason.User);

				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(810, target.Height);
				CornerstoneTest.AreEqual(SizeToContent.Width, target.SizeToContent);
			}
		}

		[PresentationTestMethod]
		public void UserResizeOfWindowWidthShouldResetSizeToContent()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var target = new Window
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = new Canvas
					{
						Width = 400,
						Height = 800
					}
				};

				Show(target);
				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);

				target.PlatformImpl!.Resized!(new Size(410, 800), WindowResizeReason.User);

				CornerstoneTest.AreEqual(410, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);
				CornerstoneTest.AreEqual(SizeToContent.Height, target.SizeToContent);
			}
		}

		[PresentationTestMethod]
		public void WidthHeightShouldBeUpdatedWhenSizeToContentIsWidthAndHeight()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var child = new Canvas
				{
					Width = 400,
					Height = 800
				};

				var target = new Window
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = child
				};

				Show(target);

				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);

				child.Width = 410;
				target.LayoutManager.ExecuteLayoutPass();

				CornerstoneTest.AreEqual(410, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);
				CornerstoneTest.AreEqual(SizeToContent.WidthAndHeight, target.SizeToContent);
			}
		}

		[PresentationTestMethod]
		public void WidthHeightShouldNotBeNaNAfterShowWithSizeToContentManual()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var child = new Canvas
				{
					Width = 400,
					Height = 800
				};

				var target = new Window
				{
					SizeToContent = SizeToContent.Manual,
					Content = child
				};

				Show(target);

				// Values come from MockWindowingPlatform defaults.
				CornerstoneTest.AreEqual(800, target.Width);
				CornerstoneTest.AreEqual(600, target.Height);
			}
		}

		[PresentationTestMethod]
		public void WidthHeightShouldNotBeNaNAfterShowWithSizeToContentWidthAndHeight()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var child = new Canvas
				{
					Width = 400,
					Height = 800
				};

				var target = new Window
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					Content = child
				};

				target.GetObservable(Window.WidthProperty).Subscribe(x => { });

				Show(target);

				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);
			}
		}

		[PresentationTestMethod]
		public void WindowResizeShouldNotResetSizeToContentIfCanResizeFalse()
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var target = new Window
				{
					SizeToContent = SizeToContent.WidthAndHeight,
					CanResize = false,
					Content = new Canvas
					{
						Width = 400,
						Height = 800
					}
				};

				Show(target);
				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);

				target.PlatformImpl!.Resized!(new Size(410, 810), WindowResizeReason.Unspecified);

				CornerstoneTest.AreEqual(400, target.Width);
				CornerstoneTest.AreEqual(800, target.Height);
				CornerstoneTest.AreEqual(SizeToContent.WidthAndHeight, target.SizeToContent);
			}
		}

		protected virtual void Show(Window window)
		{
			window.Show();
		}

		#endregion
	}

	[TestClass]
	public class TitleBarDecorationsTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void AllDecorationsShouldBeVisibleByDefault()
		{
			using var app = UnitTestApplication.Start(TestServices.StyledWindow);

			var window = CreateWindowWithDrawnDecorations();
			window.Show();

			var decorations = window.TopLevelHost.Decorations;
			CornerstoneTest.IsNotNull(decorations);
			CornerstoneTest.AreEqual(TitleBarDecorations.All, decorations.TitleBarDecorations);
			AssertClassDecorations(decorations, TitleBarDecorations.All);
		}

		[PresentationTestMethod]
		public void DecorationsSetAfterShowShouldApplyCorrectClasses()
		{
			using var app = UnitTestApplication.Start(TestServices.StyledWindow);

			var window = CreateWindowWithDrawnDecorations();
			window.Show();

			var drawnDecorations = window.TopLevelHost.Decorations;
			CornerstoneTest.IsNotNull(drawnDecorations);

			WindowDrawnDecorations.SetTitleBarDecorations(window, TitleBarDecorations.None);
			AssertClassDecorations(drawnDecorations, TitleBarDecorations.None);

			WindowDrawnDecorations.SetTitleBarDecorations(window, TitleBarDecorations.MinimizeButton);
			AssertClassDecorations(drawnDecorations, TitleBarDecorations.MinimizeButton);
		}

		[PresentationTestMethod]
		public void DecorationsSetBeforeShowShouldApplyCorrectClasses()
		{
			using var app = UnitTestApplication.Start(TestServices.StyledWindow);

			const TitleBarDecorations decorations = TitleBarDecorations.Title | TitleBarDecorations.CloseButton;

			var window = CreateWindowWithDrawnDecorations();
			WindowDrawnDecorations.SetTitleBarDecorations(window, decorations);
			window.Show();

			var drawnDecorations = window.TopLevelHost.Decorations;
			CornerstoneTest.IsNotNull(drawnDecorations);
			CornerstoneTest.AreEqual(decorations, drawnDecorations.TitleBarDecorations);
			AssertClassDecorations(drawnDecorations, decorations);
		}

		[PresentationTestMethod]
		public void DecorationsShouldNotShowButtonsUnsupportedByThePlatform()
		{
			using var app = UnitTestApplication.Start(TestServices.StyledWindow);

			var window = CreateWindowWithDrawnDecorations(PlatformAllowedWindowActions.Minimize);
			window.Show();

			var decorations = window.TopLevelHost.Decorations;
			CornerstoneTest.IsNotNull(decorations);

			// Maximize and fullscreen are requested, but not allowed by the platform.
			AssertClassDecorations(
				decorations,
				TitleBarDecorations.Title | TitleBarDecorations.MinimizeButton | TitleBarDecorations.CloseButton);
		}

		private static void AssertClassDecorations(WindowDrawnDecorations drawnDecorations, TitleBarDecorations expected)
		{
			AssertClassDecoration(TitleBarDecorations.Title, ":has-title");
			AssertClassDecoration(TitleBarDecorations.MinimizeButton, ":has-minimize");
			AssertClassDecoration(TitleBarDecorations.MaximizeButton, ":has-maximize");
			AssertClassDecoration(TitleBarDecorations.CloseButton, ":has-close");
			AssertClassDecoration(TitleBarDecorations.FullScreenButton, ":has-fullscreen");

			void AssertClassDecoration(TitleBarDecorations decoration, string className)
			{
				CornerstoneTest.AreEqual(expected.HasFlag(decoration), drawnDecorations.Classes.Contains(className));
			}
		}

		private static Window CreateWindowWithDrawnDecorations(PlatformAllowedWindowActions allowedActions = PlatformAllowedWindowActions.All)
		{
			var windowImpl = MockWindowingPlatform.CreateWindowMock();
			windowImpl.NeedsManagedDecorations = true;
			windowImpl.RequestedDrawnDecorations =
				PlatformRequestedDrawnDecoration.TitleBar | PlatformRequestedDrawnDecoration.Border;
			windowImpl.AllowedWindowActions = allowedActions;

			return new Window(windowImpl);
		}

		#endregion
	}

	private class ChildControl : Control
	{
		#region Properties

		public List<Size> MeasureSizes { get; } = new();

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureSizes.Add(availableSize);
			return base.MeasureOverride(availableSize);
		}

		#endregion
	}

	private class TopmostWindow : Window
	{
		#region Constructors

		static TopmostWindow()
		{
			TopmostProperty.OverrideDefaultValue<TopmostWindow>(true);
		}

		#endregion
	}

	#endregion
}