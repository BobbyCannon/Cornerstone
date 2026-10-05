using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Reactive;

namespace Cornerstone.Presentation
{
    internal static class ClassBindingManager
    {
        private const string ClassPropertyPrefix = "__CornerstoneReserved::Classes::";
        private static readonly Dictionary<string, PresentationProperty> s_RegisteredProperties =
            new Dictionary<string, PresentationProperty>();

        public static IDisposable Bind(StyledElement target, string className, BindingBase source, object anchor)
        {
            var prop = GetClassProperty(className);
            return target.Bind(prop, source);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("PresentationProperty", "AVP1001:The same PresentationProperty should not be registered twice",
            Justification = "Classes.attr binding feature is implemented using intermediate cornerstone properties for each class")]
        private static PresentationProperty RegisterClassProxyProperty(string className)
        {
            var prop = PresentationProperty.Register<StyledElement, bool>(ClassPropertyPrefix + className);
            prop.Changed.Subscribe(args =>
            {
                var classes = ((StyledElement)args.Sender).Classes;
                classes.Set(className, args.NewValue.GetValueOrDefault());
            });

            return prop;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static PresentationProperty GetClassProperty(string className)
        {
            var prefixedClassName = ClassPropertyPrefix + className;
            return s_RegisteredProperties.TryGetValue(prefixedClassName, out var property)
                ? property
                : s_RegisteredProperties[prefixedClassName] = RegisterClassProxyProperty(className);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsClassesBindingProperty(PresentationProperty property, [NotNullWhen(true)] out string? classPropertyName)
        {
            
            classPropertyName = default;
            if(property.Name?.StartsWith(ClassPropertyPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                classPropertyName = property.Name.Substring(ClassPropertyPrefix.Length + 1);
                return true;
            }
            return false;
        }
    }
}
