#region References

using System;
using Cornerstone.Data.Times;
using Cornerstone.Text;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Data.Bytes;

/// <summary>
/// Class to hold a ByteSize and a ByteRate, for the purpose of calculating the time to download.
/// </summary>
public class DownloadTime
{
	#region Constructors

	/// <summary>
	/// Create a ByteRate with given quantity of bytes across an interval
	/// </summary>
	/// <param name="size"> Quantity of data to download. </param>
	/// <param name="rate"> The expected average rate of downloading. </param>
	public DownloadTime(ByteSize size, ByteSize rate)
	{
		Size = size;
		Rate = rate;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The expected average rate of downloading.
	/// </summary>
	public ByteSize Rate { get; }

	/// <summary>
	/// Quantity of data to download.
	/// </summary>
	public ByteSize Size { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Humanize the time it would take to download.
	/// </summary>
	/// <returns> </returns>
	public string Humanize(IHumanizeSettings settings = null)
	{
		var time = Size.Bytes / Rate.Bytes;
		return TimeSpan.FromSeconds((double) time).Humanize(settings);
	}

	/// <summary>
	/// Get a detailed message of the download time.
	/// </summary>
	/// <param name="min"> The minimum time unit. </param>
	/// <param name="max"> The maximum time unit. </param>
	/// <returns> A detail message. </returns>
	public string ToDetailedMessage(TimeUnit min = TimeUnit.Second, TimeUnit max = TimeUnit.Second)
	{
		var response = new TextBuilder();
		response.Append("To Download: ");
		response.AppendLine(Size.Humanize());

		response.Append("Rate: ");
		response.AppendLine(Size.Humanize());

		var options = new HumanizeSettings { MaxUnit = max, MinUnit = min };
		response.Append("Estimate: ");
		response.Append(Humanize(options));

		return response.ToString();
	}

	#endregion
}