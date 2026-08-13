namespace Cornerstone.GrokMonitor.GrokUsage.Channels;

public enum GrokUsageMessageType
{
	Unknown = 0,
	EnsureHomes = 1,
	RefreshHome = 2,
	RefreshAll = 3,
	SelectHome = 4,
	SetSince = 5,
	SelectPeriod = 6,

	/// <summary>
	/// Set the GrokUsage view clock (scrub) and reproject without requiring a new disk read when cached.
	/// </summary>
	SetViewAsOf = 7,

	/// <summary>
	/// Pin the view clock to wall time (live end of the scrub range).
	/// </summary>
	SetViewLive = 8
}