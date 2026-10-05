using System;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;
using SkiaSharp;

namespace Cornerstone.Presentation.Backends.Skia;

[Unstable]
public interface ISkiaSharpApiLeaseFeature
{
    public ISkiaSharpApiLease Lease();
}

[Unstable]
public interface ISkiaSharpApiLease : IDisposable
{
    SKCanvas SkCanvas { get; }
    GRContext? GrContext { get; }
    SKSurface? SkSurface { get; }
    double CurrentOpacity { get; }
    ISkiaSharpPlatformGraphicsApiLease? TryLeasePlatformGraphicsApi();
}

[Unstable]
public interface ISkiaSharpPlatformGraphicsApiLease : IDisposable
{
    IPlatformGraphicsContext Context { get; }
}
