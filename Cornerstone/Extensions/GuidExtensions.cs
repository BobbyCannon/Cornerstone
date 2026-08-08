#region References

using System;
using Cornerstone.Convert;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for Guids.
/// </summary>
public static class GuidExtensions
{
	#region Methods

	/// <summary>
	/// Converts an integer to a Guid. Ex. 1 == 00000000-0000-0000-0000-000000000001
	/// </summary>
	/// <param name="value"> The value to be converted to a Guid. </param>
	/// <returns> The guid. </returns>
	public static Guid ToGuid(this int value)
	{
		return ToGuid((long) value);
	}

	/// <summary>
	/// Converts a long to a Guid. Ex. 1 == 00000000-0000-0000-0000-000000000001
	/// </summary>
	/// <param name="value"> The value to be converted to a Guid. </param>
	/// <returns> The guid. </returns>
	public static Guid ToGuid(this long value)
	{
		return ToGuid((ulong) value);
	}

	/// <summary>
	/// Converts a ulong to a Guid. Ex. 1 == 00000000-0000-0000-0000-000000000001
	/// </summary>
	/// <param name="value"> The value to be converted to a Guid. </param>
	/// <returns> The guid. </returns>
	public static Guid ToGuid(this ulong value)
	{
		return value.ConvertTo<Guid>();
	}

	/// <summary>
	/// Converts a Guid to a Short Guid.
	/// </summary>
	/// <param name="guid"> The Guid to convert. </param>
	/// <returns> The Guid in a Short Guid format. </returns>
	public static ShortGuid ToShortGuid(this Guid guid)
	{
		return new ShortGuid(guid);
	}

	#endregion
}