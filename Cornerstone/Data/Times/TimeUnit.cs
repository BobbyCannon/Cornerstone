#region References

using System.ComponentModel.DataAnnotations;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data.Times;

/// <summary>
/// Represents a unit of time.
/// </summary>
[SourceReflection]
public enum TimeUnit
{
	[Display(Name = "Ticks", ShortName = "t")]
	Ticks = 0,

	[Display(Name = "Nanosecond", ShortName = "ns")]
	Nanosecond = 1,

	[Display(Name = "Microsecond", ShortName = "µs")]
	Microsecond = 2,

	[Display(Name = "Millisecond", ShortName = "ms")]
	Millisecond = 3,

	[Display(Name = "Second", ShortName = "s")]
	Second = 4,

	[Display(Name = "Minute", ShortName = "m")]
	Minute = 5,

	[Display(Name = "Hour", ShortName = "h")]
	Hour = 6,

	[Display(Name = "Day", ShortName = "d")]
	Day = 7,

	[Display(Name = "Week", ShortName = "w")]
	Week = 8,

	[Display(Name = "Month", ShortName = "mth")]
	Month = 9,

	[Display(Name = "Year", ShortName = "y")]
	Year = 10
}