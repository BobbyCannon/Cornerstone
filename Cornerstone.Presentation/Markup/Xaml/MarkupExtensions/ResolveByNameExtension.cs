using System;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Controls.Naming;

namespace Cornerstone.Presentation.Markup.Xaml.MarkupExtensions
{
    public class ResolveByNameExtension
    {
        public ResolveByNameExtension(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public object? ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider, Name);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? ProvideValue(IServiceProvider serviceProvider, string name)
        {
            var nameScope = serviceProvider.GetService<INameScope>();

            if (nameScope is null)
                return null;

            var value = nameScope.FindAsync(name);

            if(value.IsCompleted)
                return value.GetResult();

            var provideValueTarget = serviceProvider.GetService<IProvideValueTarget>();

            if (provideValueTarget?.TargetProperty is IPropertyInfo property)
            {
                var target = provideValueTarget.TargetObject;
                value.OnCompleted(() => property.Set(target, value.GetResult()));
            }

            return PresentationProperty.UnsetValue;
        }
    }
}
