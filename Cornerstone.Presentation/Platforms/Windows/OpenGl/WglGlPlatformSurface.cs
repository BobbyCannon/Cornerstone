using System;
using System.Diagnostics;
using Cornerstone.Presentation.OpenGL;
using Cornerstone.Presentation.OpenGL.Egl;
using Cornerstone.Presentation.OpenGL.Surfaces;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platforms.Windows.Interop;
using static Cornerstone.Presentation.OpenGL.GlConsts;
using static Cornerstone.Presentation.Platforms.Windows.Interop.UnmanagedMethods;
namespace Cornerstone.Presentation.Platforms.Windows.OpenGl
{
    class WglGlPlatformSurface: IGlPlatformSurface
    {

        private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
        
        public WglGlPlatformSurface( EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
        {
            _info = info;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            return new RenderTarget((WglContext)context, _info);
        }

        class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly WglContext _context;
            private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
            private IntPtr _hdc;

            public RenderTarget(WglContext context,  EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
            {
                _context = context;
                _info = info;
                _hdc = context.CreateConfiguredDeviceContext(info.Handle);
            }

            public PlatformRenderTargetState State => PlatformRenderTargetState.Ready;

            public void Dispose()
            {
                WglGdiResourceManager.ReleaseDC(_info.Handle, _hdc);
            }

            public IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo)
            {
                // TODO: use expectedPixelSize
                var oldContext = _context.MakeCurrent(_hdc);
                
                // Reset to default FBO first
                _context.GlInterface.BindFramebuffer(GL_FRAMEBUFFER, 0);

                return new Session(_context, _hdc, _info, oldContext);
            }
            
            class Session : IGlPlatformSurfaceRenderingSession
            {
                private readonly WglContext _context;
                private readonly IntPtr _hdc;
                private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
                private readonly IDisposable _clearContext;
                public IGlContext Context => _context;

                public Session(WglContext context, IntPtr hdc, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info,
                    IDisposable clearContext)
                {
                    _context = context;
                    _hdc = hdc;
                    _info = info;
                    _clearContext = clearContext;
                }

                public void Dispose()
                {
                    _context.GlInterface.Flush();
                    UnmanagedMethods.SwapBuffers(_hdc);
                    _clearContext.Dispose();
                }

                public PixelSize Size => _info.Size;
                public double Scaling => _info.Scaling;
                public bool IsYFlipped { get; }
            }
        }
    }
}
