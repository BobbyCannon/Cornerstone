#region References

using System;

#endregion

namespace Cornerstone.Extensions;

public static class MathExtensions
{
	#region Methods

	public static float Clamp(float value, float max, float min)
	{
		#if NETSTANDARD2_0
		return Clamp<float>(value, max, min);
		#else
		return Math.Clamp(value, max, min);
		#endif
	}

	public static T Clamp<T>(T value, T max, T min) where T : IComparable<T>
	{
		var result = value;
		if (value.CompareTo(max) > 0)
		{
			result = max;
		}
		if (value.CompareTo(min) < 0)
		{
			result = min;
		}
		return result;
	}

	#endregion
}