#region References

using System;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Scales standard gamepad axes into the short and byte ranges <see cref="GamepadState" /> stores.
/// </summary>
internal static class GamepadValues
{
	#region Methods

	/// <summary>
	/// True when a 0-at-rest axis is at least halfway to its maximum.
	/// </summary>
	public static bool AxisPastHalf(int value, int minimum, int maximum)
	{
		if (maximum <= minimum)
		{
			return false;
		}

		var normalized = (value - (double) minimum) / (maximum - (double) minimum);
		return normalized >= 0.5d;
	}

	/// <summary>
	/// Hat axes use negative for left and up, positive for right and down.
	/// </summary>
	public static int HatDirection(int value, int minimum, int maximum, int flat)
	{
		var span = maximum - minimum;
		if (span <= 0)
		{
			return 0;
		}

		var center = (minimum + (double) maximum) / 2d;
		var delta = value - center;
		var quarter = span / 4d;
		var threshold = flat > quarter ? flat : quarter;
		if (Math.Abs(delta) <= threshold)
		{
			return 0;
		}

		return delta < 0d ? -1 : 1;
	}

	public static short ToThumb(double axis)
	{
		if (double.IsNaN(axis))
		{
			return 0;
		}

		if (axis > 1d)
		{
			axis = 1d;
		}
		else if (axis < -1d)
		{
			axis = -1d;
		}

		var scaled = axis >= 0d ? axis * 32767d : axis * 32768d;
		var rounded = (int) Math.Round(scaled);
		if (rounded > short.MaxValue)
		{
			return short.MaxValue;
		}

		if (rounded < short.MinValue)
		{
			return short.MinValue;
		}

		return (short) rounded;
	}

	public static short ToThumbFromRange(int value, int minimum, int maximum, bool invert)
	{
		if (maximum == minimum)
		{
			return 0;
		}

		var normalized = (value - (double) minimum) / (maximum - (double) minimum);
		var centered = (normalized * 2d) - 1d;
		if (invert)
		{
			centered = -centered;
		}

		return ToThumb(centered);
	}

	public static byte ToTrigger(double value)
	{
		if (double.IsNaN(value) || value <= 0d)
		{
			return 0;
		}

		if (value >= 1d)
		{
			return 255;
		}

		return (byte) Math.Round(value * 255d);
	}

	public static byte ToTriggerFromRange(int value, int minimum, int maximum)
	{
		if (maximum <= minimum)
		{
			return 0;
		}

		var normalized = (value - (double) minimum) / (maximum - (double) minimum);
		return ToTrigger(normalized);
	}

	#endregion
}
