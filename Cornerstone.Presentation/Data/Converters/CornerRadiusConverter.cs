#region References

using System;
using System.Globalization;

#endregion

namespace Cornerstone.Presentation.Data.Converters;

public class CornerRadiusConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value switch
		{
			decimal number => new CornerRadius((double) number),
			double number => new CornerRadius(number),
			string text => CornerRadius.Parse(text),
			_ => null
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not CornerRadius corner)
		{
			return null;
		}

		var response = GetBestSingle(corner);
		if (targetType == typeof(decimal))
		{
			return System.Convert.ToDecimal(response);
		}
		if (targetType == typeof(double))
		{
			return System.Convert.ToDouble(response);
		}
		if (targetType == typeof(string))
		{
			return corner.ToString();
		}

		return response;
	}

	private static double GetBestSingle(CornerRadius t)
	{
		if (t.IsUniform)
		{
			return t.TopLeft;
		}

		return (t.TopLeft + t.TopRight + t.BottomLeft + t.BottomRight) / 4.0;
	}

	#endregion
}
