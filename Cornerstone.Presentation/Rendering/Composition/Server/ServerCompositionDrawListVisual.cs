using System;
using System.Numerics;
using Cornerstone.Presentation.Collections.Pooled;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.Rendering.Composition.Transport;
using Cornerstone.Presentation.Rendering.SceneGraph;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.Controls.Layout;

namespace Cornerstone.Presentation.Rendering.Composition.Server;

/// <summary>
/// Server-side counterpart of <see cref="CompositionDrawListVisual"/>
/// </summary>
internal class ServerCompositionDrawListVisual : ServerCompositionContainerVisual, IServerRenderResourceObserver
{
#if DEBUG
    // This is needed for debugging purposes so we could see inspect the associated visual from debugger
    public readonly Visual UiVisual;
#endif
    private ServerCompositionRenderData? _renderCommands;
    
    public ServerCompositionDrawListVisual(ServerCompositor compositor, Visual v) : base(compositor)
    {
#if DEBUG
        UiVisual = v;
#endif
    }

    public override LtrbRect? ComputeOwnContentBounds() => _renderCommands?.Bounds;

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        if (reader.Read<byte>() == 1)
        {
            _renderCommands?.Dispose();
            _renderCommands = reader.ReadObject<ServerCompositionRenderData?>();
            _renderCommands?.AddObserver(this);
            InvalidateContent();
        }
        base.DeserializeChangesCore(reader, committedAt);
    }

    protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
    {
        _renderCommands?.Render(context.Canvas);
    }

    public void DependencyQueuedInvalidate(IServerRenderResource sender) => InvalidateContent();
    
#if DEBUG
    public override string ToString()
    {
        return UiVisual.GetType().ToString();
    }
#endif
}
