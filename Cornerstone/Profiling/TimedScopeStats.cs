namespace Cornerstone.Profiling;

public sealed class TimedScopeStats
{
	#region Fields

	public SeriesDataProvider AverageHistory;
	public double AverageTicks;
	public double CallsPerSecond;
	public long Count;

	/// <summary>
	/// Scope name (set when the stats entry is first created).
	/// </summary>
	public string Name = string.Empty;

	public SeriesDataProvider PerSecondHistory;
	public long TotalTicks;

	#endregion
}