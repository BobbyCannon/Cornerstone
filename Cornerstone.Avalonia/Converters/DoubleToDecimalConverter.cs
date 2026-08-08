#region References

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Converters;

/// <summary>
/// Safe double ↔ decimal for NumericUpDown bindings (Height/Width etc.).
/// Direct cast of NaN/Infinity to decimal throws OverflowException
/// ("Value was either too large or too small for a Decimal").
/// </summary>
public class DoubleToDecimalConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not double doubleValue)
		{
			return targetType.IsNullableType() ? null : 0m;
		}

		// Proven by DoubleToDecimalConverterTests: (decimal)double.NaN throws OverflowException.
		if (double.IsNaN(doubleValue)
			|| double.IsInfinity(doubleValue)
			|| (doubleValue > (double) decimal.MaxValue)
			|| (doubleValue < (double) decimal.MinValue))
		{
			return 0m;
		}

		return (decimal) doubleValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is decimal decimalValue)
		{
			return (double) decimalValue;
		}

		return targetType.IsNullableType() ? null : 0.0;
	}

	#endregion
}