#region References

using System;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Profiling;

public static class StartupProfilerExtensions
{
	#region Methods

	/// <summary>
	/// Start a nested startup scope. Returns a no-op scope when <paramref name="profiler" /> is null.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StartupScope Start(this StartupProfiler profiler, string name)
	{
		return profiler != null
			? profiler.BeginScope(name)
			: default;
	}

	public static void Record(this StartupProfiler profiler, string name, long startTicks)
	{
		profiler?.Record(name, startTicks);
	}

	/// <summary>
	/// Sum exclusive time into one child of the current <see cref="Start" /> scope, merged by name.
	/// Use for hot loops (first measure styling/templates) instead of a nested sample per call.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StartupScope Accumulate(this StartupProfiler profiler, string name)
	{
		return profiler != null
			? profiler.BeginAccumulate(name)
			: default;
	}

	public static void Mark(this StartupProfiler profiler, string name)
	{
		profiler?.Mark(name);
	}

	public static void RecordMark(this StartupProfiler profiler)
	{
		profiler?.RecordMark();
	}

	#endregion
}