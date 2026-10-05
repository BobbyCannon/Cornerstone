using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Markup.Xaml.Templates
{
    public class Template : ITemplate<Control?>
    {
        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        public Control? Build() => TemplateContent.Load(Content)?.Result;

        object? ITemplate.Build() => Build();
    }
}
