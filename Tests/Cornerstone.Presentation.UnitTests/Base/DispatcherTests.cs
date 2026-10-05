#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public partial class DispatcherTests
{
	#region Methods

	[PresentationTestMethod]
	public async Task AwaitWithPriorityRunsOnCurrentThread()
	{
		static async Task<int> Workload(Dispatcher dispatcher)
		{
			await Task.Delay(1).ConfigureAwait(false);
			CornerstoneTest.IsFalse(dispatcher.CheckAccess());

			return Thread.CurrentThread.ManagedThreadId;
		}

		using var services = new DispatcherServices(new SimpleControlledDispatcherImpl());

		var tokenSource = new CancellationTokenSource();
		var dispatcher = Dispatcher.CurrentDispatcher;

		var workload = dispatcher.InvokeAsync(async () =>
		{
			CornerstoneTest.IsTrue(dispatcher.CheckAccess());
			Task taskWithoutResult = Workload(dispatcher);

			await dispatcher.AwaitWithPriority(taskWithoutResult, DispatcherPriority.Default);

			CornerstoneTest.IsTrue(dispatcher.CheckAccess());
			var taskWithResult = Workload(dispatcher);

			await dispatcher.AwaitWithPriority(taskWithResult, DispatcherPriority.Default);

			CornerstoneTest.IsTrue(dispatcher.CheckAccess());

			tokenSource.Cancel();
		});

		dispatcher.MainLoop(tokenSource.Token);
	}

	[PresentationTestMethod]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public void CanWaitForDispatcherOperationFromTheSameThread(bool controlled, bool foreground)
	{
		var impl = controlled ? new SimpleControlledDispatcherImpl() : new SimpleDispatcherImpl();
		Dispatcher.InitializeUIThreadDispatcher(impl);
		var finished = false;

		_uiThread.InvokeAsync(() => finished = true,
			foreground ? DispatcherPriority.Default : DispatcherPriority.Background).Wait();

		CornerstoneTest.IsTrue(finished);
		if (controlled)
		{
			CornerstoneTest.AreEqual(foreground ? 0 : 1, ((SimpleControlledDispatcherImpl) impl).RunLoopCount);
		}
	}

	[PresentationTestMethod]
	public void DisableProcessingShouldStopProcessing()
	{
		using (new DispatcherServices(new SimpleControlledDispatcherImpl()))
		{
			var helper = new WaitHelper();
			PresentationLocator.CurrentMutable.Bind<NonPumpingLockHelper.IHelperImpl>().ToConstant(helper);
			using (Dispatcher.UIThread.DisableProcessing())
			{
				CornerstoneTest.IsTrue(SynchronizationContext.Current is NonPumpingSyncContext);
				Assert.Throws<InvalidOperationException>(() => Dispatcher.UIThread.MainLoop(CancellationToken.None));
				Assert.Throws<InvalidOperationException>(() => Dispatcher.UIThread.RunJobs());
			}

			var avaloniaContext = new PresentationSynchronizationContext(Dispatcher.UIThread, DispatcherPriority.Default, true);
			SynchronizationContext.SetSynchronizationContext(avaloniaContext);

			var waitHandle = new ManualResetEvent(true);

			helper.WaitCount = 0;
			waitHandle.WaitOne(100);
			CornerstoneTest.AreEqual(0, helper.WaitCount);
			using (Dispatcher.UIThread.DisableProcessing())
			{
				CornerstoneTest.AreEqual(avaloniaContext, SynchronizationContext.Current);
				waitHandle.WaitOne(100);
				CornerstoneTest.AreEqual(1, helper.WaitCount);
			}
		}
	}

	[PresentationTestMethod]
	public async Task DispatcherCanActAsTaskScheduler()
	{
		var impl = new SimpleDispatcherImpl();
		Dispatcher.InitializeUIThreadDispatcher(impl);
		Thread continuationThread = null;
		_ = Task.CompletedTask.ContinueWith(t => continuationThread = Thread.CurrentThread, Dispatcher.UIThread.ToTaskScheduler());
		CornerstoneTest.IsTrue(impl.AskedForSignal);
		impl.ExecuteSignal();
		CornerstoneTest.AreEqual(Dispatcher.UIThread.Thread, continuationThread);
	}

	[PresentationTestMethod]
	public void DispatcherExecutesJobsAccordingToPriority()
	{
		var impl = new SimpleDispatcherImpl();
		Dispatcher.InitializeUIThreadDispatcher(impl);
		var actions = new List<string>();
		_uiThread.Post(() => actions.Add("Background"), DispatcherPriority.Background);
		_uiThread.Post(() => actions.Add("Render"), DispatcherPriority.Render);
		_uiThread.Post(() => actions.Add("Input"), DispatcherPriority.Input);
		CornerstoneTest.IsTrue(impl.AskedForSignal);
		impl.ExecuteSignal();
		CornerstoneTest.AreEqual(new[] { "Render", "Input", "Background" }, actions);
	}

	[PresentationTestMethod]
	public async Task DispatcherFrameUsesCurrentDispatcher()
	{
		var uiThreadDispatcher = Dispatcher.UIThread;

		await ThreadRunHelper.RunOnDedicatedThread(() =>
		{
			var currentDispatcher = Dispatcher.CurrentDispatcher;
			var frame = new DispatcherFrame();

			CornerstoneTest.NotSame(uiThreadDispatcher, currentDispatcher);
			CornerstoneTest.Same(currentDispatcher, frame.Dispatcher);
		});
	}

	[PresentationTestMethod]
	public void DispatcherInvokeAsyncUnwrapsTasks()
	{
		var asyncMethodStage = 0;

		async Task AsyncMethod()
		{
			asyncMethodStage = 1;
			await Task.Delay(200);
			asyncMethodStage = 2;
		}

		async Task<int> AsyncMethodWithResult()
		{
			await Task.Delay(100);
			return 1;
		}

		async Task Test()
		{
			await Dispatcher.UIThread.InvokeAsync(AsyncMethod);
			CornerstoneTest.AreEqual(2, asyncMethodStage);
			CornerstoneTest.AreEqual(1, await Dispatcher.UIThread.InvokeAsync(AsyncMethodWithResult));
			asyncMethodStage = 0;

			await Dispatcher.UIThread.InvokeAsync(AsyncMethod, DispatcherPriority.Default);
			CornerstoneTest.AreEqual(2, asyncMethodStage);
			CornerstoneTest.AreEqual(1, await Dispatcher.UIThread.InvokeAsync(AsyncMethodWithResult, DispatcherPriority.Default));

			Dispatcher.UIThread.ExitAllFrames();
		}

		using (new DispatcherServices(new ManagedDispatcherImpl(null)))
		{
			var t = Test();
			var cts = new CancellationTokenSource();
			Task.Delay(3000, CancellationToken.None).ContinueWith(_ => cts.Cancel(), CancellationToken.None);
			Dispatcher.UIThread.MainLoop(cts.Token);
			CornerstoneTest.IsTrue(t.IsCompletedSuccessfully);
			t.GetAwaiter().GetResult();
		}
	}

	[PresentationTestMethod]
	public void DispatcherOperationsHaveContextWithProperPriority()
	{
		using (new DispatcherServices(new SimpleControlledDispatcherImpl()))
		{
			SynchronizationContext.SetSynchronizationContext(null);
			var disp = Dispatcher.UIThread;
			var priorities = new List<DispatcherPriority>();

			void DumpCurrentPriority()
			{
				priorities.Add(((PresentationSynchronizationContext) SynchronizationContext.Current!).Priority);
			}

			disp.Post(DumpCurrentPriority, DispatcherPriority.Normal);
			disp.Post(DumpCurrentPriority, DispatcherPriority.Loaded);
			disp.Post(DumpCurrentPriority, DispatcherPriority.Input);
			disp.Post(() =>
			{
				DumpCurrentPriority();
				disp.ExitAllFrames();
			}, DispatcherPriority.Background);
			disp.MainLoop(CancellationToken.None);

			disp.Send(_ => DumpCurrentPriority(), DispatcherPriority.Send);
			disp.Invoke(DumpCurrentPriority, DispatcherPriority.Send, CancellationToken.None);
			disp.Invoke(() =>
			{
				DumpCurrentPriority();
				return 1;
			}, DispatcherPriority.Send);

			CornerstoneTest.AreEqual(new[]
			{
				DispatcherPriority.Normal, DispatcherPriority.Loaded, DispatcherPriority.Input, DispatcherPriority.Background, DispatcherPriority.Send,
				DispatcherPriority.Send, DispatcherPriority.Send
			}, priorities);
		}
	}

	[PresentationTestMethod]
	public void DispatcherPreservesOrderWhenChangingPriority()
	{
		var impl = new SimpleDispatcherImpl();
		Dispatcher.InitializeUIThreadDispatcher(impl);
		var actions = new List<string>();
		var toPromote = _uiThread.InvokeAsync(() => actions.Add("PromotedRender"), DispatcherPriority.Background, CancellationToken.None);
		var toPromote2 = _uiThread.InvokeAsync(() => actions.Add("PromotedRender2"), DispatcherPriority.Input, CancellationToken.None);
		_uiThread.Post(() => actions.Add("Render"), DispatcherPriority.Render);
		toPromote.Priority = DispatcherPriority.Render;
		toPromote2.Priority = DispatcherPriority.Render;

		CornerstoneTest.IsTrue(impl.AskedForSignal);
		impl.ExecuteSignal();

		CornerstoneTest.AreEqual(new[] { "PromotedRender", "PromotedRender2", "Render" }, actions);
	}

	[PresentationTestMethod]
	public void DispatcherRepeatsBackgroundProcessingRequestToTheNewImplementation()
	{
		var actions = new List<string>();

		// Requests background processing from the pre-initialization implementation
		_uiThread.Post(() => actions.Add("Background"), DispatcherPriority.Background);

		var impl = new SimpleDispatcherWithBackgroundProcessingImpl();
		Dispatcher.InitializeUIThreadDispatcher(impl);

		CornerstoneTest.IsTrue(impl.AskedForBackgroundProcessing);
		impl.FireBackgroundProcessing();
		CornerstoneTest.AreEqual(new[] { "Background" }, actions);
	}

	[PresentationTestMethod]
	public void DispatcherRepeatsSignalToTheNewImplementation()
	{
		var actions = new List<string>();

		// Signals the pre-initialization implementation
		_uiThread.Post(() => actions.Add("Render"), DispatcherPriority.Render);

		var impl = new SimpleDispatcherWithBackgroundProcessingImpl();
		Dispatcher.InitializeUIThreadDispatcher(impl);

		CornerstoneTest.IsTrue(impl.AskedForSignal);
		impl.ExecuteSignal();
		CornerstoneTest.AreEqual(new[] { "Render" }, actions);
	}

	[PresentationTestMethod]
	public async Task DispatcherResumeContinuesOnCurrentThread()
	{
		using var services = new DispatcherServices(new SimpleControlledDispatcherImpl());

		var tokenSource = new CancellationTokenSource();
		var dispatcher = Dispatcher.CurrentDispatcher;

		var workload = dispatcher.InvokeAsync(async () =>
		{
			CornerstoneTest.IsTrue(dispatcher.CheckAccess());

			await Task.Delay(1).ConfigureAwait(false);
			CornerstoneTest.IsFalse(dispatcher.CheckAccess());

			await dispatcher.Resume();
			CornerstoneTest.IsTrue(dispatcher.CheckAccess());

			tokenSource.Cancel();
		});

		dispatcher.MainLoop(tokenSource.Token);
	}

	[PresentationTestMethod]
	public void DispatcherStopsItemProcessingWhenInputIsPending()
	{
		Dispatcher.ResetForUnitTests();

		var impl = new SimpleDispatcherImpl();
		impl.TestInputPending = true;
		_uiThread = new Dispatcher(impl);

		var actions = new List<int>();
		for (var c = 0; c < 10; c++)
		{
			var itemId = c;
			_uiThread.Post(() =>
			{
				actions.Add(itemId);
				if ((itemId == 0) || (itemId == 3) || (itemId == 7))
				{
					impl.TestInputPending = true;
				}
			}, DispatcherPriority.Background);
		}
		CornerstoneTest.IsFalse(impl.AskedForSignal);
		CornerstoneTest.IsNotNull(impl.NextTimer);
		impl.TestInputPending = false;

		for (var c = 0; c < 4; c++)
		{
			CornerstoneTest.IsNotNull(impl.NextTimer);
			impl.ExecuteTimer();
			CornerstoneTest.IsFalse(impl.AskedForSignal);
			var expectedCount = c switch
			{
				0 => 1,
				1 => 4,
				2 => 8,
				3 => 10,
				_ => throw new InvalidOperationException($"Unexpected value {c}")
			};

			CornerstoneTest.AreEqual(Enumerable.Range(0, expectedCount), actions);
			CornerstoneTest.IsFalse(impl.AskedForSignal);
			if (c < 3)
			{
				CornerstoneTest.IsTrue(impl.NextTimer > impl.Now);
				impl.Now = impl.NextTimer.Value + 1;
			}
			else
			{
				CornerstoneTest.IsNull(impl.NextTimer);
			}

			impl.TestInputPending = false;
		}
	}

	[PresentationTestMethod]
	public void DispatcherStopsItemProcessingWhenInteractivityDeadlineIsReached()
	{
		var impl = new SimpleDispatcherImpl();
		Dispatcher.ResetForUnitTests();
		_uiThread = new Dispatcher(impl);
		var actions = new List<int>();
		for (var c = 0; c < 10; c++)
		{
			var itemId = c;
			_uiThread.Post(() =>
			{
				actions.Add(itemId);
				impl.Now += 20;
			}, DispatcherPriority.Background);
		}

		CornerstoneTest.IsFalse(impl.AskedForSignal);
		CornerstoneTest.IsNotNull(impl.NextTimer);

		for (var c = 0; c < 4; c++)
		{
			CornerstoneTest.IsNotNull(impl.NextTimer);
			CornerstoneTest.IsFalse(impl.AskedForSignal);
			impl.ExecuteTimer();
			CornerstoneTest.IsFalse(impl.AskedForSignal);
			impl.ExecuteSignal();
			var expectedCount = (c + 1) * 3;
			if (c == 3)
			{
				expectedCount = 10;
			}

			CornerstoneTest.AreEqual(Enumerable.Range(0, expectedCount), actions);
			CornerstoneTest.IsFalse(impl.AskedForSignal);
			if (c < 3)
			{
				CornerstoneTest.IsTrue(impl.NextTimer > impl.Now);
			}
			else
			{
				CornerstoneTest.IsNull(impl.NextTimer);
			}
		}
	}

	[PresentationTestMethod]
	public async Task DispatcherYieldContinuesOnCurrentThread()
	{
		using var services = new DispatcherServices(new SimpleControlledDispatcherImpl());

		var tokenSource = new CancellationTokenSource();
		var dispatcher = Dispatcher.CurrentDispatcher;

		var workload = dispatcher.InvokeAsync(async () =>
		{
			CornerstoneTest.IsTrue(dispatcher.CheckAccess());

			await Dispatcher.Yield();
			CornerstoneTest.IsTrue(dispatcher.CheckAccess());

			tokenSource.Cancel();
		});

		dispatcher.MainLoop(tokenSource.Token);
	}

	[PresentationTestMethod]
	public void ExitAllFramesShouldExitAllFramesAndBeAbleToContinue()
	{
		using (new DispatcherServices(new SimpleControlledDispatcherImpl()))
		{
			var actions = new List<string>();
			var disp = Dispatcher.UIThread;
			disp.Post(() =>
			{
				actions.Add("Nested frame");
				Dispatcher.UIThread.MainLoop(CancellationToken.None);
				actions.Add("Nested frame exited");
			});
			disp.Post(() =>
			{
				actions.Add("ExitAllFrames");
				disp.ExitAllFrames();
			});

			disp.MainLoop(CancellationToken.None);

			CornerstoneTest.AreEqual(new[] { "Nested frame", "ExitAllFrames", "Nested frame exited" }, actions);
			actions.Clear();

			var secondLoop = new CancellationTokenSource();
			disp.Post(() =>
			{
				actions.Add("Callback after exit");
				secondLoop.Cancel();
			});
			disp.MainLoop(secondLoop.Token);
			CornerstoneTest.AreEqual(new[] { "Callback after exit" }, actions);
		}
	}

	[PresentationTestMethod]
	public void MediaContextRenderSchedulingAllowsAlreadySuppressedExecutionContextFlow()
	{
		var impl = new SimpleDispatcherWithBackgroundProcessingImpl();
		using var services = new DispatcherServices(impl);

		var testObject = new AsyncLocalTestClass();
		var test = "Not measured";
		Exception schedulingException = null;
		var control = new AsyncLocalMeasureControl(() => testObject.AsyncLocalField.Value, value => test = value);
		var root = new TestRoot { Child = control };

		root.ExecuteInitialLayoutPass();
		control.RecordMeasure = true;

		Dispatcher.UIThread.Post(() =>
		{
			testObject.AsyncLocalField.Value = "Initial Value";

			try
			{
				using (ExecutionContext.SuppressFlow())
				{
					control.InvalidateMeasure();
				}
			}
			catch (Exception e)
			{
				schedulingException = e;
			}

			testObject.AsyncLocalField.Value = null;
		});

		CornerstoneTest.IsTrue(impl.AskedForSignal);
		impl.ExecuteSignal();

		CornerstoneTest.IsNull(schedulingException);
		CornerstoneTest.IsNull(test);
	}

	[PresentationTestMethod]
	public void MediaContextRenderSchedulingDoesNotCaptureAmbientExecutionContext()
	{
		var impl = new SimpleDispatcherWithBackgroundProcessingImpl();
		using var services = new DispatcherServices(impl);

		var testObject = new AsyncLocalTestClass();
		var test = "Not measured";
		var control = new AsyncLocalMeasureControl(() => testObject.AsyncLocalField.Value, value => test = value);
		var root = new TestRoot { Child = control };

		root.ExecuteInitialLayoutPass();
		control.RecordMeasure = true;

		Dispatcher.UIThread.Post(() =>
		{
			testObject.AsyncLocalField.Value = "Initial Value";
			control.InvalidateMeasure();
			testObject.AsyncLocalField.Value = null;
		});

		CornerstoneTest.IsTrue(impl.AskedForSignal);
		impl.ExecuteSignal();

		CornerstoneTest.IsNull(test);
	}

	[PresentationTestMethod]
	public void ShutdownShouldExitAllFramesAndNotAllowNewFrames()
	{
		using (new DispatcherServices(new SimpleControlledDispatcherImpl()))
		{
			var actions = new List<string>();
			var disp = Dispatcher.UIThread;
			disp.Post(() =>
			{
				actions.Add("Nested frame");
				Dispatcher.UIThread.MainLoop(CancellationToken.None);
				actions.Add("Nested frame exited");
			});

			var criticalFrame = new DispatcherFrame(false);
			disp.Post(() =>
			{
				actions.Add("Critical frame");
				Dispatcher.UIThread.PushFrame(criticalFrame);
				actions.Add("Critical frame exited");
			});
			disp.Post(() =>
			{
				actions.Add("Shutdown");
				disp.BeginInvokeShutdown(DispatcherPriority.Normal);
			});
			disp.Post(() =>
			{
				actions.Add("Nested frame after shutdown");

				// This should exit immediately and not run any jobs
				Dispatcher.UIThread.MainLoop(CancellationToken.None);
				actions.Add("Nested frame after shutdown exited");
			});
			disp.Post(() => actions.Add("Job in critical frame"));
			disp.Post(() =>
			{
				actions.Add("Stop critical frame");
				criticalFrame.Continue = false;
			});

			disp.MainLoop(CancellationToken.None);

			CornerstoneTest.AreEqual(new[]
			{
				"Nested frame",
				"Critical frame",
				"Shutdown",

				// Normal nested frames are supposed to exit immediately
				"Nested frame after shutdown", "Nested frame after shutdown exited",

				// if frame is configured to not answer dispatcher requests, it should be allowed to run
				"Job in critical frame", "Stop critical frame", "Critical frame exited",

				// After 3-rd level frames have exited, the normal nested frame exits too
				"Nested frame exited"
			}, actions);
			actions.Clear();

			disp.Post(() => actions.Add("Frame after shutdown finished"));
			Assert.Throws<InvalidOperationException>(() => disp.MainLoop(CancellationToken.None));
			CornerstoneTest.Empty(actions);
		}
	}

	public enum TestDispatcherImplKind
	{
		Simple,
		SimpleWithBackgroundProcessing,
		SimpleControlled,
		Managed
	}

	private static IDispatcherImpl CreateDispatcherImpl(TestDispatcherImplKind kind)
	{
		switch (kind)
		{
			case TestDispatcherImplKind.Simple:
				return new SimpleDispatcherImpl();
			case TestDispatcherImplKind.SimpleWithBackgroundProcessing:
				return new SimpleDispatcherWithBackgroundProcessingImpl();
			case TestDispatcherImplKind.SimpleControlled:
				return new SimpleControlledDispatcherImpl();
			case TestDispatcherImplKind.Managed:
				return new ManagedDispatcherImpl(null);
			default:
				throw new ArgumentOutOfRangeException(nameof(kind));
		}
	}

	[PresentationTestMethod]
	[DataRow(TestDispatcherImplKind.Simple)]
	[DataRow(TestDispatcherImplKind.SimpleWithBackgroundProcessing)]
	[DataRow(TestDispatcherImplKind.SimpleControlled)]
	[DataRow(TestDispatcherImplKind.Managed)]
	public void ShutdownFromOperationAbortsQueuedOperationsWithoutRunningThem(TestDispatcherImplKind kind)
	{
		var impl = CreateDispatcherImpl(kind);
		using var services = new DispatcherServices(impl);
		var disp = Dispatcher.UIThread;

		var actions = new List<string>();
		disp.ShutdownFinished += (_, _) => actions.Add("ShutdownFinished");

		var op1 = disp.InvokeAsync(() =>
		{
			actions.Add("op1");
			disp.InvokeShutdown();
		}, DispatcherPriority.Send);
		var op2 = disp.InvokeAsync(() => actions.Add("op2"), DispatcherPriority.Send);

		if (impl is IControlledDispatcherImpl)
		{
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			disp.MainLoop(timeout.Token);
		}
		else
		{
			var simple = (SimpleDispatcherImpl) impl;
			CornerstoneTest.IsTrue(simple.AskedForSignal);
			simple.ExecuteSignal();
		}

		CornerstoneTest.AreEqual(new[] { "op1", "ShutdownFinished" }, actions);
		CornerstoneTest.AreEqual(DispatcherOperationStatus.Completed, op1.Status);
		CornerstoneTest.AreEqual(DispatcherOperationStatus.Aborted, op2.Status);
	}

	[PresentationTestMethod]
	[DataRow(TestDispatcherImplKind.SimpleControlled)]
	[DataRow(TestDispatcherImplKind.Managed)]
	public void OperationsAreOnlyAbortedAfterLastFrameExits(TestDispatcherImplKind kind)
	{
		var impl = CreateDispatcherImpl(kind);
		using var services = new DispatcherServices(impl);
		var disp = Dispatcher.UIThread;

		var actions = new List<string>();
		disp.ShutdownFinished += (_, _) => actions.Add("ShutdownFinished");

		var criticalFrame = new DispatcherFrame(false);
		DispatcherOperation op2 = null, op3 = null, op4 = null;
		disp.InvokeAsync(() =>
		{
			actions.Add("op1");
			disp.InvokeShutdown();

			// Frames are still on the stack, so nothing has been aborted yet
			CornerstoneTest.AreEqual(DispatcherOperationStatus.Pending, op2.Status);
			CornerstoneTest.AreEqual(DispatcherOperationStatus.Pending, op3.Status);

			// Explicit RunJobs and synchronous Invoke still dispatch pending operations
			disp.RunJobs(DispatcherPriority.Normal);
			CornerstoneTest.AreEqual(DispatcherOperationStatus.Completed, op2.Status);
			CornerstoneTest.AreEqual(DispatcherOperationStatus.Pending, op3.Status);
			disp.Invoke(() => actions.Add("invoke"), DispatcherPriority.Normal, CancellationToken.None);
			CornerstoneTest.AreEqual(DispatcherOperationStatus.Pending, op3.Status);

			// A frame that ignores exit requests still pumps the remaining operations
			disp.PushFrame(criticalFrame);
			CornerstoneTest.AreEqual(DispatcherOperationStatus.Completed, op3.Status);

			op4 = disp.InvokeAsync(() => actions.Add("op4"), DispatcherPriority.Normal);
			CornerstoneTest.AreEqual(DispatcherOperationStatus.Pending, op4.Status);
		}, DispatcherPriority.Normal);
		op2 = disp.InvokeAsync(() => actions.Add("op2"), DispatcherPriority.Normal);
		op3 = disp.InvokeAsync(() =>
		{
			actions.Add("op3");
			criticalFrame.Continue = false;
		}, DispatcherPriority.Background);

		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		disp.MainLoop(timeout.Token);

		CornerstoneTest.AreEqual(new[] { "op1", "op2", "invoke", "op3", "ShutdownFinished" }, actions);
		CornerstoneTest.AreEqual(DispatcherOperationStatus.Aborted, op4.Status);
	}

	#endregion

	#region Classes

	private sealed class AsyncLocalMeasureControl(Func<string> getValue, Action<string> setValue) : Control
	{
		#region Properties

		public bool RecordMeasure { get; set; }

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			if (RecordMeasure)
			{
				setValue(getValue());
			}

			return new Size(1, 1);
		}

		#endregion
	}

	private class AsyncLocalTestClass
	{
		#region Properties

		public AsyncLocal<string> AsyncLocalField { get; } = new();

		#endregion
	}

	private class DispatcherServices : IDisposable
	{
		#region Fields

		private readonly IDisposable _scope;

		#endregion

		#region Constructors

		public DispatcherServices(IDispatcherImpl impl)
		{
			_scope = PresentationLocator.EnterScope();
			Dispatcher.ResetForUnitTests();
			Dispatcher.InitializeUIThreadDispatcher(impl);
			SynchronizationContext.SetSynchronizationContext(null);
		}

		#endregion

		#region Methods

		public void Dispose()
		{
			Dispatcher.ResetForUnitTests();
			_scope.Dispose();
			SynchronizationContext.SetSynchronizationContext(null);
		}

		#endregion
	}

	private class SimpleControlledDispatcherImpl : SimpleDispatcherWithBackgroundProcessingImpl, IControlledDispatcherImpl
	{
		#region Fields

		private readonly CancellationToken? _cancel;
		private readonly bool _useTestTimeout = true;

		#endregion

		#region Constructors

		public SimpleControlledDispatcherImpl()
		{
		}

		public SimpleControlledDispatcherImpl(CancellationToken cancel, bool useTestTimeout = false)
		{
			_useTestTimeout = useTestTimeout;
			_cancel = cancel;
		}

		#endregion

		#region Properties

		public int RunLoopCount { get; private set; }

		#endregion

		#region Methods

		public void RunLoop(CancellationToken token)
		{
			RunLoopCount++;
			var st = Stopwatch.StartNew();
			while (!token.IsCancellationRequested || (_cancel?.IsCancellationRequested == true))
			{
				FireBackgroundProcessing();
				ExecuteSignal();
				if (_useTestTimeout)
				{
					CornerstoneTest.IsTrue(st.ElapsedMilliseconds < 4000, "RunLoop exceeded test time quota");
				}
				else
				{
					Thread.Sleep(10);
				}
			}
		}

		#endregion
	}

	private class SimpleDispatcherImpl : IDispatcherImpl, IDispatcherImplWithPendingInput
	{
		#region Fields

		private readonly object _lock = new();
		private readonly Thread _loopThread = Thread.CurrentThread;

		#endregion

		#region Properties

		public bool AskedForSignal { get; private set; }

		public bool CanQueryPendingInput => TestInputPending != null;
		public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _loopThread;
		public bool HasPendingInput => TestInputPending == true;
		public long? NextTimer { get; private set; }

		public long Now { get; set; }
		public bool? TestInputPending { get; set; }

		#endregion

		#region Methods

		public void ExecuteSignal()
		{
			lock (_lock)
			{
				if (!AskedForSignal)
				{
					return;
				}
				AskedForSignal = false;
			}
			Signaled?.Invoke();
		}

		public void ExecuteTimer()
		{
			if (NextTimer == null)
			{
				return;
			}
			Now = NextTimer.Value;
			Timer?.Invoke();
		}

		public void Signal()
		{
			lock (_lock)
			{
				AskedForSignal = true;
			}
		}

		public void UpdateTimer(long? dueTimeInTicks)
		{
			NextTimer = dueTimeInTicks;
		}

		#endregion

		#region Events

		public event Action Signaled;
		public event Action Timer;

		#endregion
	}

	private class SimpleDispatcherWithBackgroundProcessingImpl : SimpleDispatcherImpl, IDispatcherImplWithExplicitBackgroundProcessing
	{
		#region Properties

		public bool AskedForBackgroundProcessing { get; private set; }

		#endregion

		#region Methods

		public void FireBackgroundProcessing()
		{
			if (!AskedForBackgroundProcessing)
			{
				return;
			}
			AskedForBackgroundProcessing = false;
			ReadyForBackgroundProcessing?.Invoke();
		}

		public void RequestBackgroundProcessing()
		{
			if (!CurrentThreadIsLoopThread)
			{
				throw new InvalidOperationException();
			}
			AskedForBackgroundProcessing = true;
		}

		#endregion

		#region Events

		public event Action ReadyForBackgroundProcessing;

		#endregion
	}

	private class WaitHelper : SynchronizationContext, NonPumpingLockHelper.IHelperImpl
	{
		#region Fields

		public int WaitCount;

		#endregion

		#region Methods

		public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
		{
			WaitCount++;
			return base.Wait(waitHandles, waitAll, millisecondsTimeout);
		}

		#endregion
	}

	#endregion
}