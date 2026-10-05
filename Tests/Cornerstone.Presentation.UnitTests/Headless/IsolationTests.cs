#region References

using System;
using System.Reflection;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class IsolationTests
{
	#region Fields

	private static WeakReference<Application> spreviousAppRef;
	private static WeakReference<Dispatcher> spreviousDispatcherRef;

	#endregion

	#region Methods

	[HeadlessTestMethod]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	public void ApplicationInstanceShouldMatchIsolationLevel(int runIndex)
	{
		var currentApp = Application.Current;
		var currentDispatcher = Dispatcher.UIThread;

		if (spreviousAppRef is not null && spreviousDispatcherRef is not null)
		{
			var isolationLevel =
				GetType().Assembly.GetCustomAttribute<PresentationTestIsolationAttribute>()?.IsolationLevel ??
				PresentationTestIsolationLevel.PerTest;

			if (isolationLevel == PresentationTestIsolationLevel.PerTest)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				AssertHelper.False(spreviousAppRef.TryGetTarget(out var previousApp),
					"Previous Application instance should have been collected.");
				AssertHelper.False(spreviousDispatcherRef.TryGetTarget(out var previousDispatcher),
					"Previous Dispatcher instance should have been collected.");

				AssertHelper.False(previousApp == currentApp);
				AssertHelper.False(previousDispatcher == currentDispatcher);
			}
			else if (isolationLevel == PresentationTestIsolationLevel.PerAssembly)
			{
				AssertHelper.True(spreviousAppRef.TryGetTarget(out var previousApp),
					"Previous Application instance should still be alive.");
				AssertHelper.True(spreviousDispatcherRef.TryGetTarget(out var previousDispatcher),
					"Previous Dispatcher instance should still be alive.");

				AssertHelper.True(previousApp == currentApp);
				AssertHelper.True(previousDispatcher == currentDispatcher);
			}
			else
			{
				throw new InvalidOperationException($"Unknown isolation level: {isolationLevel}");
			}
		}

		spreviousAppRef = new WeakReference<Application>(currentApp);
		spreviousDispatcherRef = new WeakReference<Dispatcher>(currentDispatcher);
	}

	#endregion
}