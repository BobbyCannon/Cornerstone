#region References

using System;

#endregion

namespace Cornerstone.Data.Bytes;

/// <summary>
/// Provides extension methods for ByteSize
/// </summary>
public static partial class ByteSizeExtensions
{
	#region Methods

	/// <summary>
	/// Turns a byte quantity into human-readable form, eg 2 GB
	/// </summary>
	/// <param name="input"> </param>
	/// <returns> </returns>
	public static string Humanize(this ByteSize input)
	{
		return input.ToString();
	}

	/// <summary>
	/// Turns a byte quantity into human-readable form, eg 2 GB
	/// </summary>
	/// <param name="input"> </param>
	/// <param name="format"> The string format to use </param>
	/// <returns> </returns>
	public static string Humanize(this ByteSize input, string format)
	{
		return string.IsNullOrWhiteSpace(format) ? input.ToString() : input.ToString(format);
	}

	/// <summary>
	/// Turns a quantity of bytes in a given interval into a rate that can be manipulated
	/// </summary>
	/// <param name="size"> Quantity of bytes </param>
	/// <param name="interval"> Interval to create rate for </param>
	/// <returns> </returns>
	public static ByteRate Per(this ByteSize size, TimeSpan interval)
	{
		return new ByteRate(size, interval);
	}

	#endregion
}