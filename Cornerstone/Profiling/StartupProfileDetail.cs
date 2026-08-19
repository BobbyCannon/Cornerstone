namespace Cornerstone.Profiling;

/// <summary>
/// How much of a completed startup tree to show or copy.
/// Thresholds are inclusive elapsed time. Ancestors of matching nodes are kept.
/// </summary>
public enum StartupProfileDetail
{
	All = 0,
	Slow = 1,
	Slowest = 2
}
