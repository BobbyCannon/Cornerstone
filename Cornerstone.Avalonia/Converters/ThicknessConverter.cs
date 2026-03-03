#region References

using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public class ThicknessConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value switch
		{
			decimal number => new Thickness((double) number),
			double number => new Thickness(number),
			string text => Thickness.Parse(text),
			_ => null
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not Thickness thickness)
		{
			return null;
		}

		var response = CornerstoneExtensions.GetBestSingle(thickness);
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
			return thickness.ToString();
		}

		return response;
	}

	#endregion
}