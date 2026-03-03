#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for the Double type.
/// </summary>
public static class DoubleExtensions
{
	#region Methods

	/// <summary>
	/// Compares two double values for equality within a specified tolerance.
	/// </summary>
	/// <param name="d1"> The first double value to compare. </param>
	/// <param name="d2"> The second double value to compare. </param>
	/// <param name="epsilon"> The tolerance for comparison. If null, uses a relative epsilon based on the values. </param>
	/// <param name="distinguishZeroSigns"> If true, treats positive zero (0.0) and negative zero (-0.0) as distinct. If false, treats them as equal. </param>
	/// <returns> True if the values are equal or within the specified tolerance, respecting the zero sign distinction setting. </returns>
	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
	public static bool AreEqual(this double d1, double d2, double? epsilon = null, bool distinguishZeroSigns = false)
	{
		// Handle NaN cases
		if (double.IsNaN(d1) || double.IsNaN(d2))
		{
			return false;
		}

		// Handle exact equality, respecting zero sign distinction
		if (d1 == d2)
		{
			if (distinguishZeroSigns && ((d1 == 0.0) || (d2 == 0.0)))
			{
				// Compare the bit representation to distinguish +0.0 and -0.0
				return BitConverter.DoubleToInt64Bits(d1) == BitConverter.DoubleToInt64Bits(d2);
			}
			return true;
		}

		// Use provided epsilon or calculate adaptive epsilon
		var effectiveEpsilon = epsilon ?? double.Epsilon;

		// Compare within epsilon
		return Math.Abs(d1 - d2) <= effectiveEpsilon;
	}

	#endregion
}