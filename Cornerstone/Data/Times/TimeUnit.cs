#region References

using System.ComponentModel.DataAnnotations;

#endregion



namespace Cornerstone.Data.Times;

/// <summary>
/// Represents a unit of time.
/// </summary>
public enum TimeUnit
{
    [Display(Name = "Ticks", ShortName = "t")]
    Min = 0,

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

    [Display(Name = "Hour", ShortName = "h")]
    Week = 8,

    [Display(Name = "Month", ShortName = "mth")]
    Month = 9,

    [Display(Name = "Year", ShortName = "yr")]
    Year = 10,

    [Display(Name = "Year", ShortName = "yr")]
    Max = 10
}