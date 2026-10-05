using System;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation.Media;

public interface IImmutableGlyphRunReference : IDisposable
{
    internal IRef<IGlyphRunImpl>? GlyphRun { get; }
}

internal class ImmutableGlyphRunReference : IImmutableGlyphRunReference
{
    public ImmutableGlyphRunReference(IRef<IGlyphRunImpl>? glyphRun)
    {
        GlyphRun = glyphRun;
    }

    public IRef<IGlyphRunImpl>? GlyphRun { get; private set; }
    public void Dispose()
    {
        GlyphRun?.Dispose();
        GlyphRun = null;
    }
}