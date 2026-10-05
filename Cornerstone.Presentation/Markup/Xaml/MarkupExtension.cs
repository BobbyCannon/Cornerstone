using System;

namespace Cornerstone.Presentation.Markup.Xaml
{
    public abstract class MarkupExtension
    {
        public abstract object ProvideValue(IServiceProvider serviceProvider);
    }
}
