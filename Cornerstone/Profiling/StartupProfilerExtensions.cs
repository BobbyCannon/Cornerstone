#region References

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

	#endregion
}