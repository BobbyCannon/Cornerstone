#region References

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

#endregion

namespace Avalonia.Diagnostics.Converters;

public class BoolToImageConverter : IValueConverter
{
	#region Properties

	public IImage FalseImage { get; set; }
	public IImage TrueImage { get; set; }

	#endregion

	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value switch
		{
			true => TrueImage,
			false => FalseImage,
			_ => null
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return BindingOperations.DoNothing;
	}

	#endregion
}