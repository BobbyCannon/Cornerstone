// ReSharper disable UnusedMember.Local

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for numbers
/// </summary>
public static class NumberExtensions
{
	#region Constants

	private const long _bitsInByte = 8;
	private const decimal _bytesInGigabyte = 1073741824;
	private const decimal _bytesInKilobyte = 1024;
	private const decimal _bytesInMegabyte = 1048576;
	private const decimal _bytesInTerabyte = 1099511627776;

	#endregion

	#region Methods

	/// <summary>
	/// Converts the bytes into a megabyte value.
	/// </summary>
	/// <param name="bytes"> The byte value. </param>
	/// <returns> The value in a megabyte unit. </returns>
	public static decimal ToMegaBytes(this long bytes)
	{
		return bytes / _bytesInMegabyte;
	}

	#endregion
}