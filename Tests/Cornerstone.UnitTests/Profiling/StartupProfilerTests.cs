#region References

using System;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class StartupProfilerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CompleteClosesOpenScopes()
	{
		var profiler = new StartupProfiler(this);

		// Intentionally not disposed
		_ = profiler.Start("Leaked");
		IncrementTime(milliseconds: 30);
		profiler.Complete();

		AreEqual(1, profiler.Samples.Count);
		AreEqual("Leaked", profiler.Samples[0].Name);
		AreEqual(TimeSpan.FromMilliseconds(30), profiler.Samples[0].Elapsed);
	}

	[TestMethod]
	public void CompleteIsIdempotentAndAddsUnknownResidual()
	{
		var profiler = new StartupProfiler(this);

		profiler.Time("Work", () => IncrementTime(milliseconds: 100));
		IncrementTime(milliseconds: 50);

		profiler.Complete();
		var firstRoot = profiler.Root;
		profiler.Complete();

		IsTrue(profiler.IsCompleted);
		AreEqual(firstRoot, profiler.Root);
		AreEqual(2, profiler.Samples.Count);
		AreEqual("Work", profiler.Samples[0].Name);
		AreEqual(TimeSpan.FromMilliseconds(100), profiler.Samples[0].Elapsed);
		AreEqual(StartupProfiler.UnknownName, profiler.Samples[1].Name);
		AreEqual(TimeSpan.FromMilliseconds(50), profiler.Samples[1].Elapsed);
		AreEqual(TimeSpan.FromMilliseconds(150), profiler.Root.Elapsed);
	}

	[TestMethod]
	public void NestedScopesBuildTreeWithDepthAndOffset()
	{
		var profiler = new StartupProfiler(this);

		using (profiler.Start("Parent"))
		{
			IncrementTime(milliseconds: 10);
			using (profiler.Start("Child"))
			{
				IncrementTime(milliseconds: 40);
			}
			IncrementTime(milliseconds: 20);
		}

		profiler.Complete();

		AreEqual(1, profiler.Samples.Count);
		var parent = profiler.Samples[0];
		AreEqual("Parent", parent.Name);
		AreEqual(0, parent.Depth);
		AreEqual(TimeSpan.Zero, parent.Offset);
		AreEqual(TimeSpan.FromMilliseconds(70), parent.Elapsed);
		AreEqual(1, parent.Children.Count);

		var child = parent.Children[0];
		AreEqual("Child", child.Name);
		AreEqual(1, child.Depth);
		AreEqual(TimeSpan.FromMilliseconds(10), child.Offset);
		AreEqual(TimeSpan.FromMilliseconds(40), child.Elapsed);
	}

	[TestMethod]
	public void NullProfilerStartIsNoOp()
	{
		StartupProfiler profiler = null;
		using (profiler.Start("Anything"))
		{
			// should not throw
		}

		IsTrue(true);
	}

	[TestMethod]
	public void StartAfterCompleteIsNoOp()
	{
		var profiler = new StartupProfiler(this);
		profiler.Time("A", () => IncrementTime(milliseconds: 5));
		profiler.Complete();

		using (profiler.Start("Late"))
		{
			IncrementTime(milliseconds: 100);
		}

		AreEqual(1, profiler.Samples.Count);
		AreEqual("A", profiler.Samples[0].Name);
	}

	[TestMethod]
	public void TimeRecordsActionAndFuncResults()
	{
		var profiler = new StartupProfiler(this);

		profiler.Time("Action", () => IncrementTime(milliseconds: 25));
		var value = profiler.Time("Func", () =>
		{
			IncrementTime(milliseconds: 15);
			return 42;
		});

		AreEqual(42, value);
		profiler.Complete();

		// No residual when scopes cover full wall
		AreEqual(2, profiler.Samples.Count);
		AreEqual(TimeSpan.FromMilliseconds(25), profiler.Samples[0].Elapsed);
		AreEqual(TimeSpan.FromMilliseconds(15), profiler.Samples[1].Elapsed);
		AreEqual(TimeSpan.FromMilliseconds(40), profiler.Root.Elapsed);
	}

	[TestMethod]
	public void ToReportContainsHierarchy()
	{
		var profiler = new StartupProfiler(this);

		using (profiler.Start("Outer"))
		{
			IncrementTime(milliseconds: 5);
			profiler.Time("Inner", () => IncrementTime(milliseconds: 10));
		}

		var report = profiler.ToReport();
		IsTrue(report.Contains(StartupProfiler.RootName));
		IsTrue(report.Contains("Outer"));
		IsTrue(report.Contains("Inner"));
		IsTrue(profiler.IsCompleted);
	}

	#endregion
}