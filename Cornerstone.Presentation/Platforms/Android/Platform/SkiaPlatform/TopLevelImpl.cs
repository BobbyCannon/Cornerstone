using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using Cornerstone.Presentation.Android.Platform.Input;
using Cornerstone.Presentation.Android.Platform.Specific.Helpers;
using Cornerstone.Presentation.Android.Platform.Storage;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Input.TextInput;
using Cornerstone.Presentation.OpenGL.Egl;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Rendering.Composition;
using Java.Lang;
using ClipboardManager = Android.Content.ClipboardManager;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Acrylic;
using Cornerstone.Presentation.Controls.Feedback;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Android.Platform.SkiaPlatform
{
    class TopLevelImpl : ITopLevelImpl, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfoWithWaitPolicy
    {
        private readonly Context _context;
        private readonly AndroidKeyboardEventsHelper<TopLevelImpl> _keyboardHelper;
        private readonly AndroidMotionEventsHelper _pointerHelper;
        private readonly AndroidInputMethod<CornerstoneView> _textInputMethod;
        private readonly INativeControlHostImpl _nativeControlHost;
        private readonly IStorageProvider? _storageProvider;
        private readonly AndroidSystemNavigationManagerImpl _systemNavigationManager;
        private readonly AndroidInsetsManager? _insetsManager;
        private readonly Clipboard _clipboard;
        private readonly AndroidLauncher? _launcher;
        private readonly AndroidScreens? _screens;
        private readonly AndroidPlatformFeedback _feedback;
        private View _view;
        private IAndroidRenderView _renderView;
        private WindowTransparencyLevel _transparencyLevel;

        public TopLevelImpl(CornerstoneView cornerstoneView, bool placeOnTop = false)
        {
            if (cornerstoneView.Context is not { } context)
            {
                throw new ArgumentException("CornerstoneView.Context must not be null");
            }

            _context = context;
            if (NativeAirspace.BehindComposition)
            {
                var texture = new TextureViewImpl(context, this);
                _view = texture;
                _renderView = texture;
            }
            else
            {
                var surface = new SurfaceViewImpl(context, this, placeOnTop);
                _view = surface;
                _renderView = surface;
            }
            _textInputMethod = new AndroidInputMethod<CornerstoneView>(cornerstoneView);
            _keyboardHelper = new AndroidKeyboardEventsHelper<TopLevelImpl>(this);
            _pointerHelper = new AndroidMotionEventsHelper(this);
            _clipboard = new Clipboard(new ClipboardImpl(
                context.GetSystemService(Context.ClipboardService).JavaCast<ClipboardManager>(),
                context));
            _screens = new AndroidScreens(context);
            _feedback = new AndroidPlatformFeedback(cornerstoneView);

            if (context is Activity mainActivity)
            {
                _insetsManager = new AndroidInsetsManager(mainActivity, this);
                _storageProvider = new AndroidStorageProvider(mainActivity);
                _launcher = new AndroidLauncher(mainActivity);
            }

            _nativeControlHost = new AndroidNativeControlHostImpl(cornerstoneView);
            _transparencyLevel = WindowTransparencyLevel.None;

            _systemNavigationManager = new AndroidSystemNavigationManagerImpl(context as IActivityNavigationService);

            var gl = new EglGlPlatformSurface(this);
            var framebuffer = new FramebufferManager(this);
            Surfaces = [gl, framebuffer, _renderView];
            Handle = new AndroidViewControlHandle(_view);
        }

        public IInputRoot? InputRoot { get; private set; }

        public Size ClientSize => _renderView?.Size.ToSize(RenderScaling) ?? default;
        public double RenderScaling => _renderView?.Scaling ?? 1;

        public Action? Closed { get; set; }

        public Action<RawInputEventArgs>? Input { get; set; }

        public Action<Rect>? Paint { get; set; }

        public Action<Size, WindowResizeReason>? Resized { get; set; }

        public Action<double>? ScalingChanged { get; set; }

        public View View => _view;

        internal IAndroidRenderView InternalView => _renderView;

        public double DesktopScaling => RenderScaling;
        public IPlatformHandle Handle { get; }

        public IPlatformRenderSurface[] Surfaces { get; }

        public Compositor Compositor => AndroidPlatform.Compositor ??
            throw new InvalidOperationException("Android backend wasn't initialized. Make sure .UseAndroid() was executed.");

        public Point PointToClient(PixelPoint point)
        {
            return point.ToPoint(RenderScaling);
        }

        public PixelPoint PointToScreen(Point point)
        {
            return PixelPoint.FromPoint(point, RenderScaling);
        }

        public void SetCursor(ICursorImpl? cursor)
        {
            //still not implemented
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public virtual void Dispose()
        {
            _systemNavigationManager.Dispose();
            _view.Dispose();
        }

        protected void OnResized(Size size)
        {
            Resized?.Invoke(size, WindowResizeReason.Unspecified);
        }

        internal void Resize(Size size)
        {
            Resized?.Invoke(size, WindowResizeReason.Layout);
        }

        sealed class SurfaceViewImpl : InvalidationAwareSurfaceView
        {
            private readonly TopLevelImpl _tl;
            private Size _oldSize;
            private double _oldScaling;
            private Paint? _clearPaint;

            public SurfaceViewImpl(Context context, TopLevelImpl tl, bool placeOnTop) : base(context)
            {
                _tl = tl;
                if (placeOnTop)
                    SetZOrderOnTop(true);
            }

            protected override void DispatchDraw(global::Android.Graphics.Canvas canvas)
            {
                // Workaround issue #9230 on where screen remains gray after splash screen.
                // base.DispatchDraw should punch a hole into the canvas so the surface
                // can be seen below, but it does not.
                if (OperatingSystem.IsAndroidVersionAtLeast(29))
                {
                    if (_clearPaint == null)
                    {
                        _clearPaint = new Paint();
                        _clearPaint.SetColor(0);
                        _clearPaint.BlendMode = BlendMode.Clear;
                    }
                    canvas.DrawRect(0, 0, Width, Height, _clearPaint);
                }
                else
                {
                    // Android 9 did this
                    canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear!);
                }

                base.DispatchDraw(canvas);
            }

            public override void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                base.SurfaceChanged(holder, format, width, height);

                var newSize = Size.ToSize(Scaling);
                var newScaling = Scaling;

                if (newSize != _oldSize)
                {
                    _oldSize = newSize;
                    _tl.OnResized(newSize);
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (newScaling != _oldScaling)
                {
                    _oldScaling = newScaling;
                    _tl.ScalingChanged?.Invoke(newScaling);
                }
            }

            public override void SurfaceRedrawNeeded(ISurfaceHolder holder)
            {
                // Compositor Renderer handles Paint event in-sync, which is perfect for sync SurfaceRedrawNeeded
                _tl.Paint?.Invoke(new Rect(new Point(), Size.ToSize(Scaling)));
                base.SurfaceRedrawNeeded(holder);
            }

            public override void SurfaceRedrawNeededAsync(ISurfaceHolder holder, IRunnable drawingFinished)
            {
                _tl.Compositor.RequestCompositionUpdate(drawingFinished.Run);
                base.SurfaceRedrawNeededAsync(holder, drawingFinished);
            }
        }

        sealed class TextureViewImpl : InvalidationAwareTextureView
        {
            private readonly TopLevelImpl _tl;
            private Size _oldSize;
            private double _oldScaling;

            public TextureViewImpl(Context context, TopLevelImpl tl) : base(context)
            {
                _tl = tl;
            }

            protected override void OnRenderSurfaceResized()
            {
                var newSize = Size.ToSize(Scaling);
                var newScaling = Scaling;

                if (newSize != _oldSize)
                {
                    _oldSize = newSize;
                    _tl.OnResized(newSize);
                }

                if (newScaling != _oldScaling)
                {
                    _oldScaling = newScaling;
                    _tl.ScalingChanged?.Invoke(newScaling);
                }

                _tl.Paint?.Invoke(new Rect(new Point(), newSize));
            }
        }

        public IPopupImpl? CreatePopup() => null;

        public Action? LostFocus { get; set; }
        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public WindowTransparencyLevel TransparencyLevel
        {
            get => _transparencyLevel;
            private set
            {
                if (_transparencyLevel != value)
                {
                    _transparencyLevel = value;
                    TransparencyLevelChanged?.Invoke(value);
                }
            }
        }

        public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
        {
            if (_insetsManager != null)
            {
                _insetsManager.SystemBarTheme = themeVariant switch
                {
                    PlatformThemeVariant.Light => SystemBarTheme.Light,
                    PlatformThemeVariant.Dark => SystemBarTheme.Dark,
                    _ => null,
                };
            }

            if (_context is AppCompatActivity activity)
            {
                var nightMode = themeVariant == PlatformThemeVariant.Dark ?
                    AppCompatDelegate.ModeNightYes :
                    AppCompatDelegate.ModeNightNo;

                // Don't use AppCompatDelegate.DefaultNightMode: doing so will force the app to use one night mode,
                // ignoring the system's configuration and preventing us from detecting system theme changes.
                activity.Delegate.SetLocalNightMode(nightMode);
            }
        }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new(1, 1, 1);

        IntPtr EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Handle => _renderView?.Handle ?? default;
        bool EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfoWithWaitPolicy.SkipWaits => true;
        PixelSize EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Size => _renderView?.Size ?? default;
        double EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Scaling => _renderView?.Scaling ?? default;

        internal AndroidInsetsManager? InsetsManager => _insetsManager;
        internal AndroidKeyboardEventsHelper<TopLevelImpl> KeyboardHelper => _keyboardHelper;
        internal AndroidMotionEventsHelper PointerHelper => _pointerHelper;

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
        {
            if (_view?.Context is not CornerstoneActivity activity)
                return;

            foreach (var level in transparencyLevels)
            {
                if (!IsSupported(level))
                {
                    continue;
                }

                if (level == TransparencyLevel)
                {
                    return;
                }

                if (level == WindowTransparencyLevel.None)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    {
                        activity.SetTranslucent(false);
                    }

                    activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.White));
                }
                else if (level == WindowTransparencyLevel.Transparent)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    {
                        activity.SetTranslucent(true);
                        SetBlurBehind(activity, 0);
                        activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                    }
                }
                else if (level == WindowTransparencyLevel.Blur)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(31))
                    {
                        activity.SetTranslucent(true);
                        SetBlurBehind(activity, 120);
                        activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                    }
                }

                TransparencyLevel = level;
                return;
            }

            // If we get here, we didn't find a supported level. Use the default of None.
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                activity.SetTranslucent(false);
            }

            activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.White));
        }

        public virtual object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IStorageProvider))
            {
                return _storageProvider;
            }

            if (featureType == typeof(ITextInputMethodImpl))
            {
                return _textInputMethod;
            }

            if (featureType == typeof(ISystemNavigationManagerImpl))
            {
                return _systemNavigationManager;
            }

            if (featureType == typeof(INativeControlHostImpl))
            {
                return _nativeControlHost;
            }

            if (featureType == typeof(IInsetsManager) || featureType == typeof(IInputPane))
            {
                return _insetsManager;
            }

            if (featureType == typeof(IClipboard))
            {
                return _clipboard;
            }

            if (featureType == typeof(ILauncher))
            {
                return _launcher;
            }

            if (featureType == typeof(IScreenImpl))
            {
                return _screens;
            }

            if(featureType == typeof(IPlatformFeedback))
            {
                return _feedback;
            }
            return null;
        }

        private static bool IsSupported(WindowTransparencyLevel level)
        {
            if (level == WindowTransparencyLevel.None)
                return true;
            if (level == WindowTransparencyLevel.Transparent)
                return OperatingSystem.IsAndroidVersionAtLeast(30);
            if (level == WindowTransparencyLevel.Blur)
                return OperatingSystem.IsAndroidVersionAtLeast(31);
            return false;
        }

        private static void SetBlurBehind(CornerstoneActivity activity, int radius)
        {
            if (radius == 0)
                activity.Window?.ClearFlags(WindowManagerFlags.BlurBehind);
            else
                activity.Window?.AddFlags(WindowManagerFlags.BlurBehind);

            if (OperatingSystem.IsAndroidVersionAtLeast(31) && activity.Window?.Attributes is { } attr)
            {
                attr.BlurBehindRadius = radius;
                activity.Window.Attributes = attr;
            }
        }

        internal void TextInput(string text)
        {
            if (Input != null)
            {
                var args = new RawTextInputEventArgs(AndroidKeyboardDevice.Instance!, (ulong)SystemClock.UptimeMillis(), InputRoot!, text);

                Input(args);
            }
        }
    }
}
