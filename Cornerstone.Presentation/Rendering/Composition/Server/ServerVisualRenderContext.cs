using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.Layout;

namespace Cornerstone.Presentation.Rendering.Composition.Server;

internal class ServerVisualRenderContext
{
    public IDrawingContextImpl Canvas { get; }

    public ServerVisualRenderContext(IDrawingContextImpl canvas)
    {
        Canvas = canvas;
    }
}
