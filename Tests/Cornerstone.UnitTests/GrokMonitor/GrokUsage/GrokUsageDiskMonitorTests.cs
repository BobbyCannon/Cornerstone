#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cornerstone.Extensions;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.GrokMonitor.GrokUsage.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor.GrokUsage;

[TestClass]
public class GrokUsageDiskMonitorTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void NotifyChangedLeadingEdgeInvokesCallback()
	{
		var hits = new List<Guid>();
		var homeId = Guid.NewGuid();
		using var monitor = new GrokUsageDiskMonitor(
			id =>
			{
				lock (hits)
				{
					hits.Add(id);
				}
			},
			TimeSpan.FromMilliseconds(80));

		using var fixture = new TempHome();
		monitor.SyncHomes([(homeId, fixture.Root)]);
		AreEqual(1, monitor.TrackedHomeCount);

		monitor.NotifyChanged(homeId);

		lock (hits)
		{
			AreEqual(1, hits.Count);
			AreEqual(homeId, hits[0]);
		}
	}

	[TestMethod]
	public void NotifyChangedDuringCooldownSchedulesTrailingEdge()
	{
		var count = 0;
		var homeId = Guid.NewGuid();
		using var monitor = new GrokUsageDiskMonitor(
			_ => Interlocked.Increment(ref count),
			TimeSpan.FromMilliseconds(80));

		using var fixture = new TempHome();
		monitor.SyncHomes([(homeId, fixture.Root)]);

		monitor.NotifyChanged(homeId);
		AreEqual(1, count);

		// Burst during cooldown — one trailing run.
		monitor.NotifyChanged(homeId);
		monitor.NotifyChanged(homeId);
		AreEqual(1, count);

		var completed = this.WaitUntil(_ => Volatile.Read(ref count) >= 2, 1000, 5);
		IsTrue(completed);
		AreEqual(2, count);
	}

	[TestMethod]
	public void SyncHomesRemovesWatchersForDroppedHomes()
	{
		var homeA = Guid.NewGuid();
		var homeB = Guid.NewGuid();
		using var monitor = new GrokUsageDiskMonitor(_ => { }, TimeSpan.FromMilliseconds(50));
		using var fixtureA = new TempHome();
		using var fixtureB = new TempHome();

		monitor.SyncHomes([(homeA, fixtureA.Root), (homeB, fixtureB.Root)]);
		AreEqual(2, monitor.TrackedHomeCount);

		monitor.SyncHomes([(homeA, fixtureA.Root)]);
		AreEqual(1, monitor.TrackedHomeCount);
	}

	[TestMethod]
	public void DisposeStopsFurtherNotifications()
	{
		var count = 0;
		var homeId = Guid.NewGuid();
		var monitor = new GrokUsageDiskMonitor(_ => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(50));
		using var fixture = new TempHome();
		monitor.SyncHomes([(homeId, fixture.Root)]);

		monitor.NotifyChanged(homeId);
		AreEqual(1, count);

		monitor.Dispose();
		monitor.NotifyChanged(homeId);
		Thread.Sleep(100);
		AreEqual(1, count);
	}

	[TestMethod]
	public void FileWriteTriggersThrottledCallback()
	{
		var count = 0;
		var homeId = Guid.NewGuid();
		using var monitor = new GrokUsageDiskMonitor(
			_ => Interlocked.Increment(ref count),
			TimeSpan.FromMilliseconds(100));
		using var fixture = new TempHome();
		monitor.SyncHomes([(homeId, fixture.Root)]);

		var logPath = Path.Combine(fixture.Root, "logs", "unified.jsonl");
		File.AppendAllText(logPath, "{}\n");

		var completed = this.WaitUntil(_ => Volatile.Read(ref count) >= 1, 3000, 10);
		IsTrue(completed);
	}

	#endregion

	#region Classes

	private sealed class TempHome : IDisposable
	{
		public TempHome()
		{
			Root = Path.Combine(Path.GetTempPath(), "GrokDiskMon_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Root);
			Directory.CreateDirectory(Path.Combine(Root, "logs"));
			Directory.CreateDirectory(Path.Combine(Root, "sessions"));
			File.WriteAllText(Path.Combine(Root, "logs", "unified.jsonl"), string.Empty);
		}

		public string Root { get; }

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(Root))
				{
					Directory.Delete(Root, true);
				}
			}
			catch
			{
				// best-effort cleanup
			}
		}
	}

	#endregion
}
