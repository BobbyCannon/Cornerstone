using Cornerstone.Presentation.Rendering.Composition.Server;

namespace Cornerstone.Presentation.Rendering.Composition;

internal partial class CompositionExperimentalAcrylicVisual
{
    internal CompositionExperimentalAcrylicVisual(Compositor compositor, Visual visual) : base(compositor,
        new ServerCompositionExperimentalAcrylicVisual(compositor.Server, visual), visual)
    {
    }
}