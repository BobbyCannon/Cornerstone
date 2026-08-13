#region References

using System;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

/// <summary>
/// View clock for Grok Usage: live wall time, or <see cref="GrokUsageState.ViewAsOf" /> when scrubbing.
/// Built as a <see cref="DateTimeProvider" /> so the rest of GrokUsage only asks for "now".
/// </summary>
public static class GrokUsageDateTimeProvider
{
	#region Methods

	/// <summary>
	/// Creates a provider whose <see cref="IDateTimeProvider.UtcNow" /> follows GrokUsage view-clock state.
	/// </summary>
	/// <param name="usage"> Shared GrokUsage state (live flag + ViewAsOf). </param>
	/// <param name="wallClock"> Real (or test) wall clock used when live. </param>
	public static IDateTimeProvider Create(GrokUsageState usage, IDateTimeProvider wallClock = null)
	{
		if (usage == null)
		{
			throw new ArgumentNullException(nameof(usage));
		}

		var wall = wallClock ?? DateTimeProvider.RealTime;
		return new DateTimeProvider(() =>
		{
			if (usage.IsViewLive || (usage.ViewAsOf == default))
			{
				return DateTime.SpecifyKind(wall.UtcNow, DateTimeKind.Utc);
			}

			return DateTime.SpecifyKind(usage.ViewAsOf.UtcDateTime, DateTimeKind.Utc);
		});
	}

	#endregion
}