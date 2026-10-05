using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.VisualTree;

namespace Cornerstone.Presentation.Controls
{
    public class Window<T> : Window
        where T : class
    {
        public static readonly StyledProperty<T> ViewModelProperty =
            PresentationProperty.Register<Window<T>, T>(nameof(ViewModel));

        public Window()
        {
            if (!Design.IsDesignMode)
            {
                return;
            }

			// ReSharper disable once VirtualMemberCallInConstructor
			var designData = CreateDesignData();
            if (designData != null)
            {
                DataContext ??= designData;
            }

            DesignLifecycleHook?.Invoke(DataContext);
        }

        public Window(T viewModel) : this()
        {
            DataContext = viewModel;
        }

        public T ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        protected override Type StyleKeyOverride => typeof(Window);

        protected virtual T CreateDesignData()
        {
            return DesignDataFactory?.Invoke(typeof(T)) as T;
        }

        protected override object GetViewModel()
        {
            return ViewModel;
        }

        protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
        {
            if ((change.Property == DataContextProperty)
                && DataContext is T viewModel)
            {
                ViewModel = viewModel;
            }

            if (change.Property == ViewModelProperty)
            {
                ViewModelChangedHook?.Invoke(
                    this, change.OldValue, change.NewValue, DataContext, VisualRoot != null);
            }

            base.OnPropertyChanged(change);
        }
    }

    /// <summary>
    /// A top-level window.
    /// </summary>
    public class Window : WindowBase, IFocusScope
    {
        private static readonly Lazy<WindowIcon?> s_defaultIcon;
        private readonly List<(Window child, bool isDialog)> _children = new List<(Window, bool)>();
        private bool _isExtendedIntoWindowDecorations;
        private Thickness _windowDecorationMargin;
        private Thickness _offScreenMargin;
        private bool _canHandleResized = false;
        private Size _arrangeBounds;
        private bool _isForcedDecorationMode;
        private Button _closeButton;
        private Button _maximizeButton;
        private Path _maximizeIcon;
        private Button _minimizeButton;
        private Border _titleBar;
        private Image _windowIcon;

        /// <summary>
        /// Defines the <see cref="SizeToContent"/> property.
        /// </summary>
        public static readonly StyledProperty<SizeToContent> SizeToContentProperty;

        /// <summary>
        /// Defines the <see cref="ExtendClientAreaToDecorationsHint"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ExtendClientAreaToDecorationsHintProperty;

        public static readonly StyledProperty<double> ExtendClientAreaTitleBarHeightHintProperty;

        /// <summary>
        /// Defines the <see cref="IsExtendedIntoWindowDecorations"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, bool> IsExtendedIntoWindowDecorationsProperty;

        /// <summary>
        /// Defines the <see cref="WindowDecorationMargin"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, Thickness> WindowDecorationMarginProperty;

        public static readonly DirectProperty<Window, Thickness> OffScreenMarginProperty;

        /// <summary>
        /// Defines the <see cref="WindowDecorations"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowDecorations> WindowDecorationsProperty;

        /// <summary>
        /// Defines the <see cref="WindowDecorationsTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> WindowDecorationsThemeProperty;

        /// <summary>
        /// Defines the <see cref="ShowActivated"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowActivatedProperty;

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        public static readonly StyledProperty<bool> ShowInTaskbarProperty;

        /// <summary>
        /// Defines the <see cref="ClosingBehavior"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowClosingBehavior> ClosingBehaviorProperty;

        /// <summary>
        /// Represents the currently effective window state (normal, minimized, maximized)
        /// </summary>
        public static readonly DirectProperty<Window, WindowState> WindowStateProperty;

        /// <summary>
        /// Defines the <see cref="CaseMap.Title"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> TitleProperty;

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowIcon?> IconProperty;

        public static readonly StyledProperty<object> InnerRightContentProperty;

        public static readonly StyledProperty<PresentationList<MenuItemView>> MainMenuProperty;

        public static readonly StyledProperty<IImage> TitleBarIconProperty;

        /// <summary>
        /// Defines the <see cref="WindowStartupLocation"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowStartupLocation> WindowStartupLocationProperty;

        /// <summary>
        /// Defines the <see cref="CanResize"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanResizeProperty;

        /// <summary>
        /// Defines the <see cref="CanMinimize"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanMinimizeProperty;

        /// <summary>
        /// Defines the <see cref="CanMaximize"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanMaximizeProperty;

        /// <summary>
        /// Routed event that can be used for global tracking of window destruction
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> WindowClosedEvent;

        /// <summary>
        /// Routed event that can be used for global tracking of opening windows
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> WindowOpenedEvent;
        private object? _dialogResult;
        private readonly Size _maxPlatformClientSize;
        private bool _shown;
        private bool _showingAsDialog;
        private bool _positionWasSet;
        private bool _wasShownBefore;
        private IDisposable? _modalSubscription;
        private PlatformAllowedWindowActions _allowedWindowActions = PlatformAllowedWindowActions.All;

        /// <summary>
        /// Initializes static members of the <see cref="Window"/> class.
        /// </summary>
        static Window()
        {
			s_defaultIcon = new(LoadDefaultIcon);
			SizeToContentProperty = PresentationProperty.Register<Window, SizeToContent>(nameof(SizeToContent));
			ExtendClientAreaToDecorationsHintProperty = PresentationProperty.Register<Window, bool>(nameof(ExtendClientAreaToDecorationsHint), false);
			ExtendClientAreaTitleBarHeightHintProperty = PresentationProperty.Register<Window, double>(nameof(ExtendClientAreaTitleBarHeightHint), -1);
			IsExtendedIntoWindowDecorationsProperty = PresentationProperty.RegisterDirect<Window, bool>(nameof(IsExtendedIntoWindowDecorations),
				o => o.IsExtendedIntoWindowDecorations,
				unsetValue: false);
			WindowDecorationMarginProperty = PresentationProperty.RegisterDirect<Window, Thickness>(nameof(WindowDecorationMargin),
				o => o.WindowDecorationMargin);
			OffScreenMarginProperty = PresentationProperty.RegisterDirect<Window, Thickness>(nameof(OffScreenMargin),
				o => o.OffScreenMargin);
			WindowDecorationsProperty = PresentationProperty.Register<Window, WindowDecorations>(nameof(WindowDecorations), WindowDecorations.Full);
			WindowDecorationsThemeProperty = PresentationProperty.Register<Window, ControlTheme>(nameof(WindowDecorationsTheme));
			ShowActivatedProperty = PresentationProperty.Register<Window, bool>(nameof(ShowActivated), true);
			ShowInTaskbarProperty = PresentationProperty.Register<Window, bool>(nameof(ShowInTaskbar), true);
			ClosingBehaviorProperty = PresentationProperty.Register<Window, WindowClosingBehavior>(nameof(ClosingBehavior));
			WindowStateProperty = PresentationProperty.RegisterDirect<Window, WindowState>(
				nameof(WindowState), o => o.WindowState,
				(o, v) => o.WindowState = v);
			TitleProperty = PresentationProperty.Register<Window, string>(nameof(Title), "Window");
			IconProperty = PresentationProperty.Register<Window, WindowIcon>(nameof(Icon));
			InnerRightContentProperty = PresentationProperty.Register<Window, object>(nameof(InnerRightContent));
			MainMenuProperty = PresentationProperty.Register<Window, PresentationList<MenuItemView>>(nameof(MainMenu));
			TitleBarIconProperty = PresentationProperty.Register<Window, IImage>(nameof(TitleBarIcon));
			WindowStartupLocationProperty = PresentationProperty.Register<Window, WindowStartupLocation>(nameof(WindowStartupLocation));
			CanResizeProperty = PresentationProperty.Register<Window, bool>(nameof(CanResize), true);
			CanMinimizeProperty = PresentationProperty.Register<Window, bool>(nameof(CanMinimize), true);
			CanMaximizeProperty = PresentationProperty.Register<Window, bool>(nameof(CanMaximize), true, coerce: CoerceCanMaximize);
			WindowClosedEvent = RoutedEvent.Register<Window, RoutedEventArgs>("WindowClosed", RoutingStrategies.Direct);
			WindowOpenedEvent = RoutedEvent.Register<Window, RoutedEventArgs>("WindowOpened", RoutingStrategies.Direct);
			BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
            ExtendClientAreaTitleBarHeightHintProperty.Changed.AddClassHandler<Window>((w, _) => w.OnTitleBarHeightHintChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window() : this(PlatformManager.CreateWindow())
		{
			SetCurrentValue(MainMenuProperty, new PresentationList<MenuItemView>(Dispatcher));
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        /// <param name="impl">The window implementation.</param>
        public Window(IWindowImpl impl)
            : base(impl)
        {
            if (impl.TransparentClientForNativeAirspace)
            {
                Background = Brushes.Transparent;
                TransparencyBackgroundFallback = Brushes.Transparent;
            }

            impl.Closing = HandleClosing;
            impl.GotInputWhenDisabled = OnGotInputWhenDisabled;
            impl.WindowStateChanged = HandleWindowStateChanged;
            impl.DrawnDecorationsRequestChanged = UpdateDrawnDecorations;
            _maxPlatformClientSize = PlatformImpl?.MaxAutoSizeHint ?? default(Size);
            impl.ExtendClientAreaToDecorationsChanged = ExtendClientAreaToDecorationsChanged;
            impl.AllowedWindowActionsChanged = OnAllowedWindowActionsChanged;
            _allowedWindowActions = impl.AllowedWindowActions;
            this.GetObservable(ClientSizeProperty).Skip(1).Subscribe(x =>
            {
                ResizePlatformImpl(x, WindowResizeReason.Application);
            });
            ScalingChanged += OnScalingChangedUpdateDecorations;

            CreatePlatformImplBinding(TitleProperty, title => PlatformImpl!.SetTitle(title));
            CreatePlatformImplBinding(IconProperty, SetEffectiveIcon);
            CreatePlatformImplBinding(CanResizeProperty, canResize => PlatformImpl!.CanResize(canResize));
            CreatePlatformImplBinding(CanMinimizeProperty, canMinimize => PlatformImpl!.SetCanMinimize(canMinimize));
            CreatePlatformImplBinding(CanMaximizeProperty, canMaximize => PlatformImpl!.SetCanMaximize(canMaximize));
            CreatePlatformImplBinding(ShowInTaskbarProperty, show => PlatformImpl!.ShowTaskbarIcon(show));
            
            CreatePlatformImplBinding(BorderBrushProperty, brush =>
            {
                var color = brush is ISolidColorBrush solid ? solid.Color : default;
                PlatformImpl!.SetWindowBorderColor(color);
            });
            CreatePlatformImplBinding(ExtendClientAreaToDecorationsHintProperty, hint => PlatformImpl!.SetExtendClientAreaToDecorationsHint(hint));
            CreatePlatformImplBinding(ExtendClientAreaTitleBarHeightHintProperty, height => PlatformImpl!.SetExtendClientAreaTitleBarHeightHint(height));

            CreatePlatformImplBinding(MinWidthProperty, UpdateMinMaxSize);
            CreatePlatformImplBinding(MaxWidthProperty, UpdateMinMaxSize);
            CreatePlatformImplBinding(MinHeightProperty, UpdateMinMaxSize);
            CreatePlatformImplBinding(MaxHeightProperty, UpdateMinMaxSize);

            void UpdateMinMaxSize(double _) => PlatformImpl!.SetMinMaxSize(new Size(MinWidth, MinHeight), new Size(MaxWidth, MaxHeight));
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public new IWindowImpl? PlatformImpl => (IWindowImpl?)base.PlatformImpl;

        /// <summary>
        /// Gets a collection of child windows owned by this window.
        /// </summary>
        public IReadOnlyList<Window> OwnedWindows => _children.Select(x => x.child).ToArray();

        /// <summary>
        /// Gets or sets a value indicating how the window will size itself to fit its content.
        /// </summary>
        /// <remarks>
        /// If <see cref="SizeToContent"/> has a value other than <see cref="SizeToContent.Manual"/>,
        /// <see cref="SizeToContent"/> is automatically set to <see cref="SizeToContent.Manual"/>
        /// if a user resizes the window by using the resize grip or dragging the border.
        /// 
        /// NOTE: Because of a limitation of X11, <see cref="SizeToContent"/> will be reset on X11 to
        /// <see cref="SizeToContent.Manual"/> on any resize - including the resize that happens when
        /// the window is first shown. This is because X11 resize notifications are asynchronous and
        /// there is no way to know whether a resize came from the user or the layout system. To avoid
        /// this, consider setting <see cref="CanResize"/> to false, which will disable user resizing
        /// of the window.
        /// </remarks>
        public SizeToContent SizeToContent
        {
            get => GetValue(SizeToContentProperty);
            set => SetValue(SizeToContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        public string? Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets if the ClientArea is Extended into the Window Decorations (chrome or border).
        /// Defaults to false. Themes that draw a client title bar should set this to true.
        /// </summary>
        public bool ExtendClientAreaToDecorationsHint
        {
            get => GetValue(ExtendClientAreaToDecorationsHintProperty);
            set => SetValue(ExtendClientAreaToDecorationsHintProperty, value);
        }

        /// <summary>
        /// Gets or Sets the TitlebarHeightHint for when the client area is extended.
        /// Defaults to -1 (OS default). Themes that draw a client title bar should set a positive height.
        /// A value of -1 will cause the titlebar to be auto sized to the OS default.
        /// Any other positive value will cause the titlebar to assume that height.
        /// </summary>
        public double ExtendClientAreaTitleBarHeightHint
        {
            get => GetValue(ExtendClientAreaTitleBarHeightHintProperty);
            set => SetValue(ExtendClientAreaTitleBarHeightHintProperty, value);
        }

        /// <summary>
        /// Gets if the ClientArea is Extended into the Window Decorations.
        /// </summary>
        public bool IsExtendedIntoWindowDecorations
        {
            get => _isExtendedIntoWindowDecorations;
            private set => SetAndRaise(IsExtendedIntoWindowDecorationsProperty, ref _isExtendedIntoWindowDecorations, value);
        }

        /// <summary>
        /// Gets the WindowDecorationMargin.
        /// This tells you the thickness around the window that is used by borders and the titlebar.
        /// </summary>
        public Thickness WindowDecorationMargin
        {
            get => _windowDecorationMargin;
            private set => SetAndRaise(WindowDecorationMarginProperty, ref _windowDecorationMargin, value);
        }

        /// <summary>
        /// Gets the window margin that is hidden off the screen area.
        /// This is generally only the case on Windows when in Maximized where the window border
        /// is hidden off the screen. This Margin may be used to ensure user content doesnt overlap this space.
        /// </summary>
        public Thickness OffScreenMargin
        {
            get => _offScreenMargin;
            private set => SetAndRaise(OffScreenMarginProperty, ref _offScreenMargin, value);
        }

        /// <summary>
        /// Gets or sets the window decorations (title bar, border, etc).
        /// </summary>
        public WindowDecorations WindowDecorations
        {
            get => GetValue(WindowDecorationsProperty);
            set => SetValue(WindowDecorationsProperty, value);
        }

        /// <summary>
        /// Gets or sets the theme used to render the window decorations when they are not drawn by the system.
        /// </summary>
        public ControlTheme? WindowDecorationsTheme
        {
            get => GetValue(WindowDecorationsThemeProperty);
            set => SetValue(WindowDecorationsThemeProperty, value);
        }

        [Obsolete("Use WindowDecorations instead.")]
        public WindowDecorations SystemDecorations
        {
            get => WindowDecorations;
            set => WindowDecorations = value;
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a window is activated when first shown. 
        /// </summary>
        public bool ShowActivated
        {
            get => GetValue(ShowActivatedProperty);
            set => SetValue(ShowActivatedProperty, value);
        }

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        /// 
        public bool ShowInTaskbar
        {
            get => GetValue(ShowInTaskbarProperty);
            set => SetValue(ShowInTaskbarProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating how the <see cref="Closing"/> event behaves in the presence
        /// of child windows.
        /// </summary>
        public WindowClosingBehavior ClosingBehavior
        {
            get => GetValue(ClosingBehaviorProperty);
            set => SetValue(ClosingBehaviorProperty, value);
        }

        private WindowState _lastWindowState;
        /// <summary>
        /// Represents the currently effective window state (normal, minimized, maximized)
        /// </summary>
        public WindowState WindowState
        {
            get => PlatformImpl?.WindowStateGetterIsUsable == true ?
                PlatformImpl.WindowState :
                _lastWindowState;
            set
            {
                if (PlatformImpl != null)
                {
                    if (PlatformImpl.WindowStateGetterIsUsable)
                    {
                        // Attempt to set the window state to desired value, if it succeeds the platform will
                        // trigger WindowStateChanged callback which will trigger SetAndRaise for the WindowState property
                        PlatformImpl.WindowState = value;
                        
                        // If the request was refused - trigger a synthetic property change notification
                        // for data bindings and user state to fix itself.
                        if (PlatformImpl.WindowState != value)
                        {
                            // Since it's a force notify, we aren't checking for the old value and sometimes
                            // trigger notification with oldValue = newValue.
                            var oldValue = _lastWindowState;
                            _lastWindowState = PlatformImpl.WindowState;
                            RaisePropertyChanged(WindowStateProperty, oldValue, _lastWindowState);
                        }
                    }
                    else 
                    {
                        // Legacy behavior - update the property and hope for the best that the platform
                        // will update it back to match the actual window state
                        SetAndRaise(WindowStateProperty, ref _lastWindowState, value);
                        PlatformImpl.WindowState = _lastWindowState;
                    }
                }
            }
        }

        /// <summary>
        /// Enables or disables resizing of the window.
        /// </summary>
        public bool CanResize
        {
            get => GetValue(CanResizeProperty);
            set => SetValue(CanResizeProperty, value);
        }

        /// <summary>
        /// Enables or disables minimizing the window.
        /// </summary>
        /// <remarks>
        /// This property might be ignored by some window managers on Linux.
        /// </remarks>
        public bool CanMinimize
        {
            get => GetValue(CanMinimizeProperty);
            set => SetValue(CanMinimizeProperty, value);
        }

        /// <summary>
        /// Enables or disables maximizing the window.
        /// </summary>
        /// <remarks>
        /// <para>When <see cref="CanResize"/> is false, this property is always false.</para>
        /// <para>On macOS, setting this property to false also disables the full screen mode.</para>
        /// <para>This property might be ignored by some window managers on Linux.</para>
        /// </remarks>
        public bool CanMaximize
        {
            get => GetValue(CanMaximizeProperty);
            set => SetValue(CanMaximizeProperty, value);
        }

        /// <summary>
        /// Gets the window actions currently allowed by the underlying platform.
        /// </summary>
        internal PlatformAllowedWindowActions AllowedWindowActions => _allowedWindowActions;

        /// <summary>
        /// Gets or sets the icon of the window.
        /// </summary>
        public WindowIcon? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public ICommand ExitApplicationCommand { get; private set; }

        public object InnerRightContent
        {
            get => GetValue(InnerRightContentProperty);
            set => SetValue(InnerRightContentProperty, value);
        }

        public PresentationList<MenuItemView> MainMenu
        {
            get => GetValue(MainMenuProperty);
            set => SetValue(MainMenuProperty, value);
        }

        public IImage TitleBarIcon
        {
            get => GetValue(TitleBarIconProperty);
            set => SetValue(TitleBarIconProperty, value);
        }

        /// <summary>
        /// Gets or sets the startup location of the window.
        /// </summary>
        public WindowStartupLocation WindowStartupLocation
        {
            get => GetValue(WindowStartupLocationProperty);
            set => SetValue(WindowStartupLocationProperty, value);
        }

        /// <summary>
        /// Gets or sets the window position in screen coordinates.
        /// </summary>
        public PixelPoint Position
        {
            get => PlatformImpl?.Position ?? PixelPoint.Origin;
            set
            {
                PlatformImpl?.Move(value);
                _positionWasSet = true;
            }
        }

        /// <summary>
        /// Gets whether this window was opened as a dialog
        /// </summary>
        public bool IsDialog => _showingAsDialog;

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler
        /// </summary>
        public void BeginMoveDrag(PointerPressedEventArgs e) => PlatformImpl?.BeginMoveDrag(e);

        /// <summary>
        /// Starts resizing a window. This function is used if an application has window resizing controls. 
        /// Should be called from left mouse button press event handler
        /// </summary>
        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e) => PlatformImpl?.BeginResizeDrag(edge, e);

        /// <inheritdoc/>
        protected override Type StyleKeyOverride => typeof(Window);

        /// <summary>
        /// Fired before a window is closed.
        /// </summary>
        public event EventHandler<WindowClosingEventArgs>? Closing;

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void Close()
        {
            CloseCore(WindowCloseReason.WindowClosing, true, false);
        }

        /// <summary>
        /// Closes a dialog window with the specified result.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        /// <remarks>
        /// When the window is shown with the <see cref="ShowDialog{TResult}(Window)"/>
        /// or <see cref="ShowDialog{TResult}(Window)"/> method, the
        /// resulting task will produce the <see cref="_dialogResult"/> value when the window
        /// is closed.
        /// </remarks>
        public void Close(object? dialogResult)
        {
            _dialogResult = dialogResult;
            CloseCore(WindowCloseReason.WindowClosing, true, false);
        }

        internal void CloseCore(WindowCloseReason reason, bool isProgrammatic, bool ignoreCancel)
        {
            bool close = true;

            try
            {
                if (ShouldCancelClose(new WindowClosingEventArgs(reason, isProgrammatic)))
                {
                    close = false;
                }
            }
            finally
            {
                if (close || ignoreCancel)
                {
                    CloseInternal();
                }
            }
        }

        /// <summary>
        /// Handles a closing notification from <see cref="IWindowImpl.Closing"/>.
        /// <returns>true if closing is cancelled. Otherwise false.</returns>
        /// </summary>
        /// <param name="reason">The reason the window is closing.</param>
        private protected virtual bool HandleClosing(WindowCloseReason reason)
        {
            if (!ShouldCancelClose(new WindowClosingEventArgs(reason, false)))
            {
                CloseInternal();
                return false;
            }

            return true;
        }

        private void CloseInternal()
        {
            foreach (var (child, _) in _children.ToArray())
            {
                child.CloseInternal();
            }

            PlatformImpl?.Dispose();

            _showingAsDialog = false;

            Owner = null;
        }

        private bool ShouldCancelClose(WindowClosingEventArgs args)
        {
            switch (ClosingBehavior)
            {
                case WindowClosingBehavior.OwnerAndChildWindows:
                    bool canClose = true;

                    if (_children.Count > 0)
                    {
                        var childArgs = args.CloseReason == WindowCloseReason.WindowClosing ?
                            new WindowClosingEventArgs(WindowCloseReason.OwnerWindowClosing, args.IsProgrammatic) :
                            args;

                        foreach (var (child, _) in _children.ToArray())
                        {
                            if (child.ShouldCancelClose(childArgs))
                            {
                                canClose = false;
                            }
                        }
                    }

                    if (canClose)
                    {
                        OnClosing(args);

                        return args.Cancel;
                    }

                    return true;
                case WindowClosingBehavior.OwnerWindowOnly:
                    OnClosing(args);

                    return args.Cancel;
            }

            return false;
        }

        private void HandleWindowStateChanged(WindowState state)
        {
            // Check if platform impl doesn't lie about get_WindowState being usable
            Debug.Assert(PlatformImpl is not { WindowStateGetterIsUsable: true } || PlatformImpl.WindowState == state);

            var wasMinimized = _lastWindowState == WindowState.Minimized;
            SetAndRaise(WindowStateProperty, ref _lastWindowState, state);

            if (state == WindowState.Minimized)
            {
                StopRendering();
            }
            else
            {
                StartRendering();
                // Layout bounds are unchanged, so native hosts never receive ShowInBounds.
                // Child-on-top still paints. Native-behind shows the composition window over a
                // surface Windows did not put back, which is black for every native control.
                if (wasMinimized)
                {
                    ReapplyNativeHosts();
                }
            }

            // Update decoration parts and fullscreen popover state for the new window state
            UpdateDrawnDecorationParts();
        }

        private void ReapplyNativeHosts()
        {
            foreach (var host in this.GetVisualDescendants().OfType<NativeControlHost>())
            {
                host.TryUpdateNativeControlPosition();
            }
        }

        internal event Action<PlatformAllowedWindowActions>? AllowedWindowActionsChanged;

        private void OnAllowedWindowActionsChanged(PlatformAllowedWindowActions actions)
        {
            _allowedWindowActions = actions;
            AllowedWindowActionsChanged?.Invoke(actions);
        }

        private void ExtendClientAreaToDecorationsChanged(bool isExtended)
        {
            IsExtendedIntoWindowDecorations = isExtended;
            OffScreenMargin = PlatformImpl?.OffScreenMargin ?? default;

            UpdateDrawnDecorations();
        }
        
        private void UpdateDrawnDecorations()
        {
            var parts = ComputeDecorationParts();
            
            // Detect forced mode: platform needs managed decorations but app hasn't opted in
            _isForcedDecorationMode = parts != null && !IsExtendedIntoWindowDecorations;

            TopLevelHost.UpdateDrawnDecorations(parts, WindowState, WindowDecorationsTheme);

            if (parts != null)
            {
                // Forward ExtendClientAreaTitleBarHeightHint to decoration TitleBarHeight
                var decorations = TopLevelHost.Decorations;
                if (decorations != null)
                {
                    decorations.RenderScaling = RenderScaling;

                    var hint = ExtendClientAreaTitleBarHeightHint;
                    if (hint >= 0)
                        decorations.TitleBarHeightOverride = hint;
                }
            }
            
            UpdateDrawnDecorationMargins();
        }

        private void OnScalingChangedUpdateDecorations(object? sender, EventArgs e)
        {
            var decorations = TopLevelHost.Decorations;
            if (decorations != null)
                decorations.RenderScaling = RenderScaling;
        }

        /// <summary>
        /// Updates decoration parts based on current window state without
        /// re-creating the decorations instance.
        /// </summary>
        private void UpdateDrawnDecorationParts()
        {
            if (TopLevelHost.Decorations == null)
                return;

            TopLevelHost.UpdateDrawnDecorations(ComputeDecorationParts(), WindowState, WindowDecorationsTheme);
        }

        private Chrome.DrawnWindowDecorationParts? ComputeDecorationParts()
        {
            if (!(PlatformImpl?.NeedsManagedDecorations ?? false))
                return null;

            var platformNeeds = PlatformImpl?.RequestedDrawnDecorations ?? PlatformRequestedDrawnDecoration.None;
            var parts = Chrome.DrawnWindowDecorationParts.None;
            if (WindowDecorations != WindowDecorations.None)
            {
                if (platformNeeds.HasFlag(PlatformRequestedDrawnDecoration.TitleBar) &&
                    WindowDecorations == WindowDecorations.Full)
                    parts |= Chrome.DrawnWindowDecorationParts.TitleBar;
                if (platformNeeds.HasFlag(PlatformRequestedDrawnDecoration.Shadow))
                    parts |= Chrome.DrawnWindowDecorationParts.Shadow;
                if (platformNeeds.HasFlag(PlatformRequestedDrawnDecoration.Border))
                    parts |= Chrome.DrawnWindowDecorationParts.Border;
                if (platformNeeds.HasFlag(PlatformRequestedDrawnDecoration.ResizeGrips) && CanResize)
                    parts |= Chrome.DrawnWindowDecorationParts.ResizeGrips;


                // In fullscreen: no shadow, border, resize grips, or titlebar (popover takes over)
                if (WindowState == WindowState.FullScreen)
                {
                    parts &= ~(Chrome.DrawnWindowDecorationParts.Shadow
                               | Chrome.DrawnWindowDecorationParts.Border
                               | Chrome.DrawnWindowDecorationParts.ResizeGrips
                               | Chrome.DrawnWindowDecorationParts.TitleBar);
                }
                // In maximized: no shadow, border, or resize grips (titlebar stays)
                else if (WindowState == WindowState.Maximized)
                {
                    parts &= ~(Chrome.DrawnWindowDecorationParts.Shadow
                               | Chrome.DrawnWindowDecorationParts.Border
                               | Chrome.DrawnWindowDecorationParts.ResizeGrips);
                }
            }

            return parts;
        }

        private void UpdateDrawnDecorationMargins()
        {
            var decorations = TopLevelHost.Decorations;
            if (decorations == null)
            {
                // Only use platform margins if drawn decorations are not active
                WindowDecorationMargin = PlatformImpl?.ExtendedMargins ?? default;
                TopLevelHost.DecorationInset = default;
                PlatformImpl?.SetShadowExtents(default);
                return;
            }

            var parts = decorations.EnabledParts;
            var titleBarHeight = parts.HasFlag(Chrome.DrawnWindowDecorationParts.TitleBar)
                ? decorations.TitleBarHeight : 0;
            var frame = parts.HasFlag(Chrome.DrawnWindowDecorationParts.Border)
                ? decorations.FrameThickness : default;
            var shadow = parts.HasFlag(Chrome.DrawnWindowDecorationParts.Shadow)
                ? decorations.ShadowThickness : default;
            
            PlatformImpl?.SetShadowExtents(shadow);
            
            var margin = new Thickness(
                frame.Left + shadow.Left,
                titleBarHeight + frame.Top + shadow.Top,
                frame.Right + shadow.Right,
                frame.Bottom + shadow.Bottom);

            if (_isForcedDecorationMode)
            {
                // In forced mode, app is unaware of decorations.
                // TopLevelHost insets the Window child; WindowDecorationMargin stays zero.
                WindowDecorationMargin = default;
                TopLevelHost.DecorationInset = margin;
            }
            else
            {
                // In extended mode, app handles the margin itself.
                WindowDecorationMargin = margin;
                TopLevelHost.DecorationInset = default;
            }
        }

        private void OnTitleBarHeightHintChanged()
        {
            var decorations = TopLevelHost.Decorations;
            if (decorations == null)
                return;

            decorations.TitleBarHeightOverride = ExtendClientAreaTitleBarHeightHint;

            UpdateDrawnDecorationMargins();
        }

        /// <summary>
        /// Called by TopLevelHost when decoration effective geometry changes
        /// (e.g. theme changes Default* values, or EnabledParts changes).
        /// </summary>
        internal void OnDrawnDecorationsGeometryChanged()
        {
            UpdateDrawnDecorationMargins();
        }

        /// <summary>
        /// Hides the window but does not close it.
        /// </summary>
        public override void Hide()
        {
            using (FreezeVisibilityChangeHandling())
            {
                if (!_shown)
                {
                    return;
                }

                StopRendering();

                if (_children.Count > 0)
                {
                    foreach (var child in _children.ToArray())
                    {
                        child.child.Hide();
                    }
                }

                Owner = null;
                PlatformImpl?.Hide();
                IsVisible = false;

                _modalSubscription?.Dispose();
                _shown = false;
            }
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        public override void Show()
        {
            ShowCore<object>(null, false);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            EnableVisualLayerManagerLayers();
            BindTitleBarChrome(e);
        }

        protected override void IsVisibleChanged(PresentationPropertyChangedEventArgs e)
        {
            if (!IgnoreVisibilityChanges)
            {
                var isVisible = e.GetNewValue<bool>();

                if (_shown != isVisible)
                {
                    if (!_shown)
                    {
                        Show();
                    }
                    else
                    {
                        Hide();
                    }
                }
            }
        }

        /// <summary>
        /// Shows the window as a child of <paramref name="owner"/>.
        /// </summary>
        /// <param name="owner">Window that will be the owner of the shown window.</param>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        public void Show(Window owner)
        {
            if (owner is null)
            {
                throw new ArgumentNullException(nameof(owner), "Showing a child window requires valid parent.");
            }

            ShowCore<object>(owner, false);
        }

        private void EnsureStateBeforeShow()
        {
            if (PlatformImpl == null)
            {
                throw new InvalidOperationException("Cannot re-show a closed window.");
            }
        }

        private void EnsureParentStateBeforeShow(Window owner)
        {
            if (owner.PlatformImpl == null)
            {
                throw new InvalidOperationException("Cannot show a window with a closed owner.");
            }

            if (owner == this)
            {
                throw new InvalidOperationException("A Window cannot be its own owner.");
            }

            if (!owner.IsVisible)
            {
                throw new InvalidOperationException("Cannot show window with non-visible owner.");
            }
        }

        private Task<TResult>? ShowCore<TResult>(Window? owner, bool modal)
        {
            using (FreezeVisibilityChangeHandling())
            {
                EnsureStateBeforeShow();

                if (modal && owner == null)
                {
                    throw new ArgumentNullException(nameof(owner));
                }
                if (owner != null)
                {
                    EnsureParentStateBeforeShow(owner);
                }

                if (_shown)
                {
                    if (modal)
                        throw new InvalidOperationException("The window is already being shown.");
                    return null;
                }

                _showingAsDialog = modal;
                RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

                using (AppBootstrap.StartupProfiler.Start("Window.EnsureInitialized"))
                {
                    EnsureInitialized();
                }

                using (AppBootstrap.StartupProfiler.Start("Window.ApplyStyling"))
                {
                    ApplyStyling();
                }
                
                // Enable drawn decorations before layout so margins are computed
                UpdateDrawnDecorations();
                
                // In forced mode, adjust ClientSize to reflect usable content area
                if (_isForcedDecorationMode)
                {
                    var inset = TopLevelHost.DecorationInset;
                    ClientSize = new Size(
                        Math.Max(0, ClientSize.Width - inset.Left - inset.Right),
                        Math.Max(0, ClientSize.Height - inset.Top - inset.Bottom));
                }
                
                _shown = true;
                IsVisible = true;

                SetEffectiveIcon(Icon);

                // If window position was not set before then platform may provide incorrect scaling at this time,
                // but we need it for proper calculation of position and in some cases size (size to content)
                SetExpectedScaling(owner);

                var initialSize = new Size(
                    double.IsNaN(Width) ? ClientSize.Width : Width,
                    double.IsNaN(Height) ? ClientSize.Height : Height);

                var minMax = new MinMax(this);

                initialSize = new Size(
                    MathUtilities.Clamp(initialSize.Width, minMax.MinWidth, minMax.MaxWidth),
                    MathUtilities.Clamp(initialSize.Height, minMax.MinHeight, minMax.MaxHeight));

                var clientSizeChanged = initialSize != ClientSize;
                ClientSize = initialSize; // ClientSize is required for Measure and Arrange

                // this will call ArrangeSetBounds
                using (AppBootstrap.StartupProfiler.Start("Layout.ExecuteInitialLayoutPass"))
                {
                    LayoutManager.ExecuteInitialLayoutPass();
                }

                if (SizeToContent.HasFlag(SizeToContent.Width))
                {
                    initialSize = initialSize.WithWidth(MathUtilities.Clamp(_arrangeBounds.Width, minMax.MinWidth, minMax.MaxWidth));
                    clientSizeChanged |= initialSize != ClientSize;
                    ClientSize = initialSize;
                }

                if (SizeToContent.HasFlag(SizeToContent.Height))
                {
                    initialSize = initialSize.WithHeight(MathUtilities.Clamp(_arrangeBounds.Height, minMax.MinHeight, minMax.MaxHeight));
                    clientSizeChanged |= initialSize != ClientSize;
                    ClientSize = initialSize;
                }

                Owner = owner;

                SetWindowStartupLocation(owner);

                DesktopScalingOverride = null;

                // In forced mode, compare against adjusted platform size
                var platformClientSize = PlatformImpl?.ClientSize ?? default;
                var comparableClientSize = _isForcedDecorationMode
                    ? new Size(
                        Math.Max(0, platformClientSize.Width - TopLevelHost.DecorationInset.Left - TopLevelHost.DecorationInset.Right),
                        Math.Max(0, platformClientSize.Height - TopLevelHost.DecorationInset.Top - TopLevelHost.DecorationInset.Bottom))
                    : platformClientSize;

                if (clientSizeChanged || ClientSize != comparableClientSize)
                {
                    // Previously it was called before ExecuteInitialLayoutPass
                    ResizePlatformImpl(ClientSize, WindowResizeReason.Layout);

                    // we do not want PlatformImpl?.Resize to trigger HandleResized yet because it will set Width and Height.
                    // So perform some important actions from HandleResized

                    Renderer.Resized(ClientSize);
                    OnResized(new WindowResizedEventArgs(ClientSize, WindowResizeReason.Layout));

                    if (!double.IsNaN(Width))
                        Width = ClientSize.Width;
                    if (!double.IsNaN(Height))
                        Height = ClientSize.Height;
                }

                FrameSize = PlatformImpl?.FrameSize;

                _canHandleResized = true;

                using (AppBootstrap.StartupProfiler.Start("Window.StartRendering"))
                {
                    StartRendering();
                }

                using (AppBootstrap.StartupProfiler.Start("PlatformImpl.Show"))
                {
                    PlatformImpl?.Show(ShowActivated, modal);
                }

                Task<TResult>? result = null;
                if (modal)
                {
                    var tcs = new TaskCompletionSource<TResult>();

                    var disposables = new CompositeDisposable(
                    [
                        Observable.FromEventPattern(
                            x => Closed += x,
                            x => Closed -= x)
                        .Take(1)
                        .Subscribe(_ =>
                        {
                            _modalSubscription?.Dispose();
                        }),
                        Disposable.Create(() =>
                        {
                            _modalSubscription = null;
                            owner!.Activate();
                            tcs.SetResult((TResult)(_dialogResult ?? default(TResult)!));
                        })
                    ]);

                    _modalSubscription = disposables;
                    result = tcs.Task;
                }

                using (AppBootstrap.StartupProfiler.Start("Window.OnOpened"))
                {
                    OnOpened(EventArgs.Empty);
                }

                if (!modal)
                    _wasShownBefore = true;

                return result;
            }
        }

        private void ResizePlatformImpl(Size size, WindowResizeReason reason)
        {
            // In forced mode, add decoration inset so platform gets full frame size
            if (_isForcedDecorationMode)
            {
                var inset = TopLevelHost.DecorationInset;
                size = new Size(
                    size.Width + inset.Left + inset.Right,
                    size.Height + inset.Top + inset.Bottom);
                if (PlatformImpl?.ClientSize != size)
                    PlatformImpl?.Resize(size, reason);
            }
            else
                PlatformImpl?.Resize(size, reason);
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <param name="owner">The dialog's owner window.</param>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        /// <returns>
        /// A task that can be used to track the lifetime of the dialog.
        /// </returns>
        public Task ShowDialog(Window owner)
        {
            return ShowDialog<object>(owner);
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the dialog.
        /// </typeparam>
        /// <param name="owner">The dialog's owner window.</param>
        /// <returns>.
        /// A task that can be used to retrieve the result of the dialog when it closes.
        /// </returns>
        public Task<TResult> ShowDialog<TResult>(Window owner) => ShowCore<TResult>(owner, true)!;

        /// <summary>
        /// Sorts the windows ascending by their Z order - the topmost window will be the last in the list.
        /// </summary>
        /// <param name="windows">The windows to sort.</param>
        public static void SortWindowsByZOrder(Span<Window> windows)
        {
            if (windows.Length <= 1)
                return;

            var platform = PresentationLocator.Current.GetRequiredService<IWindowingPlatform>();

            var windowImpls = new IWindowImpl[windows.Length];
            for (var i = 0; i < windows.Length; ++i)
            {
                windowImpls[i] = windows[i].PlatformImpl ??
                                 throw new ArgumentException($"Invalid window at index {i}", nameof(windows));
            }

            const int stackAllocThreshold = 128;
            var zOrder = windows.Length > stackAllocThreshold ? new long[windows.Length] : stackalloc long[windows.Length];
            platform.GetWindowsZOrder(windowImpls, zOrder);
            zOrder.Sort(windows);
        }

        private void UpdateEnabled()
        {
            bool isEnabled = true;

            foreach (var (_, isDialog) in _children)
            {
                if (isDialog)
                {
                    isEnabled = false;
                    break;
                }
            }

            PlatformImpl?.SetEnabled(isEnabled);
        }

        private void AddChild(Window window, bool isDialog)
        {
            _children.Add((window, isDialog));
            UpdateEnabled();
        }

        private void RemoveChild(Window window)
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var (child, _) = _children[i];

                if (ReferenceEquals(child, window))
                {
                    _children.RemoveAt(i);
                }
            }

            UpdateEnabled();
        }

        private void OnGotInputWhenDisabled()
        {
            Window? firstDialogChild = null;

            foreach (var (child, isDialog) in _children)
            {
                if (isDialog)
                {
                    firstDialogChild = child;
                    break;
                }
            }

            if (firstDialogChild != null)
            {
                firstDialogChild.OnGotInputWhenDisabled();
            }
            else
            {
                Activate();
            }
        }

        private void SetExpectedScaling(WindowBase? owner)
        {
            if (_wasShownBefore)
            {
                return;
            }

            var location = GetEffectiveWindowStartupLocation(owner);

            switch (location)
            {
                case WindowStartupLocation.CenterOwner:
                    DesktopScalingOverride = owner?.DesktopScaling;
                    break;
                case WindowStartupLocation.CenterScreen:
                    DesktopScalingOverride = owner?.DesktopScaling ?? Screens.ScreenFromPoint(Position)?.Scaling ?? Screens.Primary?.Scaling;
                    break;
                case WindowStartupLocation.Manual:
                    DesktopScalingOverride = Screens.ScreenFromPoint(Position)?.Scaling;
                    break;
            }
        }

        private WindowStartupLocation GetEffectiveWindowStartupLocation(WindowBase? owner)
        {
            var startupLocation = WindowStartupLocation;

            if (startupLocation == WindowStartupLocation.CenterOwner &&
                (owner is null ||
                 (owner is Window ownerWindow && ownerWindow.WindowState == WindowState.Minimized))
               )
            {
                // If startup location is CenterOwner, but owner is null or minimized then fall back
                // to CenterScreen. This behavior is consistent with WPF.
                startupLocation = WindowStartupLocation.CenterScreen;
            }

            return startupLocation;
        }

        private void SetWindowStartupLocation(Window? owner = null)
        {
            if (_wasShownBefore)
            {
                return;
            }

            var startupLocation = GetEffectiveWindowStartupLocation(owner);

            PixelRect rect;
            // Use frame size, falling back to client size if the platform can't give it to us.
            if (PlatformImpl?.FrameSize.HasValue == true)
            {
                // Platform may calculate FrameSize with incorrect scaling, so do not trust the value.
                var diff = PlatformImpl.FrameSize.Value - PlatformImpl.ClientSize;
                rect = new PixelRect(PixelSize.FromSize(ClientSize + diff, DesktopScaling));
            }
            else
            {
                rect = new PixelRect(PixelSize.FromSize(ClientSize, DesktopScaling));
            }

            if (startupLocation == WindowStartupLocation.CenterScreen)
            {
                Screen? screen = null;

                if (owner is not null)
                {
                    screen = Screens.ScreenFromWindow(owner)
                             ?? Screens.ScreenFromPoint(owner.Position);
                }

                screen ??= Screens.ScreenFromPoint(Position);
                screen ??= Screens.Primary;

                if (screen is not null)
                {
                    var childRect = screen.WorkingArea.CenterRect(rect);

                    if (Screens.ScreenFromPoint(childRect.Position) == null)
                        childRect = ApplyScreenConstraint(screen, childRect);

                    Position = childRect.Position;
                }
            }
            else if (startupLocation == WindowStartupLocation.CenterOwner)
            {
                var ownerSize = owner!.FrameSize ?? owner.ClientSize;
                var ownerRect = new PixelRect(
                    owner.Position,
                    PixelSize.FromSize(ownerSize, owner.DesktopScaling));
                var childRect = ownerRect.CenterRect(rect);

                var screen = Screens.ScreenFromWindow(owner);

                childRect = ApplyScreenConstraint(screen, childRect);

                Position = childRect.Position;
            }

            if (!_positionWasSet && DesktopScaling != PlatformImpl?.DesktopScaling) // Platform returns incorrect scaling, forcing setting position may fix it
                PlatformImpl?.Move(Position);

            PixelRect ApplyScreenConstraint(Screen? screen, PixelRect childRect)
            {
                if (screen?.WorkingArea is { } constraint)
                {
                    var maxX = constraint.Right - rect.Width;
                    var maxY = constraint.Bottom - rect.Height;

                    if (constraint.X <= maxX)
                        childRect = childRect.WithX(MathUtilities.Clamp(childRect.X, constraint.X, maxX));
                    if (constraint.Y <= maxY)
                        childRect = childRect.WithY(MathUtilities.Clamp(childRect.Y, constraint.Y, maxY));
                }

                return childRect;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var sizeToContent = SizeToContent;
            var clientSize = ClientSize;
            if (_isForcedDecorationMode)
            {
                clientSize = PlatformImpl?.ClientSize ?? clientSize;
                var inset = TopLevelHost.DecorationInset;
                clientSize = new Size(
                    Math.Max(0, clientSize.Width - inset.Left - inset.Right),
                    Math.Max(0, clientSize.Height - inset.Top - inset.Bottom));
            }
            var maxAutoSize = PlatformImpl?.MaxAutoSizeHint ?? Size.Infinity;
            var useAutoWidth = sizeToContent.HasAllFlags(SizeToContent.Width);
            var useAutoHeight = sizeToContent.HasAllFlags(SizeToContent.Height);

            var constraint = new Size(
                useAutoWidth || double.IsInfinity(availableSize.Width) ? clientSize.Width : availableSize.Width,
                useAutoHeight || double.IsInfinity(availableSize.Height) ? clientSize.Height : availableSize.Height);

            if (MaxWidth > 0 && MaxWidth < maxAutoSize.Width)
            {
                maxAutoSize = maxAutoSize.WithWidth(MaxWidth);
            }
            if (MaxHeight > 0 && MaxHeight < maxAutoSize.Height)
            {
                maxAutoSize = maxAutoSize.WithHeight(MaxHeight);
            }

            if (useAutoWidth)
            {
                constraint = constraint.WithWidth(maxAutoSize.Width);
            }

            if (useAutoHeight)
            {
                constraint = constraint.WithHeight(maxAutoSize.Height);
            }

            var result = base.MeasureOverride(constraint);

            if (!useAutoWidth)
            {
                if (!double.IsInfinity(availableSize.Width))
                {
                    result = result.WithWidth(availableSize.Width);
                }
                else
                {
                    result = result.WithWidth(clientSize.Width);
                }
            }

            if (!useAutoHeight)
            {
                if (!double.IsInfinity(availableSize.Height))
                {
                    result = result.WithHeight(availableSize.Height);
                }
                else
                {
                    result = result.WithHeight(clientSize.Height);
                }
            }

            return result;
        }

        private protected sealed override Size ArrangeSetBounds(Size size)
        {
            _arrangeBounds = size;
            if (_canHandleResized)
            {
                ResizePlatformImpl(size, WindowResizeReason.Layout);
            }
            return ClientSize;
        }

        private protected sealed override void HandleClosed()
        {
            _shown = false;

            base.HandleClosed();

            RaiseEvent(new RoutedEventArgs(WindowClosedEvent));

            Owner = null;
        }

        /// <inheritdoc/>
        internal override void HandleResized(Size clientSize, WindowResizeReason reason)
        {
            // In forced decoration mode, the platform's clientSize includes decoration area.
            // Subtract the decoration inset so Window.ClientSize reflects the usable content area.
            if (_isForcedDecorationMode)
            {
                var inset = TopLevelHost.DecorationInset;
                clientSize = new Size(
                    Math.Max(0, clientSize.Width - inset.Left - inset.Right),
                    Math.Max(0, clientSize.Height - inset.Top - inset.Bottom));
            }

            if (_canHandleResized && (ClientSize != clientSize || double.IsNaN(Width) || double.IsNaN(Height)))
            {
                var sizeToContent = SizeToContent;

                // If auto-sizing is enabled, and the resize came from a user resize (or the reason was
                // unspecified) then turn off auto-resizing for any window dimension that is not equal
                // to the requested size.
                if (sizeToContent != SizeToContent.Manual &&
                    CanResize &&
                    reason == WindowResizeReason.Unspecified ||
                    reason == WindowResizeReason.User)
                {
                    if (clientSize.Width != ClientSize.Width)
                        sizeToContent &= ~SizeToContent.Width;
                    if (clientSize.Height != ClientSize.Height)
                        sizeToContent &= ~SizeToContent.Height;
                    SizeToContent = sizeToContent;
                }

                Width = clientSize.Width;
                Height = clientSize.Height;
            }

            base.HandleResized(clientSize, reason);
        }

        /// <summary>
        /// Raises the <see cref="Closing"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// A type that derives from <see cref="Window"/>  may override <see cref="OnClosing"/>. The
        /// overridden method must call <see cref="OnClosing"/> on the base class if the
        /// <see cref="Closing"/> event needs to be raised.
        /// </remarks>
        protected virtual void OnClosing(WindowClosingEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        protected virtual void BindCommands()
        {
            ExitApplicationCommand = new WindowExitCommand(this);
        }

        protected virtual void BuildMenu()
        {
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            BindCommands();
            BuildMenu();
            base.OnLoaded(e);
        }

        protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == WindowDecorationsProperty)
            {
                var (_, typedNewValue) = change.GetOldAndNewValue<WindowDecorations>();

                PlatformImpl?.SetWindowDecorations(typedNewValue);
                UpdateDrawnDecorations();
            }

            else if (change.Property == WindowDecorationsThemeProperty)
            {
                UpdateDrawnDecorations();
            }

            else if (change.Property == OwnerProperty)
            {
                var oldParent = change.OldValue as Window;
                var newParent = change.NewValue as Window;

                oldParent?.RemoveChild(this);
                newParent?.AddChild(this, _showingAsDialog);

                if (PlatformImpl is IWindowImpl impl)
                {
                    impl.SetParent(_showingAsDialog ? newParent?.PlatformImpl! : (newParent?.PlatformImpl ?? null));
                }
            }

            else if (change.Property == CanResizeProperty)
            {
                CoerceValue(CanMaximizeProperty);
            }

            else if (change.Property == IconProperty)
            {
                if (TitleBarIcon == null)
                {
                    TitleBarIcon = Icon?.ToBitmap();
                }
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WindowAutomationPeer(this);
        }

        private static WindowIcon? LoadDefaultIcon()
        {
            // Use PresentationLocator instead of static AssetLoader, so it won't fail on Unit Tests without any asset loader.
            if (PresentationLocator.Current.GetService<IAssetLoader>() is { } assetLoader
                && Assembly.GetEntryAssembly()?.GetName()?.Name is { } assemblyName
                && Uri.TryCreate($"csres://{assemblyName}/!__CornerstoneDefaultWindowIcon", UriKind.Absolute, out var path)
                && assetLoader.Exists(path))
            {
                using var stream = assetLoader.Open(path);
                return new WindowIcon(stream);
            }
            return null;
        }

        private void SetEffectiveIcon(WindowIcon? icon)
        {
            icon ??= _shown ? s_defaultIcon.Value : null;
            PlatformImpl?.SetIcon(icon?.PlatformImpl);
        }

        public bool IsOnScreen()
        {
            var screens = Screens?.All;
            if ((screens == null) || (screens.Count == 0))
            {
                // Screens are not available yet (window constructor). Do not treat as off-screen.
                return true;
            }

            var pixelSize = PixelSize.FromSize(new Size(Width, Height), DesktopScaling);
            var windowRect = new PixelRect(Position, pixelSize);
            return screens.Any(s => s.WorkingArea.Intersects(windowRect));
        }

        public void CenterOnScreen()
        {
            var screen = Screens?.Primary;
            if (screen == null)
            {
                return;
            }

            var dipWidth = Math.Max(Bounds.Width, double.IsNaN(Width) ? 0 : Width);
            var dipHeight = Math.Max(Bounds.Height, double.IsNaN(Height) ? 0 : Height);
            var pixelSize = PixelSize.FromSize(new Size(dipWidth, dipHeight), DesktopScaling);
            var area = screen.WorkingArea;
            var left = area.X + ((area.Width - pixelSize.Width) / 2);
            var top = area.Y + ((area.Height - pixelSize.Height) / 2);
            Position = new PixelPoint(left, top);
        }

        private void BindTitleBarChrome(TemplateAppliedEventArgs e)
        {
            _minimizeButton = e.NameScope.Find<Button>("MinimizeButton");
            _maximizeButton = e.NameScope.Find<Button>("MaximizeButton");
            _maximizeIcon = e.NameScope.Find<Path>("MaximizeIcon");
            _closeButton = e.NameScope.Find<Button>("CloseButton");
            _windowIcon = e.NameScope.Find<Image>("WindowIcon");
            _titleBar = e.NameScope.Find<Border>("TitleBar");

            if (_minimizeButton != null)
            {
                _minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
            }

            if (_maximizeButton != null)
            {
                _maximizeButton.Click += (_, routed) => ToggleMaximized(routed);
            }

            if (_closeButton != null)
            {
                _closeButton.Click += (_, _) => Close();
            }

            if (_windowIcon != null)
            {
                _windowIcon.DoubleTapped += (_, _) => Close();
            }

            if (_titleBar != null)
            {
                _titleBar.DoubleTapped += (_, routed) => ToggleMaximized(routed);
            }

            if (_maximizeIcon != null)
            {
                this.GetObservable(WindowStateProperty).Subscribe(OnWindowStateForMaximizeIcon);
            }
        }

        private void ToggleMaximized(RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Normal
                ? WindowState.Maximized
                : WindowState.Normal;
        }

        private void OnWindowStateForMaximizeIcon(WindowState state)
        {
            if (_maximizeIcon == null)
            {
                return;
            }

            if (state == WindowState.Maximized)
            {
                _maximizeIcon.Data = Geometry.Parse("M2048 1638h-410v410h-1638v-1638h410v-410h1638v1638zm-614-1024h-1229v1229h1229v-1229zm409-409h-1229v205h1024v1024h205v-1229z");
            }
            else
            {
                _maximizeIcon.Data = Geometry.Parse("M2048 2048v-2048h-2048v2048h2048zM1843 1843h-1638v-1638h1638v1638z");
            }
        }

        private static bool CoerceCanMaximize(PresentationObject target, bool value)
            => value && target is not Window { CanResize: false };

        private sealed class WindowExitCommand : ICommand
        {
            private readonly Window _window;

            public WindowExitCommand(Window window)
            {
                _window = window;
            }

#pragma warning disable CS0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

            public bool CanExecute(object parameter)
            {
                return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;
            }

            public void Execute(object parameter)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            }
        }
    }
}
