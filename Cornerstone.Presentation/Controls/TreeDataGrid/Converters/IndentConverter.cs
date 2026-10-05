#region References

using System;
using System.Globalization;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Data.Converters;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Converters;

public class IndentConverter : IValueConverter
{
	#region Properties

	public static IndentConverter Instance { get; } = new();

	#endregion

	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int indent)
		{
			return new Thickness(20 * indent, 0, 0, 0);
		}

		return new Thickness();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}