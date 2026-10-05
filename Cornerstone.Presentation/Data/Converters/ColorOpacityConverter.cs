#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Media;

#endregion

namespace Cornerstone.Presentation.Data.Converters;

/// <summary>
/// Converts a color or solid brush to the same RGB with a replaced alpha (opacity 0–1).
/// </summary>
public class ColorOpacityConverter : IValueConverter
{
	#region Constructors

	public ColorOpacityConverter()
	{
		Opacity = 1;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Opacity used when ConverterParameter is omitted. Range 0–1.
	/// </summary>
	public double Opacity { get; set; }

	#endregion

	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (!TryGetColor(value, out var color))
		{
			return PresentationProperty.UnsetValue;
		}

		var opacity = TryParseOpacity(parameter, culture) ?? Opacity;
		if (opacity < 0)
		{
			opacity = 0;
		}
		if (opacity > 1)
		{
			opacity = 1;
		}

		var result = Color.FromArgb((byte) Math.Round(opacity * 255), color.R, color.G, color.B);

		if ((targetType == typeof(Color)) || (targetType == typeof(Color?)))
		{
			return result;
		}

		return new SolidColorBrush(result);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return PresentationProperty.UnsetValue;
	}

	private static bool TryGetColor(object value, out Color color)
	{
		switch (value)
		{
			case Color c:
				color = c;
				return true;
			case ISolidColorBrush solid:
				color = solid.Color;
				return true;
			case HslColor hsl:
				color = hsl.ToRgb();
				return true;
			case HsvColor hsv:
				color = hsv.ToRgb();
				return true;
			default:
				color = default;
				return false;
		}
	}

	private static double? TryParseOpacity(object parameter, CultureInfo culture)
	{
		if (parameter == null)
		{
			return null;
		}

		switch (parameter)
		{
			case double d:
				return d;
			case float f:
				return f;
			case decimal m:
				return (double) m;
			case int i:
				return i;
			case string s when double.TryParse(s, NumberStyles.Float, culture ?? CultureInfo.InvariantCulture, out var parsed):
				return parsed;
			default:
				return null;
		}
	}

	#endregion
}
