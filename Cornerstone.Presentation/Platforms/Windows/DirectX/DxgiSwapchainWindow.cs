using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.Presentation.OpenGL;
using Cornerstone.Presentation.OpenGL.Egl;
using Cornerstone.Presentation.OpenGL.Surfaces;

namespace Cornerstone.Presentation.Platforms.Windows.DirectX
{
    internal class DxgiSwapchainWindow : EglGlPlatformSurfaceBase
    {
        private DxgiConnection _connection;
        private EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _window;

        public DxgiSwapchainWindow(DxgiConnection connection, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo window)
        {
            _connection = connection;
            _window = window;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            var eglContext = (EglContext)context;
            using (eglContext.EnsureCurrent())
            {
                return new DxgiRenderTarget(_window, eglContext, _connection);
            }
        }
    }
}
