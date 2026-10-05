#region References

using System;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class LoadedTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ControlLoadsAndUnloads()
	{
		// Some other tests are populating the queue and are not resetting the dispatcher, so we need to purge it
		Control.ResetLoadedQueueForUnitTests();
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			int loadedCount = 0, unloadedCount = 0;
			var window = new Window();
			window.Show();

			var target = new Button();

			target.Loaded += (_, _) => loadedCount++;
			target.Unloaded += (_, _) => unloadedCount++;

			CornerstoneTest.AreEqual(0, loadedCount);
			CornerstoneTest.AreEqual(0, unloadedCount);

			window.Content = target;
			Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);
			CornerstoneTest.IsTrue(target.IsLoaded);

			CornerstoneTest.AreEqual(1, loadedCount);
			CornerstoneTest.AreEqual(0, unloadedCount);

			window.Content = null;

			CornerstoneTest.AreEqual(1, loadedCount);
			CornerstoneTest.AreEqual(1, unloadedCount);
			CornerstoneTest.IsFalse(target.IsLoaded);
		}
	}

	[PresentationTestMethod]
	public void LoadedExceptionDoesNotPreventOtherControlsFromLoading()
	{
		// Some other tests are populating the queue and are not resetting the dispatcher, so we need to purge it
		Control.ResetLoadedQueueForUnitTests();
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			window.Show();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

			// Batch 1: a control whose Loaded handler throws, plus siblings queued in the same batch.
			var throwing = new Button();
			throwing.Loaded += (_, _) => throw new InvalidOperationException("Loaded handler failure");

			var sibling1 = new Button();
			var sibling2 = new Button();
			int sibling1LoadedCount = 0, sibling2LoadedCount = 0;
			sibling1.Loaded += (_, _) => sibling1LoadedCount++;
			sibling2.Loaded += (_, _) => sibling2LoadedCount++;

			window.Content = new StackPanel { Children = { sibling1, throwing, sibling2 } };

			// The exception must surface (propagate to the dispatcher), not be swallowed.
			PumpLoadedJobs(1);

			// Both siblings from the same batch must still have been loaded.
			CornerstoneTest.IsTrue(sibling1.IsLoaded);
			CornerstoneTest.IsTrue(sibling2.IsLoaded);
			CornerstoneTest.AreEqual(1, sibling1LoadedCount);
			CornerstoneTest.AreEqual(1, sibling2LoadedCount);

			// Batch 2: controls loaded afterwards must still receive Loaded.
			var later = new Button();
			var laterLoadedCount = 0;
			later.Loaded += (_, _) => laterLoadedCount++;
			((StackPanel) window.Content!).Children.Add(later);

			PumpLoadedJobs(0);

			CornerstoneTest.IsTrue(later.IsLoaded);
			CornerstoneTest.AreEqual(1, laterLoadedCount);
		}
	}

	[PresentationTestMethod]
	public void LoadedShouldNotBeRaisedIfDetachedFromVisualTree()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var loadedCount = 0;
		var unloadedCount = 0;
		var window = new Window();
		window.Show();

		var target = new Button();

		target.Loaded += (_, _) => loadedCount++;
		target.Unloaded += (_, _) => unloadedCount++;

		CornerstoneTest.AreEqual(0, loadedCount);
		CornerstoneTest.AreEqual(0, unloadedCount);

		// Attach to, then immediately detach from the visual tree.
		window.Content = target;
		window.Content = null;

		// Attach to another logical parent (this can actually happen outside tests with overlay popups)
		((ISetLogicalParent) target).SetParent(new Window());

		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		// At this point, the control shouldn't have been loaded at all.
		CornerstoneTest.IsNull(target.VisualParent);
		CornerstoneTest.IsFalse(target.IsLoaded);
		CornerstoneTest.AreEqual(0, loadedCount);
		CornerstoneTest.AreEqual(0, unloadedCount);
	}

	[PresentationTestMethod]
	public void WindowLoadsAndUnloads()
	{
		// Some other tests are populating the queue and are not resetting the dispatcher, so we need to purge it
		Control.ResetLoadedQueueForUnitTests();
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			int loadedCount = 0, unloadedCount = 0;
			var target = new Window();

			target.Loaded += (_, _) => loadedCount++;
			target.Unloaded += (_, _) => unloadedCount++;

			CornerstoneTest.AreEqual(0, loadedCount);
			CornerstoneTest.AreEqual(0, unloadedCount);

			target.Show();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);
			CornerstoneTest.IsTrue(target.IsLoaded);

			CornerstoneTest.AreEqual(1, loadedCount);
			CornerstoneTest.AreEqual(0, unloadedCount);

			target.Close();

			CornerstoneTest.AreEqual(1, loadedCount);
			CornerstoneTest.AreEqual(1, unloadedCount);
			CornerstoneTest.IsFalse(target.IsLoaded);
		}
	}

	private static void PumpLoadedJobs(int swallowedExceptionsAllowed)
	{
		// A throwing Loaded handler propagates its exception out of the dispatcher job.
		// Keep pumping so that any rescheduled loaded-processing jobs also run, but fail
		// if more exceptions escape than the single expected one.
		for (var i = 0; i <= swallowedExceptionsAllowed; i++)
		{
			try
			{
				Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);
				return;
			}
			catch (InvalidOperationException) when (i < swallowedExceptionsAllowed)
			{
				// Expected exception from the throwing Loaded handler; continue pumping.
			}
		}
	}

	#endregion
}