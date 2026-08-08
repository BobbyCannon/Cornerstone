#region References

using System;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for numbers
/// </summary>
public static class IntegerExtensions
{
	#region Methods

	/// <summary>
	/// Ensure the value falls between the ranges.
	/// </summary>
	/// <param name="value"> The nullable float value. </param>
	/// <param name="inclusiveMinimum"> The inclusive minimal value. </param>
	/// <param name="inclusiveMaximum"> The inclusive maximum value. </param>
	/// <returns> The value within the provided ranges. </returns>
	public static int EnsureRange(this int? value, int inclusiveMinimum, int inclusiveMaximum)
	{
		return value is null ? inclusiveMinimum : EnsureRange(value.Value, inclusiveMinimum, inclusiveMaximum);
	}

	/// <summary>
	/// Ensure the value falls between the ranges.
	/// </summary>
	/// <param name="value"> The nullable int value. </param>
	/// <param name="inclusiveMinimum"> The inclusive minimal value. </param>
	/// <param name="inclusiveMaximum"> The inclusive maximum value. </param>
	/// <returns> The value within the provided ranges. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int EnsureRange(this int value, int inclusiveMinimum, int inclusiveMaximum)
	{
		return Math.Clamp(value, inclusiveMinimum, inclusiveMaximum);
	}

	/// <summary>
	/// Ensure the value falls between the ranges.
	/// </summary>
	/// <param name="value"> The nullable int value. </param>
	/// <param name="inclusiveMinimum"> The inclusive minimal value. </param>
	/// <param name="inclusiveMaximum"> The inclusive maximum value. </param>
	/// <returns> The value within the provided ranges. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void EnsureRange(ref int value, int inclusiveMinimum, int inclusiveMaximum)
	{
		value = Math.Max(inclusiveMinimum, Math.Min(value, inclusiveMaximum));
	}

	#endregion
}