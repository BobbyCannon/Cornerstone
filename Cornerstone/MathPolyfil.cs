#if NETSTANDARD2_0

#region References

using System;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone;

public static class Math2
{
	#region Methods

	public static T Clamp<T>(T value, T max, T min)
		where T : IComparable<T>
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

public static class MathF
{
	#region Constants

	public const float E = 2.71828183f;

	public const float PI = 3.14159265f;

	public const float Tau = 6.283185307f;

	#endregion

	#region Methods

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Abs(float x)
	{
		return Math.Abs(x);
	}

	public static float Atan2(float f, float f1)
	{
		return (float) Math.Atan2(f, f1);
	}

	public static float Cos(float angle)
	{
		return (float) Math.Cos(angle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Floor(float value)
	{
		return (float) Math.Floor(value);
	}

	public static float Sin(float angle)
	{
		return (float) Math.Sin(angle);
	}

	public static float Sqrt(float f)
	{
		return (float) Math.Sqrt(f);
	}

	#endregion
}
#endif