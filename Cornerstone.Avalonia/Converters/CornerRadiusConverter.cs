#region References

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

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

		var response = CornerstoneExtensions.GetBestSingle(corner);
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

	#endregion
}