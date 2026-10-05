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
	public void AccumulateMergesExclusiveTimeByNameUnderCurrentScope()
	{
		var profiler = new StartupProfiler(this);

		using (profiler.Start("Parent"))
		{
			using (profiler.Accumulate("ApplyStyling"))
			{
				IncrementTime(milliseconds: 10);
			}
			using (profiler.Accumulate("ApplyTemplate"))
			{
				IncrementTime(milliseconds: 5);
			}
			using (profiler.Accumulate("ApplyStyling"))
			{
				IncrementTime(milliseconds: 7);
			}
		}

		profiler.Complete();

		AreEqual(1, profiler.Samples.Count);
		AreEqual("Parent", profiler.Samples[0].Name);
		AreEqual(2, profiler.Samples[0].Children.Count);
		AreEqual("ApplyStyling", profiler.Samples[0].Children[0].Name);
		AreEqual(TimeSpan.FromMilliseconds(17), profiler.Samples[0].Children[0].Elapsed);
		AreEqual("ApplyTemplate", profiler.Samples[0].Children[1].Name);
		AreEqual(TimeSpan.FromMilliseconds(5), profiler.Samples[0].Children[1].Elapsed);
	}

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
	public void EmptyScopeThenNestedSiblingRecordsBoth()
	{
		var profiler = new StartupProfiler(this);

		using (profiler.Start($"Built on {DateTime.UtcNow:O}"))
		{
			// no op
		}

		using (profiler.Start("AppBootstrap.Initialize"))
		{
			IncrementTime(milliseconds: 10);
			using (profiler.Start("RuntimeInformation"))
			{
				IncrementTime(milliseconds: 20);
			}
		}

		profiler.Complete();

		AreEqual(2, profiler.Samples.Count);
		IsTrue(profiler.Samples[0].Name.StartsWith("Built on"));
		AreEqual("AppBootstrap.Initialize", profiler.Samples[1].Name);
		AreEqual(1, profiler.Samples[1].Children.Count);
		AreEqual("RuntimeInformation", profiler.Samples[1].Children[0].Name);
	}

	[TestMethod]
	public void MarkAndRecordMarkNamesTheGap()
	{
		var profiler = new StartupProfiler(this);
		profiler.Time("First", () => IncrementTime(milliseconds: 10));
		profiler.Mark("Between");
		IncrementTime(milliseconds: 55);
		profiler.RecordMark();
		profiler.Complete();

		AreEqual("Between", profiler.Samples[1].Name);
		AreEqual(TimeSpan.FromMilliseconds(55), profiler.Samples[1].Elapsed);
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
	public void RecordNamesAGapWithoutAnOpenScope()
	{
		var profiler = new StartupProfiler(this);
		var start = profiler.GetTicks();
		IncrementTime(milliseconds: 40);
		profiler.Record("Gap", start);
		profiler.Complete();

		AreEqual("Gap", profiler.Samples[0].Name);
		AreEqual(TimeSpan.FromMilliseconds(40), profiler.Samples[0].Elapsed);
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
		IsTrue(report.Contains(" at "));
		IsTrue(report.Contains("Outer"));
		IsTrue(report.Contains("Inner"));
		IsFalse(profiler.IsCompleted);

		_ = profiler.ToString();
		IsFalse(profiler.IsCompleted);
	}

	[TestMethod]
	public void ToReportDoesNotCompleteAndLaterScopesStillRecord()
	{
		var profiler = new StartupProfiler(this);
		profiler.Time("First", () => IncrementTime(milliseconds: 10));

		_ = profiler.ToReport();
		_ = profiler.ToString();
		IsFalse(profiler.IsCompleted);

		profiler.Time("Second", () => IncrementTime(milliseconds: 15));
		profiler.Complete();

		AreEqual(2, profiler.Samples.Count);
		AreEqual("First", profiler.Samples[0].Name);
		AreEqual("Second", profiler.Samples[1].Name);
	}

	[TestMethod]
	public void ToReportDrawsTreeCorners()
	{
		var profiler = new StartupProfiler(this);

		using (profiler.Start("Outer"))
		{
			IncrementTime(milliseconds: 5);
			profiler.Time("Inner", () => IncrementTime(milliseconds: 10));
			profiler.Time("Leaf", () => IncrementTime(milliseconds: 20));
		}

		profiler.Time("Sibling", () => IncrementTime(milliseconds: 8));
		profiler.Complete();

		var report = profiler.ToReport();
		var lines = report.Replace("\r\n", "\n").Split('\n');
		IsTrue(lines[0].StartsWith(StartupProfiler.RootName));
		AreEqual("├── Outer 35.0 ms  (81.4%)", lines[1]);
		AreEqual("│   ├── Inner 10.0 ms  (28.6%)", lines[2]);
		AreEqual("│   └── Leaf 20.0 ms  (57.1%)", lines[3]);
		AreEqual("└── Sibling 8.0 ms  (18.6%)", lines[4]);
	}

	[TestMethod]
	public void ToReportOmitsFastLeavesAndKeepsAncestors()
	{
		var profiler = new StartupProfiler(this);

		using (profiler.Start("Outer"))
		{
			profiler.Time("Tiny", () => IncrementTime(milliseconds: 5));
			profiler.Time("Warm", () => IncrementTime(milliseconds: 40));
			profiler.Time("Hot", () => IncrementTime(milliseconds: 150));
		}

		profiler.Complete();

		var slow = profiler.ToReport(StartupProfileDetail.Slow);
		IsTrue(slow.Contains("Outer"));
		IsTrue(slow.Contains("Warm"));
		IsTrue(slow.Contains("Hot"));
		IsFalse(slow.Contains("Tiny"));

		var slowest = profiler.ToReport(StartupProfileDetail.Slowest);
		IsTrue(slowest.Contains(StartupProfiler.RootName));
		IsTrue(slowest.Contains("Outer"));
		IsTrue(slowest.Contains("Hot"));
		IsFalse(slowest.Contains("Warm"));
		IsFalse(slowest.Contains("Tiny"));
	}

	#endregion
}