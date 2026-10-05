using System;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Android.Platform.SkiaPlatform
{
    internal sealed class FramebufferManager : IFramebufferPlatformSurface
    {
        private readonly TopLevelImpl _topLevel;

        public FramebufferManager(TopLevelImpl topLevel)
        {
            _topLevel = topLevel;
        }

        public ILockedFramebuffer Lock() => new AndroidFramebuffer(
            _topLevel.InternalView,
            _topLevel.RenderScaling);

        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
    }
}
