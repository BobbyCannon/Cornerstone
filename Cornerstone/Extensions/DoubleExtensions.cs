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
	/// Decrement an double by a value or double.Epsilon if not provided.
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
	/// Increment an double by a value or double.Epsilon if not provided.
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
	/// <param name="precisionFactor"> The precision factor of double.Epsilon. Defaults to 1. </param>
	/// <returns>
	/// True if the values are equal or close to being equal.
	/// </returns>
	public static bool IsEqual(this double d1, double d2, uint precisionFactor = 1)
	{
		return Math.Abs(d1 - d2) < (precisionFactor * double.Epsilon);
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