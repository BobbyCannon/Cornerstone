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
		if (values is not [double progress, double total])
		{
			return 0.0;
		}

		if (progress <= 0)
		{
			return 0.0;
		}
		if (progress >= 1)
		{
			return total;
		}

		// Clamp to avoid any floating-point overshoot
		var width = Math.Min(progress * total, total);
		return width;
	}

	#endregion
}