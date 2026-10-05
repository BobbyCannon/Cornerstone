#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;

#endregion

namespace Cornerstone.Presentation.Diagnostics.Converters;

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