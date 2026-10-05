using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform;

[Unstable]
public interface IPlatformGraphics
{
    bool UsesSharedContext { get; }
    IPlatformGraphicsContext CreateContext();
    IPlatformGraphicsContext GetSharedContext();
}

[Unstable]
public interface IPlatformGraphicsWithFeatures : IPlatformGraphics, IOptionalFeatureProvider
{
    
}

[Unstable]
public interface IPlatformGraphicsReadyStateFeature
{
    bool IsReady { get; }
    bool UsesContexts { get; }
}

[Unstable]
public interface IPlatformGraphicsContext : IDisposable, IOptionalFeatureProvider
{
    bool IsLost { get; }
    IDisposable EnsureCurrent();
}

public class PlatformGraphicsContextLostException : Exception
{
    
}