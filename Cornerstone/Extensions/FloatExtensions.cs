#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for float
/// </summary>
public static class FloatExtensions
{
	#region Constants

	internal const float FloatEpsilon = 1.192092896e-07F;

	#endregion

	#region Methods

	/// <summary>
	/// Compares two float values for equality within a specified tolerance.
	/// </summary>
	/// <param name="f1"> The first float value to compare. </param>
	/// <param name="f2"> The second float value to compare. </param>
	/// <param name="epsilon"> The tolerance for comparison. If null, uses a relative epsilon based on the values. </param>
	/// <param name="distinguishZeroSigns"> If true, treats positive zero (0.0f) and negative zero (-0.0f) as distinct. If false, treats them as equal. </param>
	/// <returns> True if the values are equal or within the specified tolerance, respecting the zero sign distinction setting. </returns>
	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
	public static bool AreEqual(this float f1, float f2, float? epsilon = null, bool distinguishZeroSigns = false)
	{
		// Handle NaN cases
		if (float.IsNaN(f1) || float.IsNaN(f2))
		{
			return false;
		}

		// Handle exact equality, respecting zero sign distinction
		if (f1 == f2)
		{
			if (distinguishZeroSigns && ((f1 == 0.0f) || (f2 == 0.0f)))
			{
				// Compare the bit representation to distinguish +0.0f and -0.0f
				return BitConverter.SingleToInt32Bits(f1) == BitConverter.SingleToInt32Bits(f2);
			}
			return true;
		}

		// Use provided epsilon or calculate adaptive epsilon
		var effectiveEpsilon = epsilon ?? float.Epsilon;

		// Compare within epsilon
		return Math.Abs(f1 - f2) <= effectiveEpsilon;
	}

	/// <summary>
	/// IsZero - Returns whether the float is "close" to 0.  Same as AreClose(float, 0),
	/// but this is faster.
	/// </summary>
	/// <param name="value"> The float to compare to 0. </param>
	public static bool IsZero(float value)
	{
		return Math.Abs(value) < (10.0f * FloatEpsilon);
	}

	#endregion
}