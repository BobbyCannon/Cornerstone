using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Input.TextInput;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Presentation.Platform.Storage.FileIO;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Presentation.Controls.Acrylic;
using Cornerstone.Presentation.Controls.Scrolling;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Platforms.MacOS;

internal class MacOSTopLevelHandle : IPlatformHandle, IMacOSTopLevelPlatformHandle
{
    internal MacOSTopLevelHandle(ICsnTopLevel native)
    {
        Native = native;

        HandleDescriptor = "NSView";

        Handle = NSView;
    }

    internal MacOSTopLevelHandle(ICsnWindowBase native)
    {
        Native = native;

        HandleDescriptor = "NSWindow";

        Handle = NSWindow;
    }

    internal ICsnTopLevel Native { get; }

    public IntPtr Handle { get; }

    public string HandleDescriptor { get; }

    public IntPtr NSView => Native.ObtainNSViewHandle();

    public IntPtr GetNSViewRetained()
    {
        return Native.ObtainNSViewHandleRetained();
    }

    public IntPtr NSWindow => (Native as ICsnWindowBase)?.ObtainNSWindowHandle() ?? IntPtr.Zero;

    public IntPtr GetNSWindowRetained()
    {
        return (Native as ICsnWindowBase)?.ObtainNSWindowHandleRetained() ?? IntPtr.Zero;
    }
}

internal class TopLevelImpl : ITopLevelImpl, IFramebufferPlatformSurface
{
    protected IInputRoot? _inputRoot;
    private NativeControlHostImpl? _nativeControlHost;
    private PlatformBehaviorInhibition? _platformBehaviorInhibition;

    private readonly MouseDevice _mouse;
    private readonly PenDevice _pen;

    private readonly IKeyboardDevice? _keyboard;
    private readonly ICursorFactory? _cursorFactory;

    protected readonly ICornerstoneNativeFactory Factory;

    private Size _savedLogicalSize;
    private double _savedScaling;
    private WindowTransparencyLevel _transparencyLevel = WindowTransparencyLevel.None;

    protected MacOSTopLevelHandle? _handle;

    private object _syncRoot = new object();
    private IPlatformRenderSurface[]? _surfaces;

    public TopLevelImpl(ICornerstoneNativeFactory factory)
    {
        Factory = factory;

        _keyboard = PresentationLocator.Current.GetService<IKeyboardDevice>();
        _mouse = Cornerstone.Presentation.Input.MouseDevice.Primary;
        _pen = new PenDevice();
        _cursorFactory = PresentationLocator.Current.GetService<ICursorFactory>();
    }

    internal virtual void Init(MacOSTopLevelHandle handle)
    {
        _handle = handle;
        _savedLogicalSize = ClientSize;
        _savedScaling = Native?.Scaling ?? 1;
        _nativeControlHost = new NativeControlHostImpl(Native!.CreateNativeControlHost());
        _platformBehaviorInhibition = new PlatformBehaviorInhibition(Factory.CreatePlatformBehaviorInhibition());
        _surfaces = [new GlPlatformSurface(Native), new MetalPlatformSurface(Native), this];
        InputMethod = new MacTextInputMethod(Native);
    }

    internal void BeginDraggingSession(
        CsnDragDropEffects effects,
        CsnPoint point,
        ICsnClipboardDataSource source,
        ICsnDndResultCallback callback,
        IntPtr sourceHandle)
    {
        Native?.BeginDragAndDropOperation(effects, point, source, callback, sourceHandle);
    }

    public double DesktopScaling => 1;

    public ICsnTopLevel? Native => _handle?.Native;
    public IPlatformHandle? Handle => _handle;
    public MacTextInputMethod? InputMethod { get; protected set; }
    public Size ClientSize
    {
        get
        {
            if (Native == null)
            {
                return default;
            }

            var s = Native.ClientSize;
            return new Size(s.Width, s.Height);

        }
    }

    public double RenderScaling => _savedScaling;
    public IPlatformRenderSurface[] Surfaces => _surfaces ?? [];
    public Action<RawInputEventArgs>? Input { get; set; }
    public Action<Rect>? Paint { get; set; }
    public Action<Size, WindowResizeReason>? Resized { get; set; }
    public Action<double>? ScalingChanged { get; set; }
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
    public Compositor Compositor => MacPlatform.Compositor;
    public Action? Closed { get; set; }
    public Action? LostFocus { get; set; }

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

    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 0, 0);
    public virtual void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
    {
        //noop
    }

    public IMouseDevice? MouseDevice => _mouse;

    public INativeControlHostImpl? NativeControlHost => _nativeControlHost;

    public AutomationPeer? GetAutomationPeer()
    {
        return _inputRoot?.FocusRoot is Control c ? ControlAutomationPeer.CreatePeerForElement(c) : null;
    }

    public bool RawTextInputEvent(ulong timeStamp, string text)
    {
        if (_inputRoot is null)
            return false;

        if (_keyboard is null)
        {
            return false;
        }

        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

        var args = new RawTextInputEventArgs(_keyboard, timeStamp, _inputRoot, text);

        Input?.Invoke(args);

        return args.Handled;
    }

    public bool RawKeyEvent(
        CsnRawKeyEventType type,
        ulong timeStamp,
        CsnInputModifiers modifiers,
        CsnKey key,
        CsnPhysicalKey physicalKey,
        string keySymbol)
    {
        if (_inputRoot is null)
            return false;

        if (_keyboard is null)
        {
            return false;
        }

        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

        var args = new RawKeyEventArgs(
            _keyboard,
            timeStamp,
            _inputRoot,
            (RawKeyEventType)type,
            (Key)key,
            (RawInputModifiers)modifiers,
            (PhysicalKey)physicalKey,
            keySymbol);

        Input?.Invoke(args);

        return args.Handled;
    }

    public void RawMouseEvent(CsnRawMouseEventType type, CsnPointerDeviceType deviceType, ulong timeStamp, CsnInputModifiers modifiers, CsnPoint point, CsnVector delta, float pressure, float xTilt, float yTilt)
    {
        if (_inputRoot is null)
            return;

        if (_mouse is null)
        {
            return;
        }

        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

        switch (type)
        {
            case CsnRawMouseEventType.Wheel:
                Input?.Invoke(new RawMouseWheelEventArgs(_mouse, timeStamp, _inputRoot,
                    point.ToPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                break;

            case CsnRawMouseEventType.Magnify:
                Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Magnify,
                    point.ToPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                break;

            case CsnRawMouseEventType.Rotate:
                Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Rotate,
                    point.ToPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                break;

            case CsnRawMouseEventType.Swipe:
                Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Swipe,
                    point.ToPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                break;

            default:
                IInputDevice device = deviceType == CsnPointerDeviceType.Pen && _pen != null ? _pen : _mouse;
                var e = new RawPointerEventArgs(device, timeStamp, _inputRoot, (RawPointerEventType)type,
                    new RawPointerPoint
                    {
                        Position = point.ToPoint(),
                        Pressure = pressure,
                        XTilt = xTilt,
                        YTilt = yTilt
                    }, (RawInputModifiers)modifiers);

                if (!ChromeHitTest(e))
                {
                    Input?.Invoke(e);
                }
                break;
        }
    }

    public void Invalidate()
    {
        Native?.Invalidate();
    }

    public void SetInputRoot(IInputRoot inputRoot)
    {
        _inputRoot = inputRoot;
    }

    public Point PointToClient(PixelPoint point)
    {
        return Native?.PointToClient(point.ToCsnPoint()).ToPoint() ?? default;
    }

    public PixelPoint PointToScreen(Point point)
    {
        return Native?.PointToScreen(point.ToCsnPoint()).ToPixelPoint() ?? default;
    }

    public void SetCursor(ICursorImpl? cursor)
    {
        if (Native == null)
        {
            return;
        }

        var newCursor = cursor as MacCursor;
        newCursor ??= (_cursorFactory?.GetCursor(StandardCursorType.Arrow) as MacCursor);
        Native.SetCursor(newCursor?.Cursor);
    }

    public virtual IPopupImpl? CreatePopup()
    {
        return new PopupImpl(Factory, this);
    }

    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
    {
        foreach (var level in transparencyLevels)
        {
            CsnWindowTransparencyMode? mode = null;

            if (level == WindowTransparencyLevel.None)
                mode = CsnWindowTransparencyMode.Opaque;
            if (level == WindowTransparencyLevel.Transparent)
                mode = CsnWindowTransparencyMode.Transparent;
            else if (level == WindowTransparencyLevel.AcrylicBlur)
                mode = CsnWindowTransparencyMode.Blur;

            if (mode.HasValue && level != TransparencyLevel)
            {
                Native?.SetTransparencyMode(mode.Value);
                TransparencyLevel = level;
                return;
            }
        }

        // If we get here, we didn't find a supported level. Use the default of None.
        if (TransparencyLevel != WindowTransparencyLevel.None)
        {
            Native?.SetTransparencyMode(CsnWindowTransparencyMode.Opaque);
            TransparencyLevel = WindowTransparencyLevel.None;
        }
    }

    public virtual object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(ITextInputMethodImpl))
        {
            return InputMethod;
        }

        if (featureType == typeof(INativeControlHostImpl))
        {
            return _nativeControlHost;
        }

        if (featureType == typeof(IPlatformBehaviorInhibition))
        {
            return _platformBehaviorInhibition;
        }

        if (featureType == typeof(IClipboard))
        {
            return PresentationLocator.Current.GetRequiredService<IClipboard>();
        }

        if (featureType == typeof(IScreenImpl))
        {
            return PresentationLocator.Current.GetRequiredService<IScreenImpl>();
        }

        if (featureType == typeof(ILauncher))
        {
            return new BclLauncher();
        }

        return null;
    }

    public virtual void Dispose()
    {
        Native?.Dispose();
        _handle = null;

        _nativeControlHost?.Dispose();
        _nativeControlHost = null;
    }

    protected virtual bool ChromeHitTest(RawPointerEventArgs e)
    {
        return false;
    }

    IFramebufferRenderTarget IFramebufferPlatformSurface.CreateFramebufferRenderTarget()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            throw new RenderTargetNotReadyException();

        var nativeRenderTarget = Native?.CreateSoftwareRenderTarget();

        if (nativeRenderTarget is null)
        {
            throw new RenderTargetNotReadyException();
        }

        return new FramebufferRenderTarget(this, nativeRenderTarget);
    }

    protected internal unsafe class TopLevelEvents : NativeCallbackBase, ICsnTopLevelEvents
    {
        private readonly TopLevelImpl _parent;

        public TopLevelEvents(TopLevelImpl parent)
        {
            _parent = parent;
        }

        void ICsnTopLevelEvents.Closed()
        {
            var n = _parent.Native;

            try
            {
                _parent?.Closed?.Invoke();
            }
            finally
            {

                _parent?.Dispose();
                n?.Dispose();
            }
        }

        void ICsnTopLevelEvents.Paint()
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.UiThreadRender);
            var s = _parent.ClientSize;
            _parent.Paint?.Invoke(new Rect(0, 0, s.Width, s.Height));
        }

        void ICsnTopLevelEvents.Resized(CsnSize* size, CsnPlatformResizeReason reason)
        {
            if (_parent?.Native == null)
            {
                return;
            }

            var s = new Size(size->Width, size->Height);
            _parent._savedLogicalSize = s;
            _parent.Resized?.Invoke(s, (WindowResizeReason)reason);
        }

        void ICsnTopLevelEvents.RawMouseEvent(CsnRawMouseEventType type, CsnPointerDeviceType pointerDeviceType, ulong timeStamp, CsnInputModifiers modifiers, CsnPoint point, CsnVector delta, float pressure, float xTilt, float yTilt)
        {
            _parent.RawMouseEvent(type, pointerDeviceType, timeStamp, modifiers, point, delta, pressure, xTilt, yTilt);
        }

        int ICsnTopLevelEvents.RawKeyEvent(CsnRawKeyEventType type, ulong timeStamp, CsnInputModifiers modifiers, CsnKey key, CsnPhysicalKey physicalKey, string keySymbol)
        {
            return _parent.RawKeyEvent(type, timeStamp, modifiers, key, physicalKey, keySymbol).AsComBool();
        }

        int ICsnTopLevelEvents.RawTextInputEvent(ulong timeStamp, string text)
        {
            return _parent.RawTextInputEvent(timeStamp, text).AsComBool();
        }

        void ICsnTopLevelEvents.ScalingChanged(double scaling)
        {
            _parent._savedScaling = scaling;
            _parent.ScalingChanged?.Invoke(scaling);
        }

        void ICsnTopLevelEvents.RunRenderPriorityJobs()
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.UiThreadRender);
        }

        void ICsnTopLevelEvents.LostFocus()
        {
            _parent.LostFocus?.Invoke();

            // macOS doesn't have the concept of mouse capture. If we're losing the focus during an implicit capture
            // (standard mouse down), we should release it to avoid mouse events going to an old window.
            var mouse = _parent._mouse;
            var captured = mouse.Pointer.Captured;

            if (captured is not null &&
                mouse.Pointer.CaptureSource == CaptureSource.Implicit &&
                TopLevel.GetTopLevel(captured as Visual)?.PlatformImpl == _parent)
            {
                mouse.PlatformCaptureLost();
            }
        }

        CsnDragDropEffects ICsnTopLevelEvents.DragEvent(CsnDragEventType type, CsnPoint position,
            CsnInputModifiers modifiers,
            CsnDragDropEffects effects,
            ICsnClipboard clipboard, IntPtr dataTransferHandle)
        {
            var device = PresentationLocator.Current.GetService<IDragDropDevice>();

            if (device is null)
            {
                return CsnDragDropEffects.None;
            }

            if (_parent._inputRoot is null)
            {
                return CsnDragDropEffects.None;
            }

            IDataTransfer? dataTransfer = null;
            if (dataTransferHandle != IntPtr.Zero)
                dataTransfer = GCHandle.FromIntPtr(dataTransferHandle).Target as IDataTransfer;

            using var clipboardDataTransfer = new ClipboardDataTransfer(
                new ClipboardReadSession(clipboard, clipboard.ChangeCount, ownsNative: true));
            dataTransfer ??= clipboardDataTransfer;

            var args = new RawDragEvent(
                device,
                (RawDragEventType)type,
                _parent._inputRoot,
                position.ToPoint(),
                dataTransfer,
                (DragDropEffects)effects,
                (RawInputModifiers)modifiers);
            _parent.Input?.Invoke(args);
            return (CsnDragDropEffects)args.Effects;
        }

        ICsnAutomationPeer? ICsnTopLevelEvents.AutomationPeer
        {
            get
            {
                var native = _parent.GetAutomationPeer();

                return native is null ? null : CsnAutomationPeer.Wrap(native);
            }
        }

        int ICsnTopLevelEvents.HitTestNativeAirspaceHole(CsnPoint point)
        {
            if (!NativeAirspace.BehindComposition)
                return 0;
            if (_parent._inputRoot is not { } root)
                return 0;
            var hit = root.RootElement.InputHitTest(point.ToPoint());
            return NativeAirspace.IsHoleHit(hit) ? 1 : 0;
        }

        int ICsnTopLevelEvents.HitTestNativeAirspaceWheel(CsnPoint point)
        {
            if (!NativeAirspace.BehindComposition)
                return 0;
            var pt = point.ToPoint();
            if (_parent._inputRoot is { } root)
            {
                var hit = root.RootElement.InputHitTest(pt);
                if (hit is Visual visual
                    && visual.FindAncestorOfType<ScrollViewer>(includeSelf: true) != null)
                    return 0;
            }
            foreach (var hole in NativeAirspace.GetHoles(_parent))
            {
                if (!hole.IsEmpty && hole.Bounds.Contains(pt))
                    return 1;
            }
            return 0;
        }
    }

    private class FramebufferRenderTarget : IFramebufferRenderTarget
    {
        private readonly TopLevelImpl _parent;
        private ICsnSoftwareRenderTarget? _target;

        public FramebufferRenderTarget(TopLevelImpl parent, ICsnSoftwareRenderTarget target)
        {
            _parent = parent;
            _target = target;
        }

        public void Dispose()
        {
            lock (_parent._syncRoot)
            {
                _target?.Dispose();
                _target = null;
            }
        }

        
        public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties)
        {
            ObjectDisposedException.ThrowIf(_target is null, this);
            properties = default;
            var w = Math.Max(_parent._savedLogicalSize.Width * _parent._savedScaling, 1);
            var h = Math.Max(_parent._savedLogicalSize.Height * _parent._savedScaling, 1);
            var dpi = _parent._savedScaling * 96;
            return new DeferredFramebuffer(_target, cb =>
            {
                lock (_parent._syncRoot)
                {
                    if (_parent.Native != null && _target != null)
                    {
                        cb(_parent.Native);
                    }
                }
            }, (int)w, (int)h, new Vector(dpi, dpi));
        }

        public bool RetainsFrameContents => false;
    }
}
