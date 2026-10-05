#region References

using System;
using System.Threading;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

/// <summary>
/// Test application running on the real headless platform, set up through <see cref="AppBuilder" />
/// so that the platform owns its own slice of the locator.
/// </summary>
/// <remarks>
/// This is deliberately not a <see cref="TestServices" /> flavour: the headless platform registers
/// services of its own (keyboard device, clipboard, render loop, platform settings, hotkey config),
/// and <see cref="UnitTestApplication" /> binds the same keys from its service bag, so the two
/// overwrite each other depending on when the platform happens to initialize.
/// </remarks>
[TestClass]
public class HeadlessUnitTestApplication : Application
{
	#region Constructors

	public HeadlessUnitTestApplication()
	{
		Styles.Add(new CornerstoneTheme());
	}

	#endregion

	#region Methods

	public static Scope Start(PresentationHeadlessPlatformOptions options = null)
	{
		var scope = PresentationLocator.EnterScope();
		var oldContext = SynchronizationContext.Current is PresentationSynchronizationContext
			? null
			: SynchronizationContext.Current;

		try
		{
			Dispatcher.ResetBeforeUnitTests();

			AppBuilder.Configure<HeadlessUnitTestApplication>()

				// Popups default to dedicated top-levels here, matching the desktop platforms
				// and the app used by Cornerstone.Presentation.UnitTests.Headless.
				.UseHeadless(options ?? new PresentationHeadlessPlatformOptions { OverlayPopups = false })
				.AfterPlatformServicesSetup(_ => PresentationLocator.CurrentMutable
					.Bind<IFontManagerImpl>().ToConstant(new TestFontManager())
					.Bind<IGlobalClock>().ToConstant(new MockGlobalClock())
					.Bind<CornerstoneXamlLoader.IRuntimeXamlLoader>().ToConstant(new TestRuntimeXamlLoader()))
				.SetupUnsafe();
		}
		catch
		{
			scope.Dispose();
			throw;
		}

		return new Scope(scope, oldContext);
	}

	#endregion

	#region Classes

	public sealed class Scope(IDisposable locatorScope, SynchronizationContext oldContext) : IDisposable
	{
		#region Methods

		public void Dispose()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				Dispatcher.UIThread.RunJobs();
			}

			Current?.DetachLifetimeHandlers();
			(PresentationLocator.Current.GetService<IToolTipService>() as ToolTipService)?.Dispose();
			(PresentationLocator.Current.GetService<FontManager>() as IDisposable)?.Dispose();
			(PresentationLocator.Current.GetService<IInputManager>() as IDisposable)?.Dispose();

			PresentationHeadlessPlatform.ResetForUnitTests();
			Dispatcher.ResetForUnitTests();
			locatorScope.Dispose();
			Dispatcher.ResetBeforeUnitTests();
			SynchronizationContext.SetSynchronizationContext(oldContext);
		}

		/// <summary>
		/// Runs pending dispatcher jobs, tied to the test's cancellation token. Headless top-levels
		/// post activation, resizes and rendering, so tests have to let those settle between acts.
		/// </summary>
		public void RunJobs(DispatcherPriority priority = default)
		{
			Dispatcher.UIThread.RunJobs(priority, CancellationToken.None);
		}

		#endregion
	}

	#endregion
}