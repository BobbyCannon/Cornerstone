#region References

using System;
using Cornerstone.Data;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncClientProfilerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddSumsElapsed()
	{
		var first = new SyncClientProfiler("A");
		first.GetChanges.Add(TimeSpan.FromMilliseconds(10));
		var second = new SyncClientProfiler("B");
		second.GetChanges.Add(TimeSpan.FromMilliseconds(15));
		first.Add(second);
		AreEqual(TimeSpan.FromMilliseconds(25), first.GetChanges.Elapsed);
	}

	[TestMethod]
	public void ToStringIncludesNameAndPercents()
	{
		var profiler = new SyncClientProfiler("Client");
		profiler.GetChanges.Add(TimeSpan.FromMilliseconds(25));
		var text = profiler.ToString(TimeSpan.FromMilliseconds(100));
		IsTrue(text.Contains("Client"));
		IsTrue(text.Contains("GetChanges"));
		IsTrue(text.Contains("%"));
	}

	[TestMethod]
	public void UpdateWithCopiesTimers()
	{
		var source = new SyncClientProfiler("Source");
		source.ApplyChanges.Add(TimeSpan.FromMilliseconds(40));
		var destination = new SyncClientProfiler("Destination");
		IsTrue(destination.UpdateWith(source, IncludeExcludeSettings.Empty));
		destination.ApplyChanges.Add(source.ApplyChanges);
		AreEqual(TimeSpan.FromMilliseconds(40), destination.ApplyChanges.Elapsed);
	}

	#endregion
}