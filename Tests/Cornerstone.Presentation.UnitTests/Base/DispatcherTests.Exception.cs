#region References

using System;
using System.Threading;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

// Some of these exceptions are based from https://github.com/dotnet/wpf-test/blob/05797008bb4975ceeb71be36c47f01688f535d53/src/Test/ElementServices/FeatureTests/Untrusted/Dispatcher/UnhandledExceptionTest.cs#L30
public partial class DispatcherTests : ScopedTestBase
{
	#region Constants

	private const string ExpectedExceptionText = "Exception thrown inside Threading.Dispatcher.Invoke / Dispatcher.BeginInvoke.";

	#endregion

	#region Fields

	private int _numberOfHandlerOnUnhandledEventFilterInvoked;

	private int _numberOfHandlerOnUnhandledEventInvoked;
	private Dispatcher _uiThread;

	#endregion

	#region Constructors

	public DispatcherTests()
	{
		_numberOfHandlerOnUnhandledEventInvoked = 0;
		_numberOfHandlerOnUnhandledEventFilterInvoked = 0;

		VerifyDispatcherSanity();
		_uiThread = Dispatcher.CurrentDispatcher;
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CanHandleExceptionWithUnhandledException()
	{
		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterRequestCatch;

		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionHandled;
		var caughtCorrectException = true;
		try
		{
			_uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
			_uiThread.RunJobs(null, CancellationToken.None);
		}
		catch (Exception)
		{
			// should be no exception here.
			caughtCorrectException = false;
		}
		finally
		{
			Verification(caughtCorrectException, 1, 1);
		}
	}

	[PresentationTestMethod]
	public void CanPushFrameAndShutdownDispatcherFromUnhandledException()
	{
		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterNotRequestCatchPushFrame;

		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionHandledPushFrame;
		var caughtCorrectException = false;
		try
		{
			_uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
			_uiThread.RunJobs(null, CancellationToken.None);
		}
		catch (Exception e)
		{
			caughtCorrectException = e.Message == ExpectedExceptionText;
		}
		finally
		{
			Verification(caughtCorrectException, 0, 1);
		}
	}

	[PresentationTestMethod]
	public void CanRemoveDispatcherExceptionHandler()
	{
		var caughtCorrectException = false;

		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterRequestCatch;
		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionNotHandled;

		_uiThread.UnhandledExceptionFilter -=
			HandlerOnUnhandledExceptionFilterRequestCatch;
		_uiThread.UnhandledException -=
			HandlerOnUnhandledExceptionNotHandled;

		try
		{
			_uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
			_uiThread.RunJobs(null, CancellationToken.None);
		}
		catch (Exception e)
		{
			caughtCorrectException = e.Message == ExpectedExceptionText;
		}
		finally
		{
			Verification(caughtCorrectException, 0, 0);
		}
	}

	[PresentationTestMethod]
	public void CanRethrowExceptionWithUnhandledException()
	{
		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterRequestCatch;

		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionNotHandled;
		var caughtCorrectException = false;
		try
		{
			_uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
			_uiThread.RunJobs(null, CancellationToken.None);
		}
		catch (Exception e)
		{
			caughtCorrectException = e.Message == ExpectedExceptionText;
		}
		finally
		{
			Verification(caughtCorrectException, 1, 1);
		}
	}

	[PresentationTestMethod]
	public void DifferentThreadsAutoSpawnDispatchers()
	{
		var dispatcher = Dispatcher.CurrentDispatcher;
		ThreadRunHelper.RunOnDedicatedThread(() =>
		{
			CornerstoneTest.IsNull(Dispatcher.FromThread(Thread.CurrentThread));
			CornerstoneTest.IsNotNull(Dispatcher.CurrentDispatcher);
			CornerstoneTest.AreNotEqual(dispatcher, Dispatcher.CurrentDispatcher);
			CornerstoneTest.AreEqual(Dispatcher.CurrentDispatcher, Dispatcher.FromThread(Thread.CurrentThread));
		}).GetAwaiter().GetResult();
	}

	[PresentationTestMethod]
	public void DispatcherHandlesExceptionWithPost()
	{
		var handled = false;
		var executed = false;
		_uiThread.UnhandledException += (sender, args) =>
		{
			handled = true;
			args.Handled = true;
		};
		_uiThread.Post(() => ThrowAnException());
		_uiThread.Post(() => executed = true);

		_uiThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.IsTrue(handled);
		CornerstoneTest.IsTrue(executed);
	}

	[PresentationTestMethod]
	public void InvokeAsyncMethodDoesntTriggerUnhandledException()
	{
		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterRequestCatch;

		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionHandled;
		var caughtCorrectException = false;
		try
		{
			// Since both Invoke and InvokeAsync can throw exception, there is no need to pass them to the UnhandledException.
			var op = _uiThread.InvokeAsync(ThrowAnException, DispatcherPriority.Normal, CancellationToken.None);
			op.Wait();
			_uiThread.RunJobs(null, CancellationToken.None);
		}
		catch (Exception e)
		{
			// should be no exception here.
			caughtCorrectException = e.Message == ExpectedExceptionText;
		}
		finally
		{
			Verification(caughtCorrectException, 0, 0);
		}
	}

	[PresentationTestMethod]
	public void InvokeMethodDoesntTriggerUnhandledException()
	{
		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterRequestCatch;

		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionHandled;
		var caughtCorrectException = false;
		try
		{
			// Since both Invoke and InvokeAsync can throw exception, there is no need to pass them to the UnhandledException.
			_uiThread.Invoke(ThrowAnException, DispatcherPriority.Normal, CancellationToken.None);
			_uiThread.RunJobs(null, CancellationToken.None);
		}
		catch (Exception e)
		{
			// should be no exception here.
			caughtCorrectException = e.Message == ExpectedExceptionText;
		}
		finally
		{
			Verification(caughtCorrectException, 0, 0);
		}
	}

	[PresentationTestMethod]
	public void MultipleUnhandledExceptionCannotResetHandleFlag()
	{
		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterRequestCatch;

		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionHandled;
		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionNotHandled;
		var caughtCorrectException = true;

		try
		{
			_uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
			_uiThread.RunJobs(null, CancellationToken.None);
		}
		catch (Exception)
		{
			// should be no exception here.
			caughtCorrectException = false;
		}
		finally
		{
			Verification(caughtCorrectException, 1, 1);
		}
	}

	[PresentationTestMethod]
	public void MultipleUnhandledExceptionFilterCannotResetRequestCatchFlag()
	{
		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterNotRequestCatch;
		_uiThread.UnhandledExceptionFilter +=
			HandlerOnUnhandledExceptionFilterRequestCatch;

		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionNotHandled;
		_uiThread.UnhandledException +=
			HandlerOnUnhandledExceptionHandled;
		var caughtCorrectException = false;
		try
		{
			_uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
			_uiThread.RunJobs(null, CancellationToken.None);
		}
		catch (Exception e)
		{
			caughtCorrectException = e.Message == ExpectedExceptionText;
		}
		finally
		{
			Verification(caughtCorrectException, 0, 2);
		}
	}

	[PresentationTestMethod]
	public void SyncContextExceptionCanBeHandledWithPost()
	{
		var syncContext = _uiThread.GetContextWithPriority(DispatcherPriority.Background);

		var handled = false;
		var executed = false;
		_uiThread.UnhandledException += (sender, args) =>
		{
			handled = true;
			args.Handled = true;
		};

		syncContext.Post(_ => ThrowAnException(), null);
		syncContext.Post(_ => executed = true, null);

		_uiThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.IsTrue(handled);
		CornerstoneTest.IsTrue(executed);
	}

	private void HandlerOnUnhandledExceptionFilterNotRequestCatch(object sender,
		DispatcherUnhandledExceptionFilterEventArgs args)
	{
		args.RequestCatch = false;
		_numberOfHandlerOnUnhandledEventFilterInvoked += 1;

		CornerstoneTest.AreEqual(ExpectedExceptionText, args.Exception.Message);
	}

	private void HandlerOnUnhandledExceptionFilterNotRequestCatchPushFrame(object sender,
		DispatcherUnhandledExceptionFilterEventArgs args)
	{
		HandlerOnUnhandledExceptionFilterNotRequestCatch(sender, args);
		var frame = new DispatcherFrame();
		args.Dispatcher.InvokeAsync(() => frame.Continue = false, DispatcherPriority.Background);
		args.Dispatcher.PushFrame(frame);
	}

	private void HandlerOnUnhandledExceptionFilterRequestCatch(object sender,
		DispatcherUnhandledExceptionFilterEventArgs args)
	{
		args.RequestCatch = true;

		_numberOfHandlerOnUnhandledEventFilterInvoked += 1;
		CornerstoneTest.AreEqual(ExpectedExceptionText, args.Exception.Message);
	}

	private void HandlerOnUnhandledExceptionHandled(object sender, DispatcherUnhandledExceptionEventArgs args)
	{
		CornerstoneTest.AreEqual(ExpectedExceptionText, args.Exception.Message);
		CornerstoneTest.IsFalse(_numberOfHandlerOnUnhandledEventFilterInvoked == 0, "UnhandledExceptionFilter should be invoked before UnhandledException.");

		args.Handled = true;
		_numberOfHandlerOnUnhandledEventInvoked += 1;
	}

	private void HandlerOnUnhandledExceptionHandledPushFrame(object sender, DispatcherUnhandledExceptionEventArgs args)
	{
		CornerstoneTest.AreEqual(ExpectedExceptionText, args.Exception.Message);
		CornerstoneTest.IsFalse(_numberOfHandlerOnUnhandledEventFilterInvoked == 0, "UnhandledExceptionFilter should be invoked before UnhandledException.");

		args.Handled = true;
		_numberOfHandlerOnUnhandledEventInvoked += 1;

		var dispatcher = args.Dispatcher;
		var frame = new DispatcherFrame();
		dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
		dispatcher.PushFrame(frame);
	}

	private void HandlerOnUnhandledExceptionNotHandled(object sender, DispatcherUnhandledExceptionEventArgs args)
	{
		CornerstoneTest.AreEqual(ExpectedExceptionText, args.Exception.Message);
		CornerstoneTest.IsFalse(_numberOfHandlerOnUnhandledEventFilterInvoked == 0, "UnhandledExceptionFilter should be invoked before UnhandledException.");

		args.Handled = false;
		_numberOfHandlerOnUnhandledEventInvoked += 1;
	}

	private void ThrowAnException()
	{
		throw new Exception(ExpectedExceptionText);
	}

	private void Verification(bool caughtCorrectException, int numberOfHandlerOnUnhandledEventShouldInvoke,
		int numberOfHandlerOnUnhandledEventFilterShouldInvoke)
	{
		CornerstoneTest.IsTrue(_numberOfHandlerOnUnhandledEventInvoked >= numberOfHandlerOnUnhandledEventShouldInvoke, "Number of handler invoked on UnhandledException is invalid");

		CornerstoneTest.IsTrue(_numberOfHandlerOnUnhandledEventFilterInvoked >= numberOfHandlerOnUnhandledEventFilterShouldInvoke, "Number of handler invoked on UnhandledExceptionFilter is invalid");

		CornerstoneTest.IsTrue(caughtCorrectException, "Wrong exception caught.");
	}

	private void VerifyDispatcherSanity()
	{
		// Verify that we are in a clear-ish state. Do this for every test to ensure that our reset procedure is working
		CornerstoneTest.IsNull(Dispatcher.FromThread(Thread.CurrentThread));
		CornerstoneTest.IsNull(Dispatcher.TryGetUIThread());

		// The first (this) dispatcher becomes UI thread one
		CornerstoneTest.IsNotNull(Dispatcher.CurrentDispatcher);
		CornerstoneTest.AreEqual(Dispatcher.TryGetUIThread(), Dispatcher.CurrentDispatcher);
		CornerstoneTest.AreEqual(Dispatcher.UIThread, Dispatcher.CurrentDispatcher);

		// Dispatcher.FromThread works
		CornerstoneTest.AreEqual(Dispatcher.CurrentDispatcher, Dispatcher.FromThread(Thread.CurrentThread));
		CornerstoneTest.AreEqual(Dispatcher.UIThread, Dispatcher.FromThread(Thread.CurrentThread));
	}

	#endregion
}