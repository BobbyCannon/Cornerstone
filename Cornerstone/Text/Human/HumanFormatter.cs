﻿#region References

using System.Collections.Generic;
using System.Collections.ObjectModel;

#endregion

namespace Cornerstone.Text.Human;

internal class HumanFormatter
{
	#region Fields

	public static readonly IReadOnlyDictionary<string, string> StringFormats =
		new ReadOnlyDictionary<string, string>(
			new Dictionary<string, string>
			{

				{ "DataUnit_Bit", "Bit" },
				{ "DataUnit_BitSymbol", "b" },
				{ "DataUnit_Byte", "Byte" },
				{ "DataUnit_ByteSymbol", "B" },
				{ "DataUnit_Gigabit", "Gigabit" },
				{ "DataUnit_GigabitSymbol", "Gb" },
				{ "DataUnit_Gigabyte", "Gigabyte" },
				{ "DataUnit_GigabyteSymbol", "GB" },
				{ "DataUnit_Kilobit", "Kilobit" },
				{ "DataUnit_KilobitSymbol", "Kb" },
				{ "DataUnit_Kilobyte", "Kilobyte" },
				{ "DataUnit_KilobyteSymbol", "KB" },
				{ "DataUnit_Megabit", "Megabit" },
				{ "DataUnit_MegabitSymbol", "Mb" },
				{ "DataUnit_Megabyte", "Megabyte" },
				{ "DataUnit_MegabyteSymbol", "MB" },
				{ "DataUnit_Terabit", "Terabit" },
				{ "DataUnit_TerabitSymbol", "Tb" },
				{ "DataUnit_Terabyte", "Terabyte" },
				{ "DataUnit_TerabyteSymbol", "TB" },
				{ "DateHumanize_MultipleDaysAgo", "{0} days ago" },
				{ "DateHumanize_MultipleDaysAgo_Above20", "{0} days ago" },
				{ "DateHumanize_MultipleDaysAgo_Dual", "{0} days ago" },
				{ "DateHumanize_MultipleDaysAgo_Plural", "{0} days ago" },
				{ "DateHumanize_MultipleDaysAgo_Singular", "{0} day ago" },
				{ "DateHumanize_MultipleDaysAgo_TrialQuadral", "{0} days ago" },
				{ "DateHumanize_MultipleDaysFromNow", "{0} days from now" },
				{ "DateHumanize_MultipleDaysFromNow_Dual", "{0} days from now" },
				{ "DateHumanize_MultipleDaysFromNow_Plural", "{0} days from now" },
				{ "DateHumanize_MultipleDaysFromNow_Singular", "{0} day from now" },
				{ "DateHumanize_MultipleDaysFromNow_TrialQuadral", "{0} days from now" },
				{ "DateHumanize_MultipleHoursAgo", "{0} hours ago" },
				{ "DateHumanize_MultipleHoursAgo_Above20", "{0} hours ago" },
				{ "DateHumanize_MultipleHoursAgo_Dual", "{0} hours ago" },
				{ "DateHumanize_MultipleHoursAgo_DualTrialQuadral", "{0} hours ago" },
				{ "DateHumanize_MultipleHoursAgo_Plural", "{0} hours ago" },
				{ "DateHumanize_MultipleHoursAgo_Singular", "{0} hour ago" },
				{ "DateHumanize_MultipleHoursAgo_TrialQuadral", "{0} hours ago" },
				{ "DateHumanize_MultipleHoursFromNow", "{0} hours from now" },
				{ "DateHumanize_MultipleHoursFromNow_Dual", "{0} hours from now" },
				{ "DateHumanize_MultipleHoursFromNow_DualTrialQuadral", "{0} hours from now" },
				{ "DateHumanize_MultipleHoursFromNow_Plural", "{0} hours from now" },
				{ "DateHumanize_MultipleHoursFromNow_Singular", "{0} hour from now" },
				{ "DateHumanize_MultipleHoursFromNow_TrialQuadral", "{0} hours from now" },
				{ "DateHumanize_MultipleMinutesAgo", "{0} minutes ago" },
				{ "DateHumanize_MultipleMinutesAgo_Above20", "{0} minutes ago" },
				{ "DateHumanize_MultipleMinutesAgo_Dual", "{0} minutes ago" },
				{ "DateHumanize_MultipleMinutesAgo_DualTrialQuadral", "{0} minutes ago" },
				{ "DateHumanize_MultipleMinutesAgo_Plural", "{0} minutes ago" },
				{ "DateHumanize_MultipleMinutesAgo_Singular", "{0} minute ago" },
				{ "DateHumanize_MultipleMinutesAgo_TrialQuadral", "{0} minutes ago" },
				{ "DateHumanize_MultipleMinutesFromNow", "{0} minutes from now" },
				{ "DateHumanize_MultipleMinutesFromNow_Dual", "{0} minutes from now" },
				{ "DateHumanize_MultipleMinutesFromNow_DualTrialQuadral", "{0} minutes from now" },
				{ "DateHumanize_MultipleMinutesFromNow_Plural", "{0} minutes from now" },
				{ "DateHumanize_MultipleMinutesFromNow_Singular", "{0} minute from now" },
				{ "DateHumanize_MultipleMinutesFromNow_TrialQuadral", "{0} minutes from now" },
				{ "DateHumanize_MultipleMonthsAgo", "{0} months ago" },
				{ "DateHumanize_MultipleMonthsAgo_Dual", "{0} months ago" },
				{ "DateHumanize_MultipleMonthsAgo_DualTrialQuadral", "{0} months ago" },
				{ "DateHumanize_MultipleMonthsAgo_Plural", "{0} months ago" },
				{ "DateHumanize_MultipleMonthsAgo_Singular", "{0} month ago" },
				{ "DateHumanize_MultipleMonthsAgo_TrialQuadral", "{0} months ago" },
				{ "DateHumanize_MultipleMonthsFromNow", "{0} months from now" },
				{ "DateHumanize_MultipleMonthsFromNow_Dual", "{0} months from now" },
				{ "DateHumanize_MultipleMonthsFromNow_DualTrialQuadral", "{0} months from now" },
				{ "DateHumanize_MultipleMonthsFromNow_Plural", "{0} months from now" },
				{ "DateHumanize_MultipleMonthsFromNow_Singular", "{0} month from now" },
				{ "DateHumanize_MultipleMonthsFromNow_TrialQuadral", "{0} months from now" },
				{ "DateHumanize_MultipleSecondsAgo", "{0} seconds ago" },
				{ "DateHumanize_MultipleSecondsAgo_Above20", "{0} seconds ago" },
				{ "DateHumanize_MultipleSecondsAgo_Dual", "{0} seconds ago" },
				{ "DateHumanize_MultipleSecondsAgo_DualTrialQuadral", "{0} seconds ago" },
				{ "DateHumanize_MultipleSecondsAgo_Plural", "{0} seconds ago" },
				{ "DateHumanize_MultipleSecondsAgo_Singular", "{0} second ago" },
				{ "DateHumanize_MultipleSecondsAgo_TrialQuadral", "{0} seconds ago" },
				{ "DateHumanize_MultipleSecondsFromNow", "{0} seconds from now" },
				{ "DateHumanize_MultipleSecondsFromNow_Dual", "{0} seconds from now" },
				{ "DateHumanize_MultipleSecondsFromNow_DualTrialQuadral", "{0} seconds from now" },
				{ "DateHumanize_MultipleSecondsFromNow_Plural", "{0} seconds from now" },
				{ "DateHumanize_MultipleSecondsFromNow_Singular", "{0} second from now" },
				{ "DateHumanize_MultipleSecondsFromNow_TrialQuadral", "{0} seconds from now" },
				{ "DateHumanize_MultipleYearsAgo", "{0} years ago" },
				{ "DateHumanize_MultipleYearsAgo_Above20", "{0} years ago" },
				{ "DateHumanize_MultipleYearsAgo_Dual", "{0} years ago" },
				{ "DateHumanize_MultipleYearsAgo_DualTrialQuadral", "{0} years from ago" },
				{ "DateHumanize_MultipleYearsAgo_Plural", "{0} years ago" },
				{ "DateHumanize_MultipleYearsAgo_Singular", "{0} year ago" },
				{ "DateHumanize_MultipleYearsAgo_TrialQuadral", "{0} years ago" },
				{ "DateHumanize_MultipleYearsFromNow", "{0} years from now" },
				{ "DateHumanize_MultipleYearsFromNow_Dual", "{0} years from now" },
				{ "DateHumanize_MultipleYearsFromNow_DualTrialQuadral", "{0} years from now" },
				{ "DateHumanize_MultipleYearsFromNow_Plural", "{0} years from now" },
				{ "DateHumanize_MultipleYearsFromNow_Singular", "{0} year from now" },
				{ "DateHumanize_MultipleYearsFromNow_TrialQuadral", "{0} years from now" },
				{ "DateHumanize_Never", "never" },
				{ "DateHumanize_Now", "now" },
				{ "DateHumanize_SingleDayAgo", "yesterday" },
				{ "DateHumanize_SingleDayFromNow", "tomorrow" },
				{ "DateHumanize_SingleHourAgo", "an hour ago" },
				{ "DateHumanize_SingleHourFromNow", "an hour from now" },
				{ "DateHumanize_SingleMinuteAgo", "a minute ago" },
				{ "DateHumanize_SingleMinuteFromNow", "a minute from now" },
				{ "DateHumanize_SingleMonthAgo", "one month ago" },
				{ "DateHumanize_SingleMonthFromNow", "one month from now" },
				{ "DateHumanize_SingleSecondAgo", "one second ago" },
				{ "DateHumanize_SingleSecondFromNow", "one second from now" },
				{ "DateHumanize_SingleYearAgo", "one year ago" },
				{ "DateHumanize_SingleYearFromNow", "one year from now" },
				{ "E", "east" },
				{ "E_Short", "E" },
				{ "ENE", "east-northeast" },
				{ "ENE_Short", "ENE" },
				{ "ESE", "east-southeast" },
				{ "ESE_Short", "ESE" },
				{ "N", "north" },
				{ "N_Short", "N" },
				{ "NE", "northeast" },
				{ "NE_Short", "NE" },
				{ "NNE", "north-northeast" },
				{ "NNE_Short", "NNE" },
				{ "NNW", "north-northwest" },
				{ "NNW_Short", "NNW" },
				{ "NW", "northwest" },
				{ "NW_Short", "NW" },
				{ "S", "south" },
				{ "S_Short", "S" },
				{ "SE", "southeast" },
				{ "SE_Short", "SE" },
				{ "SSE", "south-southeast" },
				{ "SSE_Short", "SSE" },
				{ "SSW", "south-southwest" },
				{ "SSW_Short", "SSW" },
				{ "SW", "southwest" },
				{ "SW_Short", "SW" },
				{ "TimeSpanHumanize_MultipleDays", "{0} days" },
				{ "TimeSpanHumanize_MultipleDays_Dual", "{0} days" },
				{ "TimeSpanHumanize_MultipleDays_DualTrialQuadral", "{0} days" },
				{ "TimeSpanHumanize_MultipleDays_Plural", "{0} days" },
				{ "TimeSpanHumanize_MultipleDays_Singular", "{0} day" },
				{ "TimeSpanHumanize_MultipleDays_TrialQuadral", "{0} days" },
				{ "TimeSpanHumanize_MultipleHours", "{0} hours" },
				{ "TimeSpanHumanize_MultipleHours_Dual", "{0} hours" },
				{ "TimeSpanHumanize_MultipleHours_DualTrialQuadral", "{0} hours" },
				{ "TimeSpanHumanize_MultipleHours_Plural", "{0} hours" },
				{ "TimeSpanHumanize_MultipleHours_Singular", "{0} hour" },
				{ "TimeSpanHumanize_MultipleHours_TrialQuadral", "{0} hours" },
				{ "TimeSpanHumanize_MultipleMilliseconds", "{0} milliseconds" },
				{ "TimeSpanHumanize_MultipleMilliseconds_Dual", "{0} milliseconds" },
				{ "TimeSpanHumanize_MultipleMilliseconds_DualTrialQuadral", "{0} milliseconds" },
				{ "TimeSpanHumanize_MultipleMilliseconds_Plural", "{0} milliseconds" },
				{ "TimeSpanHumanize_MultipleMilliseconds_Singular", "{0} millisecond" },
				{ "TimeSpanHumanize_MultipleMilliseconds_TrialQuadral", "{0} milliseconds" },
				{ "TimeSpanHumanize_MultipleMinutes", "{0} minutes" },
				{ "TimeSpanHumanize_MultipleMinutes_Dual", "{0} minutes" },
				{ "TimeSpanHumanize_MultipleMinutes_DualTrialQuadral", "{0} minutes" },
				{ "TimeSpanHumanize_MultipleMinutes_Plural", "{0} minutes" },
				{ "TimeSpanHumanize_MultipleMinutes_Singular", "{0} minute" },
				{ "TimeSpanHumanize_MultipleMinutes_TrialQuadral", "{0} minutes" },
				{ "TimeSpanHumanize_MultipleMonths", "{0} months" },
				{ "TimeSpanHumanize_MultipleMonths_Dual", "{0} months" },
				{ "TimeSpanHumanize_MultipleMonths_DualTrialQuadral", "{0} months" },
				{ "TimeSpanHumanize_MultipleMonths_Plural", "{0} months" },
				{ "TimeSpanHumanize_MultipleMonths_Singular", "{0} months" },
				{ "TimeSpanHumanize_MultipleMonths_TrialQuadral", "{0} months" },
				{ "TimeSpanHumanize_MultipleSeconds", "{0} seconds" },
				{ "TimeSpanHumanize_MultipleSeconds_Dual", "{0} seconds" },
				{ "TimeSpanHumanize_MultipleSeconds_DualTrialQuadral", "{0} seconds" },
				{ "TimeSpanHumanize_MultipleSeconds_Plural", "{0} seconds" },
				{ "TimeSpanHumanize_MultipleSeconds_Singular", "{0} second" },
				{ "TimeSpanHumanize_MultipleSeconds_TrialQuadral", "{0} seconds" },
				{ "TimeSpanHumanize_MultipleWeeks", "{0} weeks" },
				{ "TimeSpanHumanize_MultipleWeeks_Dual", "{0} weeks" },
				{ "TimeSpanHumanize_MultipleWeeks_DualTrialQuadral", "{0} weeks" },
				{ "TimeSpanHumanize_MultipleWeeks_Plural", "{0} weeks" },
				{ "TimeSpanHumanize_MultipleWeeks_Singular", "{0} week" },
				{ "TimeSpanHumanize_MultipleWeeks_TrialQuadral", "{0} weeks" },
				{ "TimeSpanHumanize_MultipleYears", "{0} years" },
				{ "TimeSpanHumanize_MultipleYears_Dual", "{0} years" },
				{ "TimeSpanHumanize_MultipleYears_DualTrialQuadral", "{0} years" },
				{ "TimeSpanHumanize_MultipleYears_Plural", "{0} years" },
				{ "TimeSpanHumanize_MultipleYears_Singular", "{0} years" },
				{ "TimeSpanHumanize_MultipleYears_TrialQuadral", "{0} years" },
				{ "TimeSpanHumanize_SingleDay", "1 day" },
				{ "TimeSpanHumanize_SingleDay_Words", "one day" },
				{ "TimeSpanHumanize_SingleHour", "1 hour" },
				{ "TimeSpanHumanize_SingleHour_Words", "one hour" },
				{ "TimeSpanHumanize_SingleMillisecond", "1 millisecond" },
				{ "TimeSpanHumanize_SingleMillisecond_Words", "one millisecond" },
				{ "TimeSpanHumanize_SingleMinute", "1 minute" },
				{ "TimeSpanHumanize_SingleMinute_Words", "one minute" },
				{ "TimeSpanHumanize_SingleMonth", "1 month" },
				{ "TimeSpanHumanize_SingleMonth_Words", "one month" },
				{ "TimeSpanHumanize_SingleSecond", "1 second" },
				{ "TimeSpanHumanize_SingleSecond_Words", "one second" },
				{ "TimeSpanHumanize_SingleWeek", "1 week" },
				{ "TimeSpanHumanize_SingleWeek_Words", "one week" },
				{ "TimeSpanHumanize_SingleYear", "1 year" },
				{ "TimeSpanHumanize_SingleYear_Words", "one year" },
				{ "TimeSpanHumanize_Zero", "no time" },
				{ "W", "west" },
				{ "W_Short", "W" },
				{ "WNW", "west-northwest" },
				{ "WNW_Short", "WNW" },
				{ "WSW", "west-southwest" },
				{ "WSW_Short", "WSW" }
			}
		);

	#endregion

	#region Methods

	public static string GetStringFormat(string key)
	{
		return StringFormats.TryGetValue(key, out var value) ? value : string.Empty;
	}

	#endregion
}