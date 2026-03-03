#region References

using System;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class ActivityItem
{
	#region Properties

	public int Count { get; set; }

	public DateTime Date { get; set; }

	public string Description => $"{Count} activity on {Date:MMMM dd, yyyy}";

	#endregion

	#region Methods

	public static DateTime GetStartOfActivity(DateTime start)
	{
		// Find the most recent Sunday (today or up to 6 days ago)
		var daysSinceSunday = (int) start.DayOfWeek; // Sunday = 0, Monday = 1, etc.
		var lastSunday = start.AddDays(-daysSinceSunday);
		var sunday54WeeksAgo = lastSunday.AddDays(-53 * 7);
		return sunday54WeeksAgo;
	}

	#endregion
}