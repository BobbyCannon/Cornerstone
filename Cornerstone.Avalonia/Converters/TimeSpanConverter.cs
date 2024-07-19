#region References

using System;
using System.Globalization;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public class TimeSpanConverter : IValueConverter
{
	#region Methods

	/// <inheritdoc />
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value switch
		{
			TimeSpan sValue => sValue.TotalSeconds,
			_ => value
		};
	}

	/// <inheritdoc />
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value switch
		{
			int sValue => TimeSpan.FromSeconds(sValue),
			uint sValue => TimeSpan.FromSeconds(sValue),
			decimal sValue => TimeSpan.FromSeconds((double) sValue),
			double sValue => TimeSpan.FromSeconds(sValue),
			float sValue => TimeSpan.FromSeconds(sValue),
			_ => value
		};
	}

	#endregion
}