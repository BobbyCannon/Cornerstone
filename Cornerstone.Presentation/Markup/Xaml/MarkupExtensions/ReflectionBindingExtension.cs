using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Controls.Naming;

namespace Cornerstone.Presentation.Markup.Xaml.MarkupExtensions
{
    [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TrimmingMessages.ReflectionBindingRequiresDynamicCodeMessage)]
    public sealed class ReflectionBindingExtension : ReflectionBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        public ReflectionBindingExtension() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        public ReflectionBindingExtension(string path) : base(path) { }

        public ReflectionBinding ProvideValue(IServiceProvider serviceProvider)
        {
            return new ReflectionBinding
            {
                TypeResolver = serviceProvider.ResolveType,
                Converter = Converter,
                ConverterCulture = ConverterCulture,
                ConverterParameter = ConverterParameter,
                ElementName = ElementName,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Path = Path,
                Priority = Priority,
                Delay = Delay,
                Source = Source,
                StringFormat = StringFormat,
                RelativeSource = RelativeSource,
                DefaultAnchor = new WeakReference(serviceProvider.GetDefaultAnchor()),
                TargetNullValue = TargetNullValue,
                NameScope = new WeakReference<INameScope?>(serviceProvider.GetService<INameScope>()),
                UpdateSourceTrigger = UpdateSourceTrigger,
            };
        }
    }
}
