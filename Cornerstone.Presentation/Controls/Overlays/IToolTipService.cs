using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Controls.Overlays;

[Unstable, PrivateApi]
internal interface IToolTipService
{
    void Update(IInputRoot root, Visual? candidateToolTipHost);
}
