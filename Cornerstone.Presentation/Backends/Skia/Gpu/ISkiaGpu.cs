using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;
using SkiaSharp;
using Cornerstone.Presentation.Controls.Layout;

namespace Cornerstone.Presentation.Backends.Skia
{
    /// <summary>
    /// Custom Skia gpu instance.
    /// </summary>
    internal interface ISkiaGpu : IPlatformGraphicsContext
    {
        /// <summary>
        /// Gets the platform graphics context.
        /// </summary>
        IPlatformGraphicsContext? PlatformGraphicsContext { get; }

        /// <summary>
        /// Attempts to create custom render target from given surfaces.
        /// </summary>
        /// <param name="surfaces">Surfaces.</param>
        /// <returns>Created render target or <see langword="null"/> if it fails.</returns>
        ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces);

        /// <summary>
        /// Checks if a render target can be created for the given surfaces and the preferred surface is ready.
        /// </summary>
        bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces);

        /// <summary>
        /// Creates an offscreen render target surface
        /// </summary>
        /// <param name="size">size in pixels.</param>
        /// <param name="session">An optional custom render session.</param>
        ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session);

        /// <summary>
        /// Tries to get a <see cref="GRContext"/>.
        /// </summary>
        /// <returns>A <see cref="GRContext"/>.</returns>
        IScopedResource<GRContext>? TryGetGrContext();
    }
    
    public interface ISkiaSurface : IDisposable
    {
        SKSurface Surface { get; }
        bool CanBlit { get; }
        void Blit(SKCanvas canvas);
    }
}
