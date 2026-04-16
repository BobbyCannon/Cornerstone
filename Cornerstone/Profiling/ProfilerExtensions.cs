#region References

using System.Runtime.CompilerServices;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

public static class ProfilerExtensions
{
	#region Methods

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TimedScope Start(this IProfiler profiler, string name)
	{
		return profiler != null
			? new TimedScope(name, profiler)
			: default;
	}

	#endregion
}