#region References

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Cornerstone.Avalonia.Resources;

#endregion

namespace Cornerstone.Avalonia.Converters;

/// <summary>
/// Maps a daily activity count to a GitHub-style heatmap cell brush.
/// </summary>
public class ActivityLevelConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int count)
		{
			return count switch
			{
				0 => ResourceService.GetBrush("BorderBrush"),
				<= 2 => ResourceService.GetColorAsBrush("Green09"),
				<= 4 => ResourceService.GetColorAsBrush("Green08"),
				<= 6 => ResourceService.GetColorAsBrush("Green07"),
				<= 8 => ResourceService.GetColorAsBrush("Green06"),
				_ => ResourceService.GetColorAsBrush("Green05")
			};
		}

		return Brushes.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}
