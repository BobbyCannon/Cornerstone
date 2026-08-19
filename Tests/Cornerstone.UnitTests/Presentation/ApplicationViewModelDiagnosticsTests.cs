#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Diagnostics;
using Cornerstone.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class ApplicationViewModelDiagnosticsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CopyTrackedDispatchablesReturnsTrackedRoots()
	{
		var app = new ApplicationViewModel(this, Dispatcher);
		var a = new SampleDispatchable();
		var b = new SampleDispatchable();

		a.Attach(app);
		b.Attach(app);

		var list = new List<DispatchableViewModel>();
		app.CopyTrackedDispatchables(list);

		AreEqual(2, list.Count);
		IsTrue(list.Contains(a));
		IsTrue(list.Contains(b));

		a.Detach(app);
		app.CopyTrackedDispatchables(list);
		AreEqual(1, list.Count);
		AreEqual(b, list[0]);
	}

	[TestMethod]
	public void DiagnosticsDispatchableIsNotInTrackedMembership()
	{
		var app = new ApplicationViewModel(this, Dispatcher);
		var feature = new SampleDispatchable();
		var diagnostics = new SampleDispatchable();

		feature.Attach(app);
		app.DiagnosticsDispatchable = diagnostics;

		var list = new List<DispatchableViewModel>();
		app.CopyTrackedDispatchables(list);

		AreEqual(1, list.Count);
		AreEqual(feature, list[0]);
		IsFalse(list.Contains(diagnostics));
	}

	[TestMethod]
	public async Task DiagnosticsOnlyApplyDoesNotCountInLastApplyBatchSize()
	{
		var app = new ApplicationViewModel(
			this,
			Dispatcher,
			activeUpdatesPerSecond: 50,
			idleUpdatesPerSecond: 20,
			idleTicksBeforeThrottle: 8);

		var diagnostics = new PendingDispatchable();
		var capture = new ForcePendingCapture(diagnostics);

		try
		{
			app.InitializeLifecycle();
			app.LoadLifecycle();
			app.StartLifecycle();

			app.DiagnosticsCapture = capture;
			app.DiagnosticsDispatchable = diagnostics;
			diagnostics.Attach(this);
			diagnostics.MarkPending();
			app.RequestDispatch();

			var applied = await WaitForAsync(() => diagnostics.ApplyCount > 0, TimeSpan.FromSeconds(2));
			IsTrue(applied);
			// Feature pending was empty; batch size is feature-only.
			AreEqual(0, app.LastApplyBatchSize);
			IsTrue(diagnostics.ApplyCount >= 1);
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
	public async Task DiagnosticsOnlyApplyDoesNotKeepDispatchActive()
	{
		// Diagnostics-only apply must not report "applied" or mode capture Idle↔Active loops.
		var app = new ApplicationViewModel(
			this,
			Dispatcher,
			activeUpdatesPerSecond: 80,
			idleUpdatesPerSecond: 40,
			idleTicksBeforeThrottle: 2);

		var diagnostics = new PendingDispatchable();
		// Mark pending every capture (like mode/session changes), but no feature roots.
		var capture = new ForcePendingCapture(diagnostics);

		try
		{
			app.InitializeLifecycle();
			app.LoadLifecycle();
			app.StartLifecycle();

			app.DiagnosticsCapture = capture;
			app.DiagnosticsDispatchable = diagnostics;
			diagnostics.Attach(this);
			app.RequestDispatch();

			// Allow a wake into active, then empty feature ticks should throttle back to idle
			// even though diagnostics keeps applying.
			await WaitForAsync(() => diagnostics.ApplyCount > 0, TimeSpan.FromSeconds(2));
			var settledIdle = await WaitForAsync(() => !app.IsDispatchActive, TimeSpan.FromSeconds(2));
			IsTrue(settledIdle);
			IsTrue(diagnostics.ApplyCount >= 1);
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
	public void IntervalsArePositive()
	{
		var app = new ApplicationViewModel(
			this,
			Dispatcher,
			activeUpdatesPerSecond: 60,
			idleUpdatesPerSecond: 5,
			idleTicksBeforeThrottle: 3);

		IsTrue(app.ActiveInterval.TotalMilliseconds > 0);
		IsTrue(app.IdleInterval.TotalMilliseconds > 0);
		AreEqual(0, app.LastApplyBatchSize);
	}

	#endregion

	#region Classes

	private sealed class ForcePendingCapture : IDiagnosticsCapture
	{
		private readonly PendingDispatchable _diagnostics;

		public ForcePendingCapture(PendingDispatchable diagnostics)
		{
			_diagnostics = diagnostics;
		}

		public void Capture(ApplicationViewModel host, int pendingApplyCount)
		{
			_diagnostics.MarkPending();
		}
	}

	private sealed class PendingDispatchable : DispatchableViewModel
	{
		private readonly DispatchPending _pending = new();

		public PendingDispatchable()
		{
			TrackBinding(_pending, () => ApplyCount++);
		}

		public int ApplyCount { get; private set; }

		public void MarkPending()
		{
			_pending.MarkPending();
		}
	}

	private sealed class SampleDispatchable : DispatchableViewModel
	{
	}

	private static async Task<bool> WaitForAsync(Func<bool> condition, TimeSpan timeout)
	{
		var start = DateTime.UtcNow;
		while ((DateTime.UtcNow - start) < timeout)
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
