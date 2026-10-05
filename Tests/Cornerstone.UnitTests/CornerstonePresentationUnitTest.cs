#region References

using System;
using System.Threading;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Theme.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

/// <summary>
/// Base for Cornerstone control / layout tests.
/// Uses HeadlessUnitTestSession so UI work runs on a live dispatcher loop
/// (Dispatcher.UIThread.Invoke deadlocks — there is no pump on the test thread).
/// </summary>
[TestClass]
public class CornerstoneCornerstoneUnitTest : CornerstoneUnitTest
{
	#region Fields

	private static readonly object SessionLock = new();
	private static HeadlessUnitTestSession _session;

	#endregion

	#region Properties

	/// <summary>
	/// Shared headless session for this test assembly (see PresentationTestApplicationAttribute).
	/// </summary>
	private static HeadlessUnitTestSession Session
	{
		get
		{
			if (_session != null)
			{
				return _session;
			}

			lock (SessionLock)
			{
				_session ??= HeadlessUnitTestSession.GetOrStartForAssembly(typeof(TestAppBuilder).Assembly);
			}

			return _session;
		}
	}

	#endregion

	#region Methods

	[AssemblyInitialize]
	public static void AssemblyInitialize(TestContext context)
	{
		// Create the headless session (and its dedicated UI thread) before any test constructs
		// Cornerstone objects on an MSTest worker thread.
		_ = Session;
	}

	/// <summary>
	/// Run action on the headless Cornerstone UI thread. Required when constructing controls,
	/// loading XAML, or measuring layout — MSTest may invoke tests off the UI thread.
	/// </summary>
	protected static void RunOnUi(Action action)
	{
		if (action == null)
		{
			throw new ArgumentNullException(nameof(action));
		}

		// Nested re-entry only when the headless session has already created Application.
		// Do not treat CheckAccess alone as sufficient: Cornerstone can bind a dispatcher to the
		// MSTest thread before the session starts, which would skip Dispatch and hang or miss resources.
		if (CanRunInlineOnUiThread())
		{
			EnsureApplicationResourcesForStaticResource();
			action();
			return;
		}

		// Bound wait so a stuck dispatcher surfaces as a timeout instead of hanging the suite forever.
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
		Session.Dispatch(() =>
		{
			EnsureApplicationResourcesForStaticResource();
			action();
		}, cts.Token).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Run function on the headless Cornerstone UI thread and return its result.
	/// </summary>
	protected static T RunOnUi<T>(Func<T> function)
	{
		if (function == null)
		{
			throw new ArgumentNullException(nameof(function));
		}

		if (CanRunInlineOnUiThread())
		{
			EnsureApplicationResourcesForStaticResource();
			return function();
		}

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
		return Session.Dispatch(() =>
		{
			EnsureApplicationResourcesForStaticResource();
			return function();
		}, cts.Token).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Flush pending dispatcher jobs (layout, bindings, deferred posts) on the UI thread.
	/// </summary>
	protected static void RunUiJobs()
	{
		RunOnUi(static () => global::Cornerstone.Presentation.Threading.Dispatcher.UIThread.RunJobs());
	}

	private static void AddResource(Application application, string key, object value)
	{
		if (!application.Resources.ContainsKey(key))
		{
			application.Resources.Add(key, value);
		}
	}

	private static bool CanRunInlineOnUiThread()
	{
		return (Application.Current != null)
			&& global::Cornerstone.Presentation.Threading.Dispatcher.UIThread.CheckAccess();
	}

	/// <summary>
	/// StaticResource during control XAML load resolves Application.Resources dictionary keys.
	/// Theme Styles / MergedDictionaries alone are not enough for unloaded controls in headless tests.
	/// Re-run every Dispatch: PerTest isolation recreates Application each time.
	/// </summary>
	private static void EnsureApplicationResourcesForStaticResource()
	{
		var application = Application.Current;
		if (application == null)
		{
			return;
		}

		// Explicit instances — StaticResource only sees top-level Application.Resources entries
		// for controls created outside a visual tree (sample tabs, etc.).
		AddResource(application, "TimeSpanConverter", new TimeSpanConverter());
		AddResource(application, "DoubleToDecimalConverter", new DoubleToDecimalConverter());
		AddResource(application, "DecimalToDoubleConverter", new DecimalToDoubleConverter());
		AddResource(application, "DateTimeConverter", new DateTimeConverter());
		AddResource(application, "CornerRadiusConverter", new CornerRadiusConverter());
		AddResource(application, "ThicknessConverter", new ThicknessConverter());
		AddResource(application, "PercentWidthConverter", new PercentWidthConverter());
		AddResource(application, "ProgressWidthConverter", new ProgressWidthConverter());
		AddResource(application, "ActivityLevelConverter", new ActivityLevelConverter());
	}

	#endregion
}