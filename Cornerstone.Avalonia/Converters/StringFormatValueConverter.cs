#region References

using System;
using System.Globalization;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

/// <summary>
/// A value converter which calls <see cref="string.Format(string, object)" />
/// </summary>
public class StringValueConverter : IValueConverter
{
	#region Constructors

	public StringValueConverter()
	{
		NumberStyles = NumberStyles.Any;
	}

	#endregion

	#region Properties

	public string Format { get; set; }

	public NumberStyles NumberStyles { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var format = GetFinalFormat();

		return value switch
		{
			float sValue => FloatFormat(format, sValue),
			double sValue => DoubleFormat(format, sValue),
			_ => string.Format(culture, format, value)
		};
	}

	/// <inheritdoc />
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not string sValue)
		{
			return value;
		}

		var format = GetFinalFormat();

		if (targetType == typeof(float))
		{
			if (IsHexFormat(format))
			{
				return (float) ulong.Parse(sValue, NumberStyles);
			}

			return float.Parse(sValue, NumberStyles, null);
		}

		if (targetType == typeof(double))
		{
			if (IsHexFormat(format))
			{
				return (double) ulong.Parse(sValue, NumberStyles);
			}

			return double.Parse(sValue, NumberStyles, null);
		}

		if (targetType == typeof(int))
		{
			return int.Parse(sValue, NumberStyles, null);
		}

		if (targetType == typeof(uint))
		{
			return uint.Parse(sValue, NumberStyles, null);
		}

		return value;
	}

	private string GetFinalFormat()
	{
		var format = Format;
		if (!format.Contains('{'))
		{
			format = $"{{0:{format}}}";
		}
		return format;
	}

	private static string DoubleFormat(string format, double value)
	{
		if (!IsHexFormat(format))
		{
			return value.ToString(format);
		}
		var lValue = (ulong) value;
		return string.Format(format, lValue);
	}

	private static string FloatFormat(string format, float value)
	{
		if (!IsHexFormat(format))
		{
			return value.ToString(format);
		}
		var lValue = (ulong) value;
		return string.Format(format, lValue);
	}

	private static bool IsHexFormat(string format)
	{
		return format.Contains('x', StringComparison.OrdinalIgnoreCase);
	}

	#endregion
}