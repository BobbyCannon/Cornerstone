using System;
using System.Collections.Generic;
using System.Globalization;
using Cornerstone.Presentation.Data.Converters;

namespace Cornerstone.Presentation.Controls.Converters;

public class TreeViewItemIndentConverter : IMultiValueConverter
{
    public static readonly TreeViewItemIndentConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 1 && values[0] is int level && values[1] is double indent)
        {
            return new Thickness(indent * level, 0, 0, 0);
        }

        return new Thickness(0);
    }
}
