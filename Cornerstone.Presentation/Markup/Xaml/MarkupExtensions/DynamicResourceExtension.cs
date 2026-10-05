using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Controls.Resources;

namespace Cornerstone.Presentation.Markup.Xaml.MarkupExtensions
{
    public sealed class DynamicResourceExtension : BindingBase
    {
        private object? _anchor;
        private BindingPriority _priority;
        private ThemeVariant? _themeVariant;

        public DynamicResourceExtension()
        {
        }

        public DynamicResourceExtension(object resourceKey)
        {
            ResourceKey = resourceKey;
        }

        [ConstructorArgument("resourceKey")]
        public object? ResourceKey { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider.IsInControlTemplate())
                _priority = BindingPriority.Template;

            var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

            if (provideTarget?.TargetObject is not StyledElement)
            {
                _anchor = serviceProvider.GetFirstParent<StyledElement>() ??
                    serviceProvider.GetFirstParent<IResourceProvider>() ??
                    (object?)serviceProvider.GetFirstParent<IResourceHost>();
            }

            _themeVariant = StaticResourceExtension.GetDictionaryVariant(
                serviceProvider.GetService<ICornerstoneXamlIlParentStackProvider>());

            return this;
        }

        internal override BindingExpressionBase CreateInstance(PresentationObject target, PresentationProperty? targetProperty, object? anchor)
        {
            if (ResourceKey is null)
                throw new InvalidOperationException("DynamicResource must have a ResourceKey.");
            return new DynamicResourceExpression(ResourceKey, _anchor, _themeVariant, _priority);
        }
    }
}
