#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public class ProgressWidthConverter : IMultiValueConverter
{
	#region Methods

	public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is not [double value, double minimum, double maximum, double totalWidth])
		{
			return 0.0;
		}

		var total = maximum - minimum;
		var normalizedValue = value - minimum;

		if ((total == 0)
			|| (normalizedValue <= 0)
			|| (totalWidth <= 0))
		{
			return 0.0;
		}

		if (normalizedValue >= total)
		{
			return totalWidth;
		}

		var percentage = normalizedValue / total;
		return percentage * totalWidth;
	}

	#endregion
}