using System;
using System.ComponentModel;
using System.Globalization;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Markup.Xaml.Converters
{
    public class FontFamilyTypeConverter : TypeConverter
    {
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            var s = (string)value;

            return FontFamily.Parse(s, context?.GetContextBaseUri());
        }
    }
}
