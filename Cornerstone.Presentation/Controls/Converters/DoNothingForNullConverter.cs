using System;
using System.Globalization;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;

namespace Cornerstone.Presentation.Controls.Converters
{
    /// <summary>
    /// Converter that will do nothing (not update bound values) when a null value is encountered.
    /// This converter enables binding nullable with non-nullable properties in some scenarios.
    /// </summary>
    public class DoNothingForNullConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return value ?? BindingOperations.DoNothing;
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return value ?? BindingOperations.DoNothing;
        }
    }
}

