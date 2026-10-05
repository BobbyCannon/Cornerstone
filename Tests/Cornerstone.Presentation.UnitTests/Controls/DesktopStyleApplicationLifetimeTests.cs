#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class DesktopStyleApplicationLifetimeTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CloseShouldRemoveWindowFromOpenWindows()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			var window = new Window();

			window.Show();
			CornerstoneTest.AreEqual(1, lifetime.Windows.Count);
			window.Close();

			CornerstoneTest.Empty(lifetime.Windows);
		}
	}

	[PresentationTestMethod]
	public void ImplClosingShouldRemoveWindowFromOpenWindows()
	{
		var windowImpl = new StubWindowImpl();

		var screen1 = new MockScreen(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
		var screens = new StubScreenImpl(screen1);
		windowImpl.Screens = screens;

		var services = TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() => windowImpl));

		using (UnitTestApplication.Start(services))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			var window = new Window();

			window.Show();
			CornerstoneTest.AreEqual(1, lifetime.Windows.Count);
			windowImpl.Closed!();

			CornerstoneTest.Empty(lifetime.Windows);
		}
	}

	[PresentationTestMethod]
	public void LastWindowClosedShutdownShouldBeCancellable()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			lifetime.ShutdownMode = ShutdownMode.OnLastWindowClose;
			Setup(lifetime);

			var hasExit = false;

			lifetime.Exit += (_, _) => hasExit = true;

			var windowA = new Window();

			windowA.Show();

			var windowB = new Window();

			windowB.Show();

			var raised = 0;

			lifetime.ShutdownRequested += (_, e) =>
			{
				e.Cancel = true;
				++raised;
			};

			windowA.Close();

			CornerstoneTest.IsFalse(hasExit);

			windowB.Close();

			CornerstoneTest.AreEqual(1, raised);
			CornerstoneTest.IsFalse(hasExit);
		}
	}

	[PresentationTestMethod]
	public void MainWindowClosedShutdownShouldBeCancellable()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
			Setup(lifetime);

			var hasExit = false;

			lifetime.Exit += (_, _) => hasExit = true;

			var mainWindow = new Window();

			mainWindow.Show();

			lifetime.MainWindow = mainWindow;

			var window = new Window();

			window.Show();

			var raised = 0;

			lifetime.ShutdownRequested += (_, e) =>
			{
				e.Cancel = true;
				++raised;
			};

			mainWindow.Close();

			CornerstoneTest.AreEqual(1, raised);
			CornerstoneTest.IsFalse(hasExit);
		}
	}

	[PresentationTestMethod]
	public void OnMainWindowCloseOverridesSecondaryWindowCancellation()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
			Setup(lifetime);

			var hasExit = false;
			var secondaryWindowClosingExecuted = false;
			var secondaryWindowClosedExecuted = false;

			lifetime.Exit += (_, _) => hasExit = true;

			var mainWindow = new Window();
			mainWindow.Show();

			lifetime.MainWindow = mainWindow;

			var window = new Window();
			window.Closing += (_, args) =>
			{
				secondaryWindowClosingExecuted = true;
				args.Cancel = true;
			};
			window.Closed += (_, _) => { secondaryWindowClosedExecuted = true; };
			window.Show();

			mainWindow.Close();

			CornerstoneTest.IsTrue(secondaryWindowClosingExecuted);
			CornerstoneTest.IsTrue(secondaryWindowClosedExecuted);
			CornerstoneTest.IsTrue(hasExit);
		}
	}

	[PresentationTestMethod]
	public void OnMainWindowCloseOverridesSecondaryWindowCancellationFromTryShutdown()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
			Setup(lifetime);

			var hasExit = false;
			var secondaryWindowClosingExecuted = false;
			var secondaryWindowClosedExecuted = false;

			lifetime.Exit += (_, _) => hasExit = true;

			var mainWindow = new Window();
			mainWindow.Show();

			lifetime.MainWindow = mainWindow;

			var window = new Window();
			window.Closing += (_, args) =>
			{
				secondaryWindowClosingExecuted = true;
				args.Cancel = true;
			};
			window.Closed += (_, _) => { secondaryWindowClosedExecuted = true; };
			window.Show();

			lifetime.TryShutdown();

			CornerstoneTest.IsTrue(secondaryWindowClosingExecuted);
			CornerstoneTest.IsTrue(secondaryWindowClosedExecuted);
			CornerstoneTest.IsTrue(hasExit);
		}
	}

	[PresentationTestMethod]
	public void SetupWithClassicDesktopLifetimeShouldNotRaiseStartup()
	{
		var frameworkInitCalled = false;
		var lifetimeBuilderCalled = false;
		var startupRaised = false;

		CreateAppBuilder(onFrameworkInitializationCompleted: () => frameworkInitCalled = true)
			.SetupWithClassicDesktopLifetime(
				["foo", "bar"],
				l =>
				{
					lifetimeBuilderCalled = true;
					l.Startup += (_, _) => startupRaised = true;
				});

		CornerstoneTest.IsTrue(frameworkInitCalled);
		CornerstoneTest.IsTrue(lifetimeBuilderCalled);
		CornerstoneTest.IsFalse(startupRaised);
	}

	[PresentationTestMethod]
	public void SetupWithClassicDesktopLifetimeShouldSubscribeToPlatformShutdownRequested()
	{
		var lifetimeEvents = new StubPlatformLifetimeEvents();
		ClassicDesktopStyleApplicationLifetime lifetime = null;

		CreateAppBuilder(lifetimeEvents).SetupWithClassicDesktopLifetime(
			[],
			l => lifetime = (ClassicDesktopStyleApplicationLifetime) l);

		CornerstoneTest.IsNotNull(lifetime);

		using (lifetime)
		{
			var window = new Window();
			window.Show();

			var raised = 0;

			lifetime.ShutdownRequested += (_, e) =>
			{
				e.Cancel = true;
				++raised;
			};

			lifetimeEvents.Raise(x => x.ShutdownRequested += null, new ShutdownRequestedEventArgs());

			CornerstoneTest.AreEqual(1, raised);
			CornerstoneTest.AreEqual([window], lifetime.Windows);
		}
	}

	[PresentationTestMethod]
	public void ShouldAllowCancelingShutdownViaShutdownRequestedEvent()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow.With()))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			var lifetimeEvents = new StubPlatformLifetimeEvents();
			PresentationLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(lifetimeEvents);

			// Force exit immediately
			Dispatcher.UIThread.Post(Dispatcher.UIThread.ExitAllFrames);
			lifetime.Start(Array.Empty<string>());

			var window = new Window();
			var raised = 0;

			window.Show();

			lifetime.ShutdownRequested += (_, e) =>
			{
				e.Cancel = true;
				++raised;
			};

			lifetimeEvents.Raise(x => x.ShutdownRequested += null, new ShutdownRequestedEventArgs());

			CornerstoneTest.AreEqual(1, raised);
			CornerstoneTest.AreEqual(new[] { window }, lifetime.Windows);
		}
	}

	[PresentationTestMethod]
	public void ShouldCloseAllRemainingOpenWindowsAfterExplicitExitCall()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			var windows = new List<Window> { new(), new(), new(), new() };

			foreach (var window in windows)
			{
				window.Show();
			}
			CornerstoneTest.AreEqual(4, lifetime.Windows.Count);
			lifetime.Shutdown();

			CornerstoneTest.Empty(lifetime.Windows);
		}
	}

	[PresentationTestMethod]
	public void ShouldExitAfterLastWindowClosed()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			lifetime.ShutdownMode = ShutdownMode.OnLastWindowClose;
			Setup(lifetime);

			var hasExit = false;

			lifetime.Exit += (_, _) => hasExit = true;

			var windowA = new Window();

			windowA.Show();

			var windowB = new Window();

			windowB.Show();

			windowA.Close();

			CornerstoneTest.IsFalse(hasExit);

			windowB.Close();

			CornerstoneTest.IsTrue(hasExit);
		}
	}

	[PresentationTestMethod]
	public void ShouldExitAfterMainWindowClosed()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
			Setup(lifetime);

			var hasExit = false;

			lifetime.Exit += (_, _) => hasExit = true;

			var mainWindow = new Window();

			mainWindow.Show();

			lifetime.MainWindow = mainWindow;

			var window = new Window();

			window.Show();

			mainWindow.Close();

			CornerstoneTest.IsTrue(hasExit);
		}
	}

	[PresentationTestMethod]
	public void ShouldOnlyExitOnExplicitExit()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			Setup(lifetime);

			var hasExit = false;

			lifetime.Exit += (_, _) => hasExit = true;

			var windowA = new Window();

			windowA.Show();

			var windowB = new Window();

			windowB.Show();

			windowA.Close();

			CornerstoneTest.IsFalse(hasExit);

			windowB.Close();

			CornerstoneTest.IsFalse(hasExit);

			lifetime.Shutdown();

			CornerstoneTest.IsTrue(hasExit);
		}
	}

	[PresentationTestMethod]
	public void ShouldSetExitCodeAfterShutdown()
	{
		using (UnitTestApplication.Start(new TestServices()))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			Dispatcher.UIThread.Post(() => lifetime.Shutdown(1337));
			var exitCode = lifetime.Start(Array.Empty<string>());

			CornerstoneTest.AreEqual(1337, exitCode);
		}
	}

	[PresentationTestMethod]
	public void ShowShouldAddWindowToOpenWindows()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			var window = new Window();

			window.Show();

			CornerstoneTest.AreEqual(new[] { window }, lifetime.Windows);
		}
	}

	[PresentationTestMethod]
	public void ShutdownDoesntRaiseShutdownRequested()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			var hasExit = false;

			lifetime.Exit += (_, _) => hasExit = true;

			var raised = 0;

			lifetime.ShutdownRequested += (_, _) => { ++raised; };

			lifetime.Shutdown();

			CornerstoneTest.AreEqual(0, raised);
			CornerstoneTest.IsTrue(hasExit);
		}
	}

	[PresentationTestMethod]
	public void ShutdownNotCancellableByPreventingWindowClose()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow.With()))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			var hasExit = false;
			var closingRaised = 0;
			var closedRaised = 0;

			lifetime.Exit += (_, _) => hasExit = true;

			var windowA = new Window();

			windowA.Show();

			var windowB = new Window();

			windowB.Show();

			windowA.Closing += (_, e) =>
			{
				e.Cancel = true;
				++closingRaised;
			};
			windowA.Closed += (_, e) => { ++closedRaised; };

			lifetime.Shutdown();

			CornerstoneTest.AreEqual(1, closingRaised);
			CornerstoneTest.AreEqual(1, closedRaised);
			CornerstoneTest.IsTrue(hasExit);
		}
	}

	[PresentationTestMethod]
	public void StartAfterSetupWithClassicDesktopLifetimeShouldNotRaiseStartupTwice()
	{
		ClassicDesktopStyleApplicationLifetime lifetime = null;
		var raised = 0;

		CreateAppBuilder().SetupWithClassicDesktopLifetime(
			[],
			l =>
			{
				lifetime = (ClassicDesktopStyleApplicationLifetime) l;
				l.Startup += (_, _) => ++raised;
			});

		CornerstoneTest.IsNotNull(lifetime);

		using (lifetime)
		{
			CornerstoneTest.AreEqual(0, raised);

			Dispatcher.UIThread.Post(Dispatcher.UIThread.ExitAllFrames);
			lifetime.Start([]);

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void TryShutdownCancellableByPreventingWindowClose()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			lifetime.Exit += (_, _) => CornerstoneTest.Fail("lifetime.Exit was called.");
			Dispatcher.UIThread.ShutdownStarted += UiThreadOnShutdownStarted;

			static void UiThreadOnShutdownStarted(object sender, EventArgs e)
			{
				CornerstoneTest.Fail("Threading.Dispatcher.UIThread.ShutdownStarted was called.");
			}

			var windowA = new Window();

			windowA.Show();

			var windowB = new Window();

			windowB.Show();

			var raised = 0;

			windowA.Closing += (_, e) =>
			{
				e.Cancel = true;
				++raised;
			};

			lifetime.TryShutdown();

			CornerstoneTest.AreEqual(1, raised);

			Dispatcher.UIThread.ShutdownStarted -= UiThreadOnShutdownStarted;
		}
	}

	[PresentationTestMethod]
	public void WindowShouldBeAddedToOpenWindowsOnlyOnce()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
		{
			Setup(lifetime);

			var window = new Window();

			window.Show();
			window.Show();
			window.IsVisible = true;

			CornerstoneTest.AreEqual(new[] { window }, lifetime.Windows);

			window.Close();
		}
	}

	private static AppBuilder CreateAppBuilder(
		IPlatformLifetimeEventsImpl platformLifetimeEvents = null,
		Action onFrameworkInitializationCompleted = null)
	{
		AppBuilder.ResetSetupForUnitTests();

		return AppBuilder.Configure(() => new SetupTestApplication(onFrameworkInitializationCompleted))
			.UseRuntimePlatformSubsystem(() => { })
			.UseRenderingSubsystem(() => { })
			.UseTextShapingSubsystem(() => { })
			.UseWindowingSubsystem(() =>
			{
				if (platformLifetimeEvents is not null)
				{
					PresentationLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(platformLifetimeEvents);
				}
			});
	}

	private static void Setup(ClassicDesktopStyleApplicationLifetime lifetime)
	{
		ISetupApplicationLifetime setupLifetime = lifetime;
		setupLifetime.BeforeAppInit();
		setupLifetime.AfterAppInit();
	}

	#endregion

	#region Classes

	private sealed class SetupTestApplication(Action onFrameworkInitializationCompleted)
		: UnitTestApplication(TestServices.StyledWindow)
	{
		#region Fields

		private bool _servicesRegistered;

		#endregion

		#region Methods

		public override void OnFrameworkInitializationCompleted()
		{
			base.OnFrameworkInitializationCompleted();
			onFrameworkInitializationCompleted?.Invoke();
		}

		public override void RegisterServices()
		{
			if (_servicesRegistered)
			{
				return;
			}

			_servicesRegistered = true;
			base.RegisterServices();
		}

		#endregion
	}

	#endregion
}