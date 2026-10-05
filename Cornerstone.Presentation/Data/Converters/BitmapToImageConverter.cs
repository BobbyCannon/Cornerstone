#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media.Imaging;

#endregion

namespace Cornerstone.Presentation.Data.Converters;

internal class BitmapToImageConverter : IValueConverter
{
	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Bitmap bm)
		{
			return new Image { Source = bm };
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}
