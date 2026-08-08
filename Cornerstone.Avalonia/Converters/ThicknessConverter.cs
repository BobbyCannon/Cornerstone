#region References

using System;
using System.Globalization;
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
			decimal number => new Thickness((double)number),
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

		return targetType switch
		{
			Type t when t == typeof(decimal) => System.Convert.ToDecimal(response),
			Type t when t == typeof(double) => System.Convert.ToDouble(response),
			Type t when t == typeof(string) => thickness.ToString(),
			_ => response
		};
	}

	#endregion
}