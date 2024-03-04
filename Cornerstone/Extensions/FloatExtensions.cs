#region References

using System;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for float
/// </summary>
public static class FloatExtensions
{
	#region Methods

	/// <summary>
	/// Decrement an float by a value or float.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional value to decrement. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static float Decrement(this float value, float decrease = float.Epsilon)
	{
		if (float.IsInfinity(value) || float.IsInfinity(decrease))
		{
			return value;
		}

		if (float.IsNaN(value) || float.IsNaN(decrease))
		{
			return value;
		}

		return value - decrease;
	}

	/// <summary>
	/// Increment an float by a value or float.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static float Increment(this float value, float increase = float.Epsilon)
	{
		if (float.IsInfinity(value) || float.IsInfinity(increase))
		{
			return value;
		}

		if (float.IsNaN(value) || float.IsNaN(increase))
		{
			return value;
		}

		return value + increase;
	}

	/// <summary>
	/// Compares two floats to see if they are close or equal.
	/// </summary>
	/// <param name="d1"> The value to compare to. </param>
	/// <param name="d2"> The value to compare. </param>
	/// <param name="precisionFactor"> The precision factor of float.Epsilon. Defaults to 1. </param>
	/// <returns>
	/// True if the values are equal or close to being equal.
	/// </returns>
	public static bool IsEqual(this float d1, float d2, uint precisionFactor = 1)
	{
		return Math.Abs(d1 - d2) < (precisionFactor * float.Epsilon);
	}

	#endregion
}