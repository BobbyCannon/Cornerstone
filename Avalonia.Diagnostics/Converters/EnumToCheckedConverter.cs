#region References

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

#endregion

namespace Avalonia.Diagnostics.Converters;

public class EnumToCheckedConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Equals(value, parameter);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool isChecked && isChecked)
		{
			return parameter;
		}

		return BindingOperations.DoNothing;
	}

	#endregion
}