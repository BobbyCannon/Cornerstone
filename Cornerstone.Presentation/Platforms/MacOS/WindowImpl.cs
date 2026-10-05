using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Input.TextInput;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using MicroCom.Runtime;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class WindowImpl : WindowBaseImpl, IWindowImpl
    {
        private readonly MacOSPlatformOptions _opts;
        private readonly ICsnWindow _native;
        private double _extendTitleBarHeight = -1;
        private DoubleClickHelper _doubleClickHelper;
        private readonly ITopLevelNativeMenuExporter _nativeMenuExporter;
        private bool _canResize = true;
        private bool _canMaximize = true;
        private Controls.Chrome.WindowDecorations _decorations = Controls.Chrome.WindowDecorations.Full;

        internal WindowImpl(ICornerstoneNativeFactory factory, MacOSPlatformOptions opts) : base(factory)
        {
            _opts = opts;
            _doubleClickHelper = new DoubleClickHelper();
            
            using (var e = new WindowEvents(this))
            {
                Init(new MacOSTopLevelHandle(_native = factory.CreateWindow(e)));
            }

            _nativeMenuExporter = new MacMenuExporter(_native, factory);
        }

        internal sealed override void Init(MacOSTopLevelHandle handle)
        {
            base.Init(handle);
        }

        class WindowEvents : WindowBaseEvents, ICsnWindowEvents
        {
            readonly WindowImpl _parent;

            public WindowEvents(WindowImpl parent) : base(parent)
            {
                _parent = parent;
            }

            int ICsnWindowEvents.Closing()
            {
                if (_parent.Closing != null)
                {
                    return _parent.Closing(WindowCloseReason.WindowClosing).AsComBool();
                }

                return true.AsComBool();
            }

            void ICsnWindowEvents.WindowStateChanged(CsnWindowState state)
            {
                _parent.InvalidateExtendedMargins();

                _parent.WindowStateChanged?.Invoke((WindowState)state);
            }

            void ICsnWindowEvents.GotInputWhenDisabled()
            {
                _parent.GotInputWhenDisabled?.Invoke();
            }
        }
        
        public new ICsnWindow Native => _native;

        public void CanResize(bool value)
        {
            _canResize = value;
            _native.SetCanResize(value.AsComBool());
        }

        public void SetCanMinimize(bool value)
        {
            _native.SetCanMinimize(value.AsComBool());
        }

        public void SetCanMaximize(bool value)
        {
            _canMaximize = value;
            _native.SetCanMaximize(value.AsComBool());
        }

        public void SetWindowDecorations(Controls.Chrome.WindowDecorations enabled)
        {
            _decorations = enabled;
            _native.SetDecorations((Interop.SystemDecorations)enabled);
            InvalidateExtendedMargins();
        }

        public void SetTitleBarColor(Cornerstone.Presentation.Media.Color color)
        {
            _native.SetTitleBarColor(new CsnColor { Alpha = color.A, Red = color.R, Green = color.G, Blue = color.B });
        }

        public void SetTitle(string? title)
        {
            _native.SetTitle(title ?? "");
        }

        public WindowState WindowState
        {
            get => (WindowState)_native.WindowState;
            set => _native.SetWindowState((CsnWindowState)value);
        }

        public bool WindowStateGetterIsUsable => false;
        public Action<WindowState>? WindowStateChanged { get; set; }

        public Action<bool>? ExtendClientAreaToDecorationsChanged { get; set; }

        // Extension is handled by native backend
        public PlatformRequestedDrawnDecoration RequestedDrawnDecorations => default;
        public Thickness ExtendedMargins { get; private set; }

        public Thickness OffScreenMargin { get; } = new Thickness();

        public IntPtr? ZOrder => _native.WindowZOrder;

        private bool _isExtended;
        public bool IsClientAreaExtendedToDecorations => _isExtended;

        public override void Show(bool activate, bool isDialog)
        {
            base.Show(activate, isDialog);
            
            InvalidateExtendedMargins();
        }

        protected override bool ChromeHitTest (RawPointerEventArgs e)
        {
            if(_isExtended)
            {
                if(e.Type == RawPointerEventType.LeftButtonDown)
                {
                    // TODO: Casts like this are evil
                    var source = (PresentationSource?)_inputRoot;
                    var visual = source?.Renderer.HitTestFirst(e.Position, source.RootElement, x =>
                            {
                                if (x is IInputElement ie && (!ie.IsHitTestVisible || !ie.IsEffectivelyVisible))
                                {
                                    return false;
                                }
                                return true;
                            });

                    if (visual == null || WindowDecorationProperties.GetElementRole(visual) == WindowDecorationsElementRole.TitleBar)
                    {
                        if (_doubleClickHelper.IsDoubleClick(e.Timestamp, e.Position))
                        {
                            switch (WindowState)
                            {
                                case WindowState.Maximized or WindowState.FullScreen
                                when _canResize:
                                    WindowState = WindowState.Normal;
                                    break;

                                case WindowState.Normal
                                when _canMaximize:
                                    WindowState = WindowState.Maximized;
                                    break;
                            }
                        }
                        else
                        {
                            _native.BeginMoveDrag();   
                        }
                    }
                }
            }

            return false;
        }
        
        private void InvalidateExtendedMargins()
        {
            if(_native is MicroComProxyBase pb && pb.IsDisposed) 
                return;

            if (WindowState ==  WindowState.FullScreen || !_isExtended || _decorations != Controls.Chrome.WindowDecorations.Full)
            {
                ExtendedMargins = new Thickness();
            }
            else
            {
                ExtendedMargins = new Thickness(0, _extendTitleBarHeight == -1 ? _native.ExtendTitleBarHeight : _extendTitleBarHeight, 0, 0);
            }

            ExtendClientAreaToDecorationsChanged?.Invoke(_isExtended);
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {
            _isExtended = extendIntoClientAreaHint;

            _native.SetExtendClientArea(extendIntoClientAreaHint.AsComBool());

            InvalidateExtendedMargins();
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            _extendTitleBarHeight = titleBarHeight;
            _native.SetExtendTitleBarHeight(titleBarHeight);

            InvalidateExtendedMargins();
        }

        /// <inheritdoc/>
        public bool NeedsManagedDecorations => false;

        public void ShowTaskbarIcon(bool value)
        {
            // NO OP On OSX
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            // NO OP on OSX
        }

        public Func<WindowCloseReason, bool>? Closing { get; set; }

        public void Move(PixelPoint point) => Position = point;

        public override IPopupImpl? CreatePopup() =>
            _opts.OverlayPopups ? null : new PopupImpl(Factory, this);

        public Action? GotInputWhenDisabled { get; set; }

        public void SetParent(IWindowImpl? parent)
        {
            _native.SetParent(((WindowImpl?)parent)?.Native);
        }

        public void SetEnabled(bool enable)
        {
            _native.SetEnabled(enable.AsComBool());

            // Showing a dialog should result in mouse capture being lost. macOS doesn't have the concept of mouse
            // capture, so no we have no OS-level event to hook into. Instead, release the mouse capture when the
            // owner window is disabled. This behavior matches win32, which sends a WM_CANCELMODE message when
            // EnableWindow(hWnd, false) is called from SetEnabled.
            if (!enable && MouseDevice is MouseDevice mouse)
                mouse.PlatformCaptureLost();
        }

        public override object? TryGetFeature(Type featureType)
        {
            if(featureType == typeof(ITextInputMethodImpl))
            {
                return InputMethod;
            } 
            
            if (featureType == typeof(ITopLevelNativeMenuExporter))
            {
                return _nativeMenuExporter;
            }
            
            return base.TryGetFeature(featureType);
        }
    }
}
