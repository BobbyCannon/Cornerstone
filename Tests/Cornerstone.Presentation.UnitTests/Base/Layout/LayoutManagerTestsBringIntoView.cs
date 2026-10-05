#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class LayoutManagerTestsBringIntoView : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void LayoutInvalidatedByRequestConvergesWithinSamePass()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };
		root.LayoutManager.ExecuteInitialLayoutPass();

		control.Measured = control.Arranged = false;

		var request = new TestRequest(control) { OnExecute = control.InvalidateMeasure };
		GetLayoutManager(root).EnqueueBringIntoView(request);

		root.LayoutManager.ExecuteLayoutPass();

		// The layout invalidated by the request (e.g. a scroll offset change) has been
		// re-run before ExecuteLayoutPass returned, so the frame is rendered fully scrolled.
		CornerstoneTest.AreEqual(1, request.Executions);
		CornerstoneTest.IsTrue(control.Measured);
		CornerstoneTest.IsTrue(control.Arranged);
	}

	[PresentationTestMethod]
	public void LayoutUpdatedIsNotRaisedUntilAllRequestsHaveBeenProcessed()
	{
		var first = new LayoutTestControl();
		var second = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = new StackPanel { Children = { first, second } } };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var layoutManager = GetLayoutManager(root);
		var layoutUpdatedRaised = false;
		var secondExecutedBeforeLayoutUpdated = false;
		root.LayoutManager.LayoutUpdated += (_, _) => layoutUpdatedRaised = true;

		// The second request can only execute once the layout invalidated by the first one has
		// been re-run, so it is executed by a following pass of the processing loop.
		first.Measured = false;

		var secondRequest = new TestRequest(second)
		{
			CanExecute = () => first.Measured,
			OnExecute = () => secondExecutedBeforeLayoutUpdated = !layoutUpdatedRaised
		};

		var firstRequest = new TestRequest(first) { OnExecute = first.InvalidateMeasure };

		layoutManager.EnqueueBringIntoView(firstRequest);
		layoutManager.EnqueueBringIntoView(secondRequest);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, secondRequest.Executions);
		CornerstoneTest.IsTrue(secondExecutedBeforeLayoutUpdated);
	}

	[PresentationTestMethod]
	public void LayoutUpdatedIsRaisedOnceWhenRequestInvalidatesLayout()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var layoutUpdated = 0;
		root.LayoutManager.LayoutUpdated += (_, _) => ++layoutUpdated;

		var request = new TestRequest(control) { OnExecute = control.InvalidateMeasure };
		GetLayoutManager(root).EnqueueBringIntoView(request);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, request.Executions);
		CornerstoneTest.AreEqual(1, layoutUpdated);
	}

	[PresentationTestMethod]
	public void RequestEnqueuedWhileProcessingIsAttemptedByTheSamePass()
	{
		var first = new LayoutTestControl();
		var second = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = new StackPanel { Children = { first, second } } };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var layoutManager = GetLayoutManager(root);
		var canExecuteSecond = false;
		var secondRequest = new TestRequest(second) { CanExecute = () => canExecuteSecond };

		// Executing a request can enqueue another one (as ControlExtensions.BringIntoViewCore does):
		// the new request must be attempted by the same pass, and retained if it can't execute yet.
		var firstRequest = new TestRequest(first)
		{
			OnExecute = () =>
			{
				layoutManager.EnqueueBringIntoView(secondRequest);
				first.InvalidateMeasure();
			}
		};

		layoutManager.EnqueueBringIntoView(firstRequest);
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, firstRequest.Executions);

		// Attempted first during the same pass as the first request, then retried once after a new layout pass.
		CornerstoneTest.AreEqual(2, secondRequest.ExecuteAttempts);
		CornerstoneTest.AreEqual(0, secondRequest.Executions);

		// The second request couldn't execute despite having been through an extra layout pass.
		// We can't retry forever (nothing has changed). The request will be retried again on the next "natural" pass.
		canExecuteSecond = true;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(3, secondRequest.ExecuteAttempts);
		CornerstoneTest.AreEqual(1, secondRequest.Executions);
	}

	[PresentationTestMethod]
	public void RequestIsDroppedWhenTargetIsDetached()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var request = new TestRequest(control);
		GetLayoutManager(root).EnqueueBringIntoView(request);

		root.Child = null;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(0, request.ExecuteAttempts);

		root.Child = control;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(0, request.ExecuteAttempts);
	}

	[PresentationTestMethod]
	public void RequestIsExecutedAtEndOfLayoutPass()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var request = new TestRequest(control);
		GetLayoutManager(root).EnqueueBringIntoView(request);

		CornerstoneTest.AreEqual(0, request.ExecuteAttempts);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, request.ExecuteAttempts);
		CornerstoneTest.AreEqual(1, request.Executions);

		root.LayoutManager.ExecuteLayoutPass();

		// Should not have been executed twice.
		CornerstoneTest.AreEqual(1, request.ExecuteAttempts);
		CornerstoneTest.AreEqual(1, request.Executions);
	}

	[PresentationTestMethod]
	public void RequestIsExecutedBeforeLayoutUpdatedIsRaised()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var layoutUpdatedRaised = false;
		var executedBeforeLayoutUpdated = false;
		root.LayoutManager.LayoutUpdated += (_, _) => layoutUpdatedRaised = true;

		var request = new TestRequest(control)
		{
			OnExecute = () => executedBeforeLayoutUpdated = !layoutUpdatedRaised
		};

		GetLayoutManager(root).EnqueueBringIntoView(request);
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, request.Executions);
		CornerstoneTest.IsTrue(executedBeforeLayoutUpdated);
	}

	[PresentationTestMethod]
	public void RequestIsRetainedUntilItCanExecute()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var canExecute = false;
		var request = new TestRequest(control) { CanExecute = () => canExecute };
		GetLayoutManager(root).EnqueueBringIntoView(request);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, request.ExecuteAttempts);
		CornerstoneTest.AreEqual(0, request.Executions);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(2, request.ExecuteAttempts);
		CornerstoneTest.AreEqual(0, request.Executions);

		canExecute = true;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(3, request.ExecuteAttempts);
		CornerstoneTest.AreEqual(1, request.Executions);
	}

	[PresentationTestMethod]
	public void RequestsAreCoalescedByTarget()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var first = new TestRequest(control);
		var second = new TestRequest(control);
		var layoutManager = GetLayoutManager(root);

		layoutManager.EnqueueBringIntoView(first);
		layoutManager.EnqueueBringIntoView(second);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(0, first.ExecuteAttempts);
		CornerstoneTest.AreEqual(1, second.Executions);
	}

	private static LayoutManager GetLayoutManager(TestRoot root)
	{
		return CornerstoneTest.IsType<LayoutManager>(root.LayoutManager, false);
	}

	#endregion

	#region Classes

	private sealed class TestRequest(Layoutable target) : BringIntoViewRequest(target)
	{
		#region Properties

		public Func<bool> CanExecute { get; init; }
		public int ExecuteAttempts { get; private set; }
		public int Executions { get; private set; }
		public Action OnExecute { get; init; }

		#endregion

		#region Methods

		public override bool TryExecute()
		{
			++ExecuteAttempts;

			var canExecute = CanExecute?.Invoke() ?? true;
			if (!canExecute)
			{
				return false;
			}

			++Executions;
			OnExecute?.Invoke();
			return true;
		}

		#endregion
	}

	#endregion
}