#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class ThreadingTests
{
	#region Methods

	[HeadlessTestMethod]
	[DataRow(1)]
	[DataRow(10)]
	[DataRow(100)]
	public async Task DispatcherTimerWorksOnTheSameThread(int interval)
	{
		AssertHelper.NotNull(SynchronizationContext.Current);
		var currentThread = Thread.CurrentThread;

		await Task.Delay(100);

		AssertHelper.Same(currentThread, Thread.CurrentThread);

		var tcs = new TaskCompletionSource();

		DispatcherTimer.RunOnce(() =>
		{
			try
			{
				AssertHelper.Same(currentThread, Thread.CurrentThread);
				tcs.SetResult();
			}
			catch (Exception ex)
			{
				tcs.SetException(ex);
			}
		}, TimeSpan.FromTicks(interval));

		await tcs.Task;
	}

	[HeadlessTestMethod]
	public void ShouldBeOnDispatcherThread()
	{
		Dispatcher.UIThread.VerifyAccess();
	}

	[HeadlessTestMethod]
	public void ShouldFailTestOnDelayedPostWhenFlushDispatcher()
	{
		CornerstoneTest.ExpectedException<InvalidOperationException>(() =>
		{
			Dispatcher.UIThread.Post(() => throw new InvalidOperationException(), DispatcherPriority.Default);
			Dispatcher.UIThread.RunJobs();
		});
	}

	#endregion
}