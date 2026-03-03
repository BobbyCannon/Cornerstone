#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public class PercentWidthConverter : IMultiValueConverter
{
	#region Methods

	public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is not [double value, double totalWidth])
		{
			return 0.0;
		}

		var calculated = value * totalWidth;
		var rounded = Math.Round(calculated / 2.0, MidpointRounding.ToEven) * 2.0;
		return rounded;
	}

	#endregion
}