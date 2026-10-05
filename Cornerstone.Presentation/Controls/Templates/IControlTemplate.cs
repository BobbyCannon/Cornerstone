using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build a <see cref="TemplatedControl"/>.
    /// </summary>
    [ControlTemplateScope]
    public interface IControlTemplate : ITemplate<TemplatedControl, TemplateResult<Control>?>
    {
    }
}
