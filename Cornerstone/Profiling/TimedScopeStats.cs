namespace Cornerstone.Profiling;

public sealed class TimedScopeStats
{
	#region Fields

	public SeriesDataProvider AverageHistory;
	public double AverageTicks;
	public double CallsPerSecond;
	public long Count;
	public SeriesDataProvider PerSecondHistory;
	public long TotalTicks;

	#endregion
}