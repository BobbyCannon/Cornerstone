#region References

using System;
using System.Globalization;
using Avalonia.Data.Converters;

#endregion

namespace Avalonia.Diagnostics.Converters;

public class BoolToOpacityConverter : IValueConverter
{
	#region Properties

	public double Opacity { get; set; }

	#endregion

	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool boolean && boolean)
		{
			return 1d;
		}

		return Opacity;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}