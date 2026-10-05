#region References

using System;
using System.Linq;
using System.Reflection;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Threading;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public sealed class VerifyEmptyDispatcherAfterTestAttribute : BeforeAfterTestAttribute
{
	#region Methods

	public override void After(MethodInfo methodUnderTest)
	{
		if (typeof(ScopedTestBase).IsAssignableFrom(methodUnderTest.DeclaringType))
		{
			return;
		}

		var dispatcher = Dispatcher.UIThread;
		var jobs = dispatcher.GetJobs();
		if (jobs.Count == 0)
		{
			return;
		}

		dispatcher.ClearJobs();

		// Ignore the Control.Loaded callback. It might happen synchronously or might be posted.
		if ((jobs.Count == 1) && IsLoadedCallback(jobs[0]))
		{
			return;
		}

		CornerstoneTest.Fail($"The test left {jobs.Count} unprocessed dispatcher {(jobs.Count == 1 ? "job" : "jobs")}:\n" +
			$"{string.Join(Environment.NewLine, jobs.Select(job => $"  - {job.DebugDisplay}"))}\n" +
			$"Consider using ScopedTestBase or UnitTestApplication.Start().");

		static bool IsLoadedCallback(DispatcherOperation job)
		{
			return (job.Priority == DispatcherPriority.Loaded) &&
				((job.Callback as Delegate)?.Method.DeclaringType?.DeclaringType == typeof(Control));
		}
	}

	#endregion
}