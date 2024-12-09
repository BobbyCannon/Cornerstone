#region References

using System;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Data.Times;

public class HolidayValue
{
	#region Constructors

	public HolidayValue()
	{
	}

	public HolidayValue(Holiday holiday, DateTime date) : this(holiday, date, null)
	{
	}

	public HolidayValue(Holiday holiday, DateTime date, DateTime? inLieuDate)
	{
		Holiday = holiday;
		Date = date.ToLocalTime(true);
		InLieuDate = inLieuDate?.ToLocalTime(true);
	}

	#endregion

	#region Properties

	public DateTime Date { get; set; }

	public Holiday Holiday { get; set; }

	public DateTime? InLieuDate { get; set; }

	public bool IsFederalHoliday => Holiday is >= Holiday.NewYearsDay and <= Holiday.ChristmasDay;

	#endregion
}