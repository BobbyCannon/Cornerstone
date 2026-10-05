using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation
{
    public static class StyledElementExtensions
    {
        public static IDisposable BindClass(this StyledElement target, string className, BindingBase source, object anchor) =>
            ClassBindingManager.Bind(target, className, source, anchor);

        public static PresentationProperty GetClassProperty(string className) =>
            ClassBindingManager.GetClassProperty(className);

        internal static bool IsClassesBindingProperty(this PresentationProperty property, [NotNullWhen(true)] out string? classPropertyName) =>
            ClassBindingManager.IsClassesBindingProperty(property, out classPropertyName);
    }
}
