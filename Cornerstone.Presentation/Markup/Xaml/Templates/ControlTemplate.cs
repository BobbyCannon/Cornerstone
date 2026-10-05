using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Markup.Xaml.Templates
{
    public class ControlTemplate : IControlTemplate
    {
        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        public Type? TargetType { get; set; }

        public TemplateResult<Control>? Build(TemplatedControl control) => TemplateContent.Load(Content);
    }
}
