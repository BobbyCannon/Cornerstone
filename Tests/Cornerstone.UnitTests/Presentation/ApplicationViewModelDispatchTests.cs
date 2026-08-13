#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class ApplicationViewModelDispatchTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public async Task RequestDispatchWakesIdleWaitAndEntersActive()
	{
		// Slow idle period: RequestDispatch must wake early rather than wait ~500 ms.
		var app = new ApplicationViewModel(
			this,
			Dispatcher,
			idleUpdatesPerSecond: 2,
			activeUpdatesPerSecond: 50,
			idleTicksBeforeThrottle: 8);

		try
		{
			app.InitializeLifecycle();
			app.LoadLifecycle();
			app.StartLifecycle();

			IsFalse(app.IsDispatchActive);

			app.RequestDispatch();

			var becameActive = await WaitForAsync(() => app.IsDispatchActive, TimeSpan.FromSeconds(2));
			IsTrue(becameActive);
		}
		finally
		{
			if (app.IsLifecycleStarted())
			{
				app.StopLifecycle();
			}

			app.UnloadLifecycle();
			app.UninitializeLifecycle();
		}
	}

	[TestMethod]
	public async Task StopLifecycleUnblocksWorker()
	{
		var app = new ApplicationViewModel(
			this,
			Dispatcher,
			idleUpdatesPerSecond: 1,
			activeUpdatesPerSecond: 10,
			idleTicksBeforeThrottle: 8);

		app.InitializeLifecycle();
		app.LoadLifecycle();
		app.StartLifecycle();
		IsTrue(app.IsLifecycleStarted());

		app.StopLifecycle();

		var completed = await WaitForAsync(() => !app.IsLifecycleStarted(), TimeSpan.FromSeconds(2));
		IsTrue(completed);

		app.UnloadLifecycle();
		app.UninitializeLifecycle();
	}

	[TestMethod]
	public void RequestDispatchBeforeStartDoesNotThrow()
	{
		var app = new ApplicationViewModel(this, Dispatcher);
		app.RequestDispatch();
	}

	private static async Task<bool> WaitForAsync(Func<bool> condition, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		while (DateTime.UtcNow < deadline)
		{
			if (condition())
			{
				return true;
			}

			await Task.Delay(10);
		}

		return condition();
	}

	#endregion
}
