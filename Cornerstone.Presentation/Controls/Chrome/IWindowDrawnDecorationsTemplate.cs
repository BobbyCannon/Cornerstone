using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Styling;

namespace Cornerstone.Presentation.Controls.Chrome;

/// <summary>
/// Interface for a template that produces <see cref="WindowDrawnDecorationsContent"/>.
/// Implemented by the XAML template class in Cornerstone.Presentation.Markup.Xaml.
/// Extends <see cref="ITemplate"/> so the XAML compiler assigns the template object directly
/// instead of auto-calling Build().
/// </summary>
[ControlTemplateScope]
public interface IWindowDrawnDecorationsTemplate : ITemplate
{
    /// <summary>
    /// Builds the template and returns the content with its name scope.
    /// </summary>
    new TemplateResult<WindowDrawnDecorationsContent> Build();
}
