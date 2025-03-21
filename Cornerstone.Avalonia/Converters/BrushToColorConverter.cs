#region References

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Converters;

public class BrushToColorConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is SolidColorBrush brush)
		{
			return brush.Color;
		}

		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Color color)
		{
			return new SolidColorBrush(color);
		}

		return value;
	}

	#endregion
}