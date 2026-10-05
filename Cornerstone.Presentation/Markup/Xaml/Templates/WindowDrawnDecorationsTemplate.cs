using System;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Styling;

namespace Cornerstone.Presentation.Markup.Xaml.Templates;

[ControlTemplateScope]
public class WindowDrawnDecorationsTemplate : IWindowDrawnDecorationsTemplate, ITemplate
{
    [Content]
    [TemplateContent(TemplateResultType = typeof(WindowDrawnDecorationsContent))]
    public object? Content { get; set; }

    public TemplateResult<WindowDrawnDecorationsContent> Build() =>
        TemplateContent.Load<WindowDrawnDecorationsContent>(Content)
        ?? throw new InvalidOperationException();

    object? ITemplate.Build() => Build().Result;
}
