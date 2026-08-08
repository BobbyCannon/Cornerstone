#region References

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Converters;

public class DecimalToDoubleConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value switch
		{
			decimal.MaxValue => double.MaxValue,
			decimal.MinValue => double.MinValue,
			decimal.MinusOne => -1.0,
			decimal.One => 1.0,
			decimal.Zero => 0.0,
			decimal decimalValue => (double) decimalValue,
			_ => targetType.IsNullableType() ? null : 0.0
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not double doubleValue)
		{
			return targetType.IsNullableType() ? null : 0m;
		}

		// Match DoubleToDecimalConverter: NaN/Infinity must not cast to decimal (OverflowException).
		if (double.IsNaN(doubleValue)
			|| double.IsInfinity(doubleValue)
			|| (doubleValue > (double) decimal.MaxValue)
			|| (doubleValue < (double) decimal.MinValue))
		{
			return 0m;
		}

		return (decimal) doubleValue;
	}

	#endregion
}