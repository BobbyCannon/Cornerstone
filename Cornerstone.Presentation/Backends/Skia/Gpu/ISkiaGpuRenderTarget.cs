using System;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Backends.Skia
{
    /// <summary>
    /// Custom Skia render target.
    /// </summary>
    public interface ISkiaGpuRenderTarget : IDisposable
    {
        /// <summary>
        /// Start rendering to this render target.
        /// </summary>
        /// <param name="sceneInfo">Information about the scene that will be rendered.</param>
        /// <returns>A render session instance.</returns>
        ISkiaGpuRenderSession BeginRenderingSession(IRenderTarget.RenderTargetSceneInfo sceneInfo);
        
        PlatformRenderTargetState State => PlatformRenderTargetState.Ready;
    }
}
