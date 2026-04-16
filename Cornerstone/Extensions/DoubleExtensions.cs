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
	#region Constants

	internal const double DoubleEpsilon = 2.2204460492503131e-016;

	#endregion

	#region Methods

	/// <summary>
	/// AreClose - Returns whether two doubles are "close".
	/// That is, whether they are within epsilon of each other.
	/// </summary>
	/// <param name="value1"> The first double to compare. </param>
	/// <param name="value2"> The second double to compare. </param>
	public static bool AreClose(double value1, double value2)
	{
		//in case they are Infinities (then epsilon check does not work)
		if (value1 == value2)
		{
			return true;
		}
		var eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DoubleEpsilon;
		var delta = value1 - value2;
		return (-eps < delta) && (eps > delta);
	}

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

	/// <summary>
	/// GreaterThan - Returns whether the first double is greater than the second double.
	/// That is, whether the first is strictly greater than *and* not within epsilon of
	/// the other number.
	/// </summary>
	/// <param name="value1"> The first double to compare. </param>
	/// <param name="value2"> The second double to compare. </param>
	public static bool GreaterThan(double value1, double value2)
	{
		return (value1 > value2) && !AreClose(value1, value2);
	}

	/// <summary>
	/// Compares Y to see if it is greater than or equal to X.
	/// </summary>
	/// <param name="x"> The first value. </param>
	/// <param name="y"> The second value. </param>
	/// <returns> True if the value is greater than or equal otherwise false. </returns>
	public static bool GreaterThanOrEqualTo(this double x, double y)
	{
		return (x > y) || x.Equals(y);
	}

	/// <summary>
	/// IsZero - Returns whether the double is "close" to 0.  Same as AreClose(double, 0),
	/// but this is faster.
	/// </summary>
	/// <param name="value"> The double to compare to 0. </param>
	public static bool IsZero(double value)
	{
		return Math.Abs(value) < (10.0 * DoubleEpsilon);
	}

	/// <summary>
	/// Compares Y to see if it is less than or equal to X.
	/// </summary>
	/// <param name="x"> The first value. </param>
	/// <param name="y"> The second value. </param>
	/// <returns> True if the value is less than or equal otherwise false. </returns>
	public static bool LessThanOrEqualTo(this double x, double y)
	{
		return (x < y) || x.Equals(y);
	}

	#endregion
}