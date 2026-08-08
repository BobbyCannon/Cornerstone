#region References

using System;
using System.Globalization;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public class DateTimeConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType)
	{
		return value switch
		{
			DateTime s when targetType == typeof(DateOnly) => DateOnly.FromDateTime(s),
			DateTime s when targetType == typeof(DateOnly?) => DateOnly.FromDateTime(s),
			
			DateOnly s when targetType == typeof(DateTime) => s.ToDateTime(TimeOnly.MinValue),
			DateOnly s when targetType == typeof(DateTime?) => s.ToDateTime(TimeOnly.MinValue),
			DateOnly s when targetType == typeof(DateTimeOffset) => new DateTimeOffset(s.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
			DateOnly s when targetType == typeof(DateTimeOffset?) => new DateTimeOffset(s.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
			
			DateTimeOffset s when targetType == typeof(DateOnly) => DateOnly.FromDateTime(s.DateTime),
			DateTimeOffset s when targetType == typeof(DateOnly?) => DateOnly.FromDateTime(s.DateTime),
			
			_ => value
		};
	}

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Convert(value, targetType);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Convert(value, targetType);
	}

	#endregion
}