#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;

#endregion

namespace Cornerstone.Avalonia.Converters;

public class MultiplicationConverter : IMultiValueConverter
{
	#region Methods

	/// <inheritdoc />
	public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
	{
		if (!targetType.IsAssignableTo(typeof(IConvertible)))
		{
			throw new NotSupportedException($"The conversion target type must implement '{nameof(IConvertible)}'.");
		}

		if (values.Count <= 0)
		{
			return 0;
		}
		double? result = null;
		foreach (var value in values)
		{
			if (value is UnsetValueType
				|| value is CompiledBindingExtension { Source: UnsetValueType })
			{
				continue;
			}
			if (value is IConvertible convertible)
			{
				var doubleValue = System.Convert.ToDouble(convertible, culture);
				if (result is null)
				{
					result = doubleValue;
				}
				else
				{
					result *= doubleValue;
				}
			}
			else
			{
				throw new NotSupportedException($"All values must be of a type that implements '{nameof(IConvertible)}'.");
			}
		}
		if (result is not null)
		{
			return result.Value;
		}
		return 0;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}