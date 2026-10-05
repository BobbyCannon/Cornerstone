using System;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Cornerstone.Presentation.Android.Platform.SkiaPlatform;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Android
{
    internal abstract class InvalidationAwareTextureView : TextureView, TextureView.ISurfaceTextureListener, IAndroidRenderView
    {
        private IntPtr _nativeWindowHandle = IntPtr.Zero;
        private Surface _surface;
        private PixelSize _size = new(1, 1);
        private double _scaling = 1;

        public event EventHandler SurfaceWindowCreated;
        public event EventHandler SurfaceWindowDestroyed;

        public PixelSize Size => _size;
        public double Scaling => _scaling;

        IntPtr IPlatformHandle.Handle => _nativeWindowHandle;
        string IPlatformHandle.HandleDescriptor => "TextureView";

        protected InvalidationAwareTextureView(Context context) : base(context)
        {
            SetOpaque(false);
            Clickable = false;
            SurfaceTextureListener = this;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                SurfaceTextureListener = null;

            ReleaseNativeWindow();
            base.Dispose(disposing);
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, $"InvalidationAwareTextureView Available {width} x {height}");
            BindSurface(surface, width, height);
            SurfaceWindowCreated?.Invoke(this, EventArgs.Empty);
            OnRenderSurfaceResized();
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, $"InvalidationAwareTextureView SizeChanged {width} x {height}");
            _size = new PixelSize(Math.Max(1, width), Math.Max(1, height));
            _scaling = Resources?.DisplayMetrics?.Density ?? 1;
            OnRenderSurfaceResized();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, "InvalidationAwareTextureView Destroyed");
            SurfaceWindowDestroyed?.Invoke(this, EventArgs.Empty);
            ReleaseNativeWindow();
            _size = new PixelSize(1, 1);
            return true;
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
        }

        protected virtual void OnRenderSurfaceResized()
        {
        }

        private void BindSurface(SurfaceTexture texture, int width, int height)
        {
            ReleaseNativeWindow();
            _surface = new Surface(texture);
            var handle = AndroidFramebuffer.ANativeWindow_fromSurface(JNIEnv.Handle, _surface.Handle);
            Interlocked.Exchange(ref _nativeWindowHandle, handle);
            _size = new PixelSize(Math.Max(1, width), Math.Max(1, height));
            _scaling = Resources?.DisplayMetrics?.Density ?? 1;
        }

        private void ReleaseNativeWindow()
        {
            if (Interlocked.Exchange(ref _nativeWindowHandle, IntPtr.Zero) is var oldHandle
                && oldHandle != IntPtr.Zero)
            {
                AndroidFramebuffer.ANativeWindow_release(oldHandle);
            }

            _surface?.Release();
            _surface?.Dispose();
            _surface = null;
        }
    }
}