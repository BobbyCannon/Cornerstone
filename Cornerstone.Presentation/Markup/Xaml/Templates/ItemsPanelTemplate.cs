using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Markup.Xaml.Templates
{
    [ControlTemplateScope]
    public class ItemsPanelTemplate : ITemplate<Panel?>
    {
        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        public Panel? Build() => (Panel?)TemplateContent.Load(Content)?.Result;

        object? ITemplate.Build() => Build();
    }
}
