#region References

using System.Linq;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Keystone.Lifecycle;

[TestClass]
public class LifecycleTrackerStartupProfilerTests : CornerstoneUnitTest
{
	#region Methods

	[TestCleanup]
	public void Cleanup()
	{
		AppBootstrap.StartupProfiler = null;
	}

	[TestMethod]
	public void InitializeLoadStartRecordChildScopesWhenProfilerSet()
	{
		var profiler = new StartupProfiler(this);
		AppBootstrap.StartupProfiler = profiler;

		var tracker = new LifecycleTracker();
		var childA = new NamedLifecycle("ServiceA");
		var childB = new NamedLifecycle("ServiceB");
		tracker.Track(childA);
		tracker.Track(childB);

		tracker.InitializeLifecycle();
		IncrementTime(milliseconds: 1);
		tracker.LoadLifecycle();
		IncrementTime(milliseconds: 1);
		tracker.StartLifecycle();

		profiler.Complete();

		var names = FlattenNames(profiler.Root).ToArray();
		IsTrue(names.Any(n => n.EndsWith(".Initialize")));
		IsTrue(names.Any(n => n.EndsWith(".Load")));
		IsTrue(names.Any(n => n.EndsWith(".Start")));
		IsTrue(names.Contains(nameof(NamedLifecycle)));
	}

	[TestMethod]
	public void LifecycleWithoutProfilerDoesNotThrow()
	{
		AppBootstrap.StartupProfiler = null;
		var tracker = new LifecycleTracker();
		tracker.Track(new NamedLifecycle("Only"));
		tracker.InitializeLifecycle();
		tracker.LoadLifecycle();
		tracker.StartLifecycle();
		IsTrue(tracker.IsLifecycleStarted());
	}

	#endregion

	#region Methods (helpers)

	private static System.Collections.Generic.IEnumerable<string> FlattenNames(StartupSample sample)
	{
		if (sample == null)
		{
			yield break;
		}

		yield return sample.Name;
		foreach (var child in sample.Children)
		{
			foreach (var name in FlattenNames(child))
			{
				yield return name;
			}
		}
	}

	#endregion

	#region Nested Types

	private sealed class NamedLifecycle : CornerstoneObject
	{
		public NamedLifecycle(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}

	#endregion
}