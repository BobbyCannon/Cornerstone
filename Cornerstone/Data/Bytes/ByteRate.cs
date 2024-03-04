#region References

using System;
using Cornerstone.Data.Times;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Data.Bytes;

/// <summary>
/// Class to hold a ByteSize and a measurement interval, for the purpose of calculating the rate of transfer
/// </summary>
public class ByteRate
{
	#region Constructors

	/// <summary>
	/// Create a ByteRate with given quantity of bytes across an interval
	/// </summary>
	/// <param name="size"> </param>
	/// <param name="interval"> </param>
	public ByteRate(ByteSize size, TimeSpan interval)
	{
		Size = size;
		Interval = interval;
	}

	static ByteRate()
	{
		Cellular2G = ByteSize.FromKilobits(50);
		Cellular3G = ByteSize.FromMegabytes(5);
		Cellular4G = ByteSize.FromMegabytes(20);
		Cellular5G = ByteSize.FromMegabytes(100);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Average speed of 2G cellular network. 50 Kb/s
	/// </summary>
	public static ByteSize Cellular2G { get; }

	/// <summary>
	/// Average speed of 3G cellular network. 5 Mb/s
	/// </summary>
	public static ByteSize Cellular3G { get; }

	/// <summary>
	/// Average speed of 4G cellular network. 20 Mb/s
	/// </summary>
	public static ByteSize Cellular4G { get; }

	/// <summary>
	/// Average speed of 5G cellular network. 100 Mb/s
	/// </summary>
	public static ByteSize Cellular5G { get; }

	/// <summary>
	/// Interval that bytes were transferred in.
	/// </summary>
	public TimeSpan Interval { get; }

	/// <summary>
	/// Quantity of data.
	/// </summary>
	public ByteSize Size { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Calculate rate for the quantity of bytes and interval defined by this instance
	/// </summary>
	/// <param name="timeUnit"> Unit of time to calculate rate for (defaults is per second) </param>
	/// <returns> </returns>
	public string Humanize(TimeUnit timeUnit = TimeUnit.Second)
	{
		return Humanize(null, timeUnit);
	}

	/// <summary>
	/// Calculate rate for the quantity of bytes and interval defined by this instance
	/// </summary>
	/// <param name="timeUnit"> Unit of time to calculate rate for (defaults is per second) </param>
	/// <param name="format"> The string format to use for the number of bytes </param>
	/// <returns> </returns>
	public string Humanize(string format, TimeUnit timeUnit = TimeUnit.Second)
	{
		var displayInterval = timeUnit switch
		{
			TimeUnit.Second => TimeSpan.FromSeconds(1),
			TimeUnit.Minute => TimeSpan.FromMinutes(1),
			TimeUnit.Hour => TimeSpan.FromHours(1),
			_ => throw new NotSupportedException("timeUnit must be Second, Minute, or Hour")
		};
		var size = new ByteSize((decimal) (((double) Size.Bytes / Interval.TotalSeconds) * displayInterval.TotalSeconds));
		return $"{size.Humanize(format)}/{timeUnit.Humanize()}";
	}

	#endregion
}