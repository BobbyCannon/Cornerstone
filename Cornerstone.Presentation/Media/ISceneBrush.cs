using System;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering.Composition.Drawing;

namespace Cornerstone.Presentation.Media
{
    [NotClientImplementable]
    public interface ISceneBrush : ITileBrush
    {
        ISceneBrushContent? CreateContent();
    }
    
    [NotClientImplementable]
    public interface ISceneBrushContent : IImmutableBrush, IDisposable
    {
        ITileBrush Brush { get; }
        Rect Rect { get; }
        void Render(IDrawingContextImpl context, Matrix? transform);
        internal bool UseScalableRasterization { get; }
    }

    internal class ImmutableSceneBrush : ImmutableTileBrush
    {
        public ImmutableSceneBrush(ITileBrush source) : base(source)
        {
        }
    }
}
