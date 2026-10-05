using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Markup.Xaml.Converters
{
    [RequiresUnreferencedCode(TrimmingMessages.XamlTypeResolvedRequiresUnreferenceCodeMessage)]
    public class PresentationPropertyTypeConverter : TypeConverter
    {
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            var registry = PresentationPropertyRegistry.Instance;
            var (ns, owner, propertyName) = PropertyParser.Parse(new CharacterReader(((string)value).AsSpan()));
            var ownerType = TryResolveOwnerByName(context, ns, owner);
            var targetType = context?.GetFirstParent<ControlTemplate>()?.TargetType ??
                context?.GetFirstParent<Style>()?.Selector?.TargetType ??
                typeof(Control);
            var effectiveOwner = ownerType ?? targetType;
            var property = registry.FindRegistered(effectiveOwner, propertyName);

            if (property == null)
            {
                throw new XamlLoadException($"Could not find property '{effectiveOwner.Name}.{propertyName}'.");
            }

            if (effectiveOwner != targetType &&
                !property.IsAttached &&
                !PresentationPropertyRegistry.Instance.IsRegistered(targetType, property))
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Property)?.Log(
                    this,
                    "Property '{Owner}.{Name}' is not registered on '{Type}'.",
                    effectiveOwner,
                    propertyName,
                    targetType);
            }

            return property;
        }

        [RequiresUnreferencedCode(TrimmingMessages.XamlTypeResolvedRequiresUnreferenceCodeMessage)]
        private static Type? TryResolveOwnerByName(ITypeDescriptorContext? context, string? ns, string? owner)
        {
            if (owner != null)
            {
                var result = context?.ResolveType(ns, owner);

                if (result == null)
                {
                    var name = string.IsNullOrEmpty(ns) ? owner : $"{ns}:{owner}";
                    throw new XamlLoadException($"Could not find type '{name}'.");
                }

                return result;
            }

            return null;
        }
    }
}
