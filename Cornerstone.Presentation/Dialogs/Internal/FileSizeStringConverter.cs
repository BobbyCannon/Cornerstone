using System;
using System.Globalization;
using Cornerstone.Presentation.Data.Converters;

namespace Cornerstone.Presentation.Dialogs.Internal
{
    public class FileSizeStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long size && size > 0)
            {
                return Cornerstone.Presentation.Utilities.ByteSizeHelper.ToString((ulong)size, true);
            }

            return "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
