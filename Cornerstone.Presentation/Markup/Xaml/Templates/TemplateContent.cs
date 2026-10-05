using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Markup.Xaml.Templates
{
    public static class TemplateContent
    {
        public static TemplateResult<Control>? Load(object? templateContent)
            => Load<Control>(templateContent);

        public static TemplateResult<T>? Load<T>(object? templateContent)
        {
            using (AppBootstrap.StartupProfiler.Accumulate("Template.Build.Deferred"))
            {
                return templateContent switch
                {
                    IDeferredContent deferred => (TemplateResult<T>?)deferred.Build(null),
                    Func<IServiceProvider?, object?> deferred => (TemplateResult<T>?)deferred(null),
                    null => null,
                    _ => throw new ArgumentException($"Unexpected content {templateContent.GetType()}", nameof(templateContent))
                };
            }
        }
    }
}
