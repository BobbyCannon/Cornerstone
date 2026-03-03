#region References

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

#endregion

namespace Avalonia.Diagnostics.Converters;

public class GetTypeNameConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Type type)
		{
			return type.GetTypeName();
		}
		return BindingOperations.DoNothing;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return BindingOperations.DoNothing;
	}

	#endregion
}