using System;
using System.Globalization;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Controls.Converters
{
    /// <summary>
    /// Gets the approximated display name for the color.
    /// </summary>
    public class ColorToDisplayNameConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            Color color;

            if (value is Color valueColor)
            {
                color = valueColor;
            }
            else if (value is HslColor valueHslColor)
            {
                color = valueHslColor.ToRgb();
            }
            else if (value is HsvColor valueHsvColor)
            {
                color = valueHsvColor.ToRgb();
            }
            else if (value is SolidColorBrush valueBrush)
            {
                color = valueBrush.Color;
            }
            else
            {
                // Invalid color value provided
                return PresentationProperty.UnsetValue;
            }

            // ColorHelper.ToDisplayName ignores the alpha component
            // This means fully transparent colors will be named as a real color
            // That undesirable behavior is specially overridden here
            if (color.A == 0x00)
            {
                return PresentationProperty.UnsetValue;
            }
            else
            {
                return ColorHelper.ToDisplayName(color);
            }
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return PresentationProperty.UnsetValue;
        }
    }
}
