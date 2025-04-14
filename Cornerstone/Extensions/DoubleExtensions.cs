#region References

using System;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for the Double type.
/// </summary>
public static class DoubleExtensions
{
	#region Methods

	/// <summary>
	/// Compare two values using a precision.
	/// </summary>
	/// <param name="a"> The first version. </param>
	/// <param name="b"> The second version. </param>
	/// <param name="epsilon"> The precision. </param>
	/// <returns> True if the values are equal or close otherwise false. </returns>
	public static bool ComparePrecision(this double a, double b, double epsilon = double.Epsilon)
	{
		var absA = Math.Abs(a);
		var absB = Math.Abs(b);
		var diff = Math.Abs(a - b);

		if (a.Equals(b))
		{
			// shortcut, handles infinities and NaN
			return true;
		}
		if ((a == 0) || (b == 0) || ((absA + absB) < double.MinValue))
		{
			// a or b is zero or both are extremely close to it
			// relative error is less meaningful here
			return diff < (epsilon * double.MinValue);
		}

		// use relative error
		return (diff / (absA + absB)) < epsilon;
	}

	/// <summary>
	/// Decrement a double by a value or double.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional value to decrement. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static double Decrement(this double value, double decrease = double.Epsilon)
	{
		if (double.IsInfinity(value) || double.IsInfinity(decrease))
		{
			return value;
		}

		if (double.IsNaN(value) || double.IsNaN(decrease))
		{
			return value;
		}

		return value - decrease;
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
	/// Increment a double by a value or double.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static double Increment(this double value, double increase = double.Epsilon)
	{
		if (double.IsInfinity(value) || double.IsInfinity(increase))
		{
			return value;
		}

		if (double.IsNaN(value) || double.IsNaN(increase))
		{
			return value;
		}

		return value + increase;
	}

	/// <summary>
	/// Compares two doubles to see if they are close or equal.
	/// </summary>
	/// <param name="d1"> The value to compare to. </param>
	/// <param name="d2"> The value to compare. </param>
	/// <returns>
	/// True if the values are equal or close to being equal.
	/// </returns>
	public static bool IsEqual(this double d1, double d2)
	{
		// In case they are Infinities (then epsilon check does not work)
		if (d1 == d2)
		{
			return true;
		}
		var eps = (Math.Abs(d1) + Math.Abs(d2) + 10.0) * double.Epsilon;
		var delta = d1 - d2;
		return (-eps < delta) && (eps > delta);
	}

	/// <summary>
	/// IsZero - Returns whether the double is "close" to 0.  Same as AreClose(double, 0),
	/// but this is faster.
	/// </summary>
	/// <param name="value"> The double to compare to 0. </param>
	public static bool IsZero(double value)
	{
		return Math.Abs(value) < (10.0 * double.Epsilon);
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