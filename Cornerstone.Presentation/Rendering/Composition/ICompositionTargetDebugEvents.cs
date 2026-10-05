using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Rendering.Composition;

internal interface ICompositionTargetDebugEvents
{
    int RenderedVisuals { get; set; }
    int VisitedVisuals { get; set; }
    void RectInvalidated(LtrbRect rc);
}
