#region References

using System;
using System.Collections.Generic;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncResultsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void PropertyDefaultsThenSet()
	{
		var scenarios = new Dictionary<SyncResultStatus, Action<SyncResults>>
		{
			{ SyncResultStatus.Cancelled, x => x.SyncCancelled = true },
			{ SyncResultStatus.Completed, x => x.SyncCompleted = true },
			{ SyncResultStatus.Started, x => x.SyncStarted = true },
			{ SyncResultStatus.Successful, x => x.SyncSuccessful = true }
		};

		foreach (var scenario in scenarios)
		{
			var actual = new SyncResults();
			AreEqual(false, actual.SyncCancelled);
			AreEqual(false, actual.SyncStatus.HasFlag(SyncResultStatus.Cancelled));
			AreEqual(false, actual.SyncCompleted);
			AreEqual(false, actual.SyncStatus.HasFlag(SyncResultStatus.Completed));
			AreEqual(false, actual.SyncStarted);
			AreEqual(false, actual.SyncStatus.HasFlag(SyncResultStatus.Started));
			AreEqual(false, actual.SyncSuccessful);
			AreEqual(false, actual.SyncStatus.HasFlag(SyncResultStatus.Successful));
			AreEqual(null, actual.SyncType);

			scenario.Value(actual);

			AreEqual(scenario.Key == SyncResultStatus.Cancelled, actual.SyncCancelled);
			AreEqual(scenario.Key == SyncResultStatus.Cancelled, actual.SyncStatus.HasFlag(SyncResultStatus.Cancelled));
			AreEqual(scenario.Key == SyncResultStatus.Completed, actual.SyncCompleted);
			AreEqual(scenario.Key == SyncResultStatus.Completed, actual.SyncStatus.HasFlag(SyncResultStatus.Completed));
			AreEqual(scenario.Key == SyncResultStatus.Started, actual.SyncStarted);
			AreEqual(scenario.Key == SyncResultStatus.Started, actual.SyncStatus.HasFlag(SyncResultStatus.Started));
			AreEqual(scenario.Key == SyncResultStatus.Successful, actual.SyncSuccessful);
			AreEqual(scenario.Key == SyncResultStatus.Successful, actual.SyncStatus.HasFlag(SyncResultStatus.Successful));
		}
	}

	#endregion
}