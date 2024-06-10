#region References

using System.ComponentModel.DataAnnotations;

#endregion



namespace Cornerstone.Data.Times;

/// <summary>
/// Months
/// </summary>
public enum Months
{
	Unknown = 0,

	[Display(Name = "January", ShortName = "JAN")]
	January = 1,

	[Display(Name = "February", ShortName = "FEB")]
	February = 2,

	[Display(Name = "March", ShortName = "MAR")]
	March = 3,

	[Display(Name = "April", ShortName = "APR")]
	April = 4,

	[Display(Name = "May", ShortName = "MAY")]
	May = 5,

	[Display(Name = "June", ShortName = "JUN")]
	June = 6,

	[Display(Name = "July", ShortName = "JUL")]
	July = 7,

	[Display(Name = "August", ShortName = "AUG")]
	August = 8,

	[Display(Name = "September", ShortName = "SEP")]
	September = 9,

	[Display(Name = "October", ShortName = "OCT")]
	October = 10,

	[Display(Name = "November", ShortName = "NOV")]
	November = 11,

	[Display(Name = "December", ShortName = "DEC")]
	December = 12
}