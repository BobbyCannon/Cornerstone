#nullable enable

#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Data.Converters;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;

public class TestValueConverter : IValueConverter
{
	#region Properties

	public string? Append { get; set; }

	#endregion

	#region Methods

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value + Append;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}