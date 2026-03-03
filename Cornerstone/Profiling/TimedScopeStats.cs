namespace Cornerstone.Profiling;

public sealed class TimedScopeStats
{
	#region Fields

	public double AverageTicks;
	public double CallsPerSecond;
	public long Count;
	public SeriesDataProvider History;
	public long TotalTicks;

	#endregion
}