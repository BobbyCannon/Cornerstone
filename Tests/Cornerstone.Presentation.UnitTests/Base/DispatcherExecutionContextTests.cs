#if WPFCOMPARE
using System.Windows.Threading;
using Cornerstone.Testing;
using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;
using DispatcherPriority = Cornerstone.Presentation.DispatcherPriority;
using Cornerstone.Presentation;

namespace Cornerstone.Presentation.UnitTests.WpfCompare;

[TestClass]
public class DispatcherExecutionContextTests
	#else

#region References

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class DispatcherExecutionContextTests : ScopedTestBase
#endif
{
	#region Constants

	// A second, distinct culture used as the "calling thread" culture in the cross-thread test.
	private const string CallSiteCultureName = "kk-KZ";

	// Sumerian: extremely unlikely to be the machine default, so these tests don't depend on the environment.
	private const string CustomCultureName = "sux-Shaw-UM";

	#endregion

	#region Methods

	// A culture change made *inside* an operation persists on the UI thread (the outbound half of the
	// pre-4.6 semantics). With the runtime's default ExecutionContext flow the change would be discarded.
	[PresentationTestMethod]
	public void CultureSetInsideDispatcherOperationPersistsToUIThread()
	{
		var dispatcher = Dispatcher.CurrentDispatcher;
		var custom = CultureInfo.GetCultureInfo(CustomCultureName);
		var oldCulture = Thread.CurrentThread.CurrentUICulture;
		try
		{
			string nextOpSaw = null;

			dispatcher.InvokeAsync(() => Thread.CurrentThread.CurrentUICulture = custom, DispatcherPriority.Normal);
			dispatcher.InvokeAsync(() => nextOpSaw = Thread.CurrentThread.CurrentUICulture.Name, DispatcherPriority.Normal);

			DispatcherTestServices.DrainQueue(dispatcher, DispatcherPriority.Background);

			// The change made by the first operation is visible to the next one and stays on the thread.
			CornerstoneTest.AreEqual(CustomCultureName, nextOpSaw);
			CornerstoneTest.AreEqual(CustomCultureName, Thread.CurrentThread.CurrentUICulture.Name);
		}
		finally
		{
			Thread.CurrentThread.CurrentUICulture = oldCulture;
			CornerstoneTest.AreNotEqual(CustomCultureName, oldCulture.Name);
		}
	}

	// Regression test for https://github.com/AvaloniaUI/Avalonia/issues/21451. A dispatcher operation must
	// run under the UI thread's live culture - even one queued *before* the culture was set, whose execution
	// context captured the old culture - and running operations must not reset the UI-thread culture.
	[PresentationTestMethod]
	public void DispatcherOperationRunsUnderLiveUIThreadCultureAndDoesNotResetIt()
	{
		var dispatcher = Dispatcher.CurrentDispatcher;
		var custom = CultureInfo.GetCultureInfo(CustomCultureName);
		var oldCulture = Thread.CurrentThread.CurrentUICulture;
		try
		{
			string earlyOpSaw = null;
			string lateOpSaw = null;

			// Queued BEFORE the culture is set: its execution context captures the current (default) culture.
			dispatcher.InvokeAsync(() => earlyOpSaw = Thread.CurrentThread.CurrentUICulture.Name, DispatcherPriority.Normal);

			// The application sets a custom UI culture on the UI thread.
			Thread.CurrentThread.CurrentUICulture = custom;

			dispatcher.InvokeAsync(() => lateOpSaw = Thread.CurrentThread.CurrentUICulture.Name, DispatcherPriority.Normal);

			DispatcherTestServices.DrainQueue(dispatcher, DispatcherPriority.Background);

			// The early operation runs under the LIVE UI-thread culture, not the default it captured...
			CornerstoneTest.AreEqual(CustomCultureName, earlyOpSaw);
			CornerstoneTest.AreEqual(CustomCultureName, lateOpSaw);

			// ...and running operations must not have reset the UI-thread culture.
			CornerstoneTest.AreEqual(CustomCultureName, Thread.CurrentThread.CurrentUICulture.Name);
		}
		finally
		{
			Thread.CurrentThread.CurrentUICulture = oldCulture;
			CornerstoneTest.AreNotEqual(CustomCultureName, oldCulture.Name);
		}
	}

	// A dispatcher operation runs under the UI thread's live culture, NOT under the culture of the thread
	// that happened to queue it - even though that other thread's culture would flow into a plain Task.Run
	// continuation. This is the cross-thread counterpart of the same-thread test above.
	[PresentationTestMethod]
	public void DispatcherOperationsUseLiveUIThreadCultureNotCallingThreadCulture()
	{
		var dispatcher = Dispatcher.CurrentDispatcher;
		var uiThreadCulture = CultureInfo.GetCultureInfo(CustomCultureName);
		var callSiteCulture = CultureInfo.GetCultureInfo(CallSiteCultureName);
		var oldCulture = Thread.CurrentThread.CurrentCulture;

		// This (test) thread pumps the frame below, i.e. it is the UI thread. Give it a known culture.
		Thread.CurrentThread.CurrentCulture = uiThreadCulture;
		try
		{
			var frame = new DispatcherFrame();
			string taskRunSaw = null;
			string invokeSaw = null;
			string invokeAsyncSaw = null;

			var callingThread = new Thread(() =>
			{
				// A DIFFERENT culture on this non-UI (calling) thread.
				Thread.CurrentThread.CurrentCulture = callSiteCulture;

				// Baseline: a plain Task.Run continuation DOES flow the calling-thread culture through the EC.
				taskRunSaw = Task.Run(() => Thread.CurrentThread.CurrentCulture.Name).GetAwaiter().GetResult();

				// Queue work onto the dispatcher from this non-UI thread, then stop the frame.
				dispatcher.Invoke(() => invokeSaw = Thread.CurrentThread.CurrentCulture.Name);
				dispatcher.InvokeAsync(() => invokeAsyncSaw = Thread.CurrentThread.CurrentCulture.Name, DispatcherPriority.Normal);
				dispatcher.InvokeAsync(() => frame.Continue = false, DispatcherPriority.Normal);
			});
			callingThread.Start();

			DispatcherTestServices.PushFrame(dispatcher, frame);
			callingThread.Join();

			// Baseline: Task.Run flows the calling-thread culture (guaranteed by the runtime).
			CornerstoneTest.AreEqual(CallSiteCultureName, taskRunSaw);

			// Dispatcher operations run under the UI thread's live culture, not the calling thread's culture.
			CornerstoneTest.AreEqual(CustomCultureName, invokeSaw);
			CornerstoneTest.AreEqual(CustomCultureName, invokeAsyncSaw);
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = oldCulture;
			CornerstoneTest.AreNotEqual(CustomCultureName, oldCulture.Name);
		}
	}

	// An AsyncLocal set inside one operation must not leak into the next one (each operation restores its
	// own captured context).
	[PresentationTestMethod]
	public void ExecutionContextDoesNotFlowBetweenDispatcherOperations()
	{
		var dispatcher = Dispatcher.CurrentDispatcher;
		var asyncLocal = new AsyncLocal<string>();
		var seen = "unset";

		dispatcher.InvokeAsync(() => asyncLocal.Value = "set-by-first-op", DispatcherPriority.Normal);
		dispatcher.InvokeAsync(() => seen = asyncLocal.Value, DispatcherPriority.Normal);

		DispatcherTestServices.DrainQueue(dispatcher, DispatcherPriority.Background);

		CornerstoneTest.IsNull(seen);
	}

	// Non-culture ExecutionContext state (an AsyncLocal) DOES flow into an operation from the context that
	// was captured when it was queued - this is preserved, unlike culture which is special-cased.
	[PresentationTestMethod]
	public void ExecutionContextFlowsIntoDispatcherOperation()
	{
		var dispatcher = Dispatcher.CurrentDispatcher;
		var asyncLocal = new AsyncLocal<string>();
		string seen = null;

		asyncLocal.Value = "captured";
		dispatcher.InvokeAsync(() => seen = asyncLocal.Value, DispatcherPriority.Normal);

		// Change the ambient value AFTER the operation captured its context.
		asyncLocal.Value = "ambient";

		DispatcherTestServices.DrainQueue(dispatcher, DispatcherPriority.Background);

		// The operation observes the value captured at post time, not the later ambient value.
		CornerstoneTest.AreEqual("captured", seen);
	}

	#endregion
}