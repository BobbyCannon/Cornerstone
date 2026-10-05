using System;
using System.Collections.Generic;
using System.Globalization;
using Cornerstone.Presentation;

namespace Cornerstone.Presentation.Data.Converters;

/// <summary>
/// Multiplies all convertible binding values. Used by JoystickControl transforms.
/// </summary>
public class MultiplicationConverter : IMultiValueConverter
{
	public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
	{
		if ((values == null) || (values.Count == 0))
		{
			return 0.0;
		}

		double? result = null;
		foreach (var value in values)
		{
			if ((value == null) || ReferenceEquals(value, PresentationProperty.UnsetValue))
			{
				continue;
			}

			if (value is not IConvertible convertible)
			{
				return 0.0;
			}

			var doubleValue = System.Convert.ToDouble(convertible, culture);
			result = result == null ? doubleValue : result.Value * doubleValue;
		}

		return result ?? 0.0;
	}
}
