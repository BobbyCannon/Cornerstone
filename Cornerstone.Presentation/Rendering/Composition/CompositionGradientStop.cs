using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering.Composition.Server;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation.Rendering.Composition;

public partial class CompositionGradientStop : IGradientStop
{
    internal CompositionGradientStop(Compositor compositor, ServerCompositionGradientStop server, double offset, Color color) : base(compositor, server)
    {
        Server = server;
        if (MathUtilities.IsZero(offset))
        {
            offset = 0;
        }
        Offset = (offset < 0) ? 0 : (offset > 1) ? 1 : offset;
        Color = color;
        InitializeDefaults();
    }
    partial void InitializeDefaultsExtra()
    {
        Server.Activate();
    }
}
