#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls.Acrylic;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Primitives.PopupPositioning;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Rendering.Composition;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubWindowImpl : IWindowImpl, IPopupImpl
{
	#region Fields

	private Size _clientSize;

	private readonly Dictionary<Type, object> _features;
	private WeakReference<StubWindowImpl> _lastPopup;
	private bool _needsManagedDecorations;
	private WindowState _windowState;

	#endregion

	#region Constructors

	public StubWindowImpl(double initialWidth = 800, double initialHeight = 600, Compositor compositor = null,
		IWindowBaseImpl popupParent = null)
	{
		Calls = new StubCallLog();
		_features = new Dictionary<Type, object>();
		_clientSize = new Size(initialWidth, initialHeight);
		MaxAutoSizeHint = new Size(1280, 1024);
		_windowState = WindowState.Normal;
		DesktopScaling = 1;
		RenderScaling = 1;
		Surfaces = Array.Empty<IPlatformRenderSurface>();
		Handle = new PlatformHandle(IntPtr.Zero, "STUB");
		Compositor = compositor ?? RendererMocks.CreateDummyCompositor();
		Screens = new StubScreenImpl(new MockScreen(1, new PixelRect(0, 0, 1280, 1024), new PixelRect(0, 0, 1280, 1024), true));
		TransparencyLevel = WindowTransparencyLevel.None;
		AcrylicCompensationLevels = new AcrylicPlatformCompensationLevels(1, 1, 1);
		RequestedDrawnDecorations = default;
		ExtendedMargins = default;
		OffScreenMargin = default;
		WindowStateGetterIsUsable = false;
		AllowedWindowActions = PlatformAllowedWindowActions.All;
		AutoActivateOnShow = false;
		RaiseClosedOnDispose = popupParent != null;

		if (popupParent != null)
		{
			PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(popupParent,
				(pos, size, _) =>
				{
					Position = pos;
					PositionChanged?.Invoke(pos);
					Resize(size, WindowResizeReason.Unspecified);
				}));
		}
	}

	#endregion

	#region Properties

	public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; set; }

	public Action Activated { get; set; }

	public PlatformAllowedWindowActions AllowedWindowActions { get; set; }
	public bool AutoActivateOnShow { get; set; }
	public StubCallLog Calls { get; }

	public Size ClientSize
	{
		get => ClientSizeGetter != null ? ClientSizeGetter() : _clientSize;
		set => _clientSize = value;
	}

	public Func<Size> ClientSizeGetter { get; set; }

	public Action Closed { get; set; }
	public Func<WindowCloseReason, bool> Closing { get; set; }
	public Compositor Compositor { get; set; }
	public Func<IPopupImpl> CreatePopupHandler { get; set; }
	public Action Deactivated { get; set; }
	public double DesktopScaling { get; set; }

	public Action Disposing { get; set; }
	public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }
	public Thickness ExtendedMargins { get; set; }
	public Size? FrameSize { get; set; }
	public Action GotInputWhenDisabled { get; set; }
	public IPlatformHandle Handle { get; set; }
	public Action<RawInputEventArgs> Input { get; set; }
	public bool IsClientAreaExtendedToDecorations { get; set; }
	public Action LostFocus { get; set; }
	public Size MaxAutoSizeHint { get; set; }

	public bool NeedsManagedDecorations
	{
		get => NeedsManagedDecorationsGetter != null ? NeedsManagedDecorationsGetter() : _needsManagedDecorations;
		set => _needsManagedDecorations = value;
	}

	public Func<bool> NeedsManagedDecorationsGetter { get; set; }
	public Thickness OffScreenMargin { get; set; }
	public Action<Rect> Paint { get; set; }

	public Func<PixelPoint, Point> PointToClientHandler { get; set; }

	public Func<Point, PixelPoint> PointToScreenHandler { get; set; }
	public IPopupPositioner PopupPositioner { get; set; }
	public PixelPoint Position { get; set; }
	public Action<PixelPoint> PositionChanged { get; set; }
	public bool RaiseClosedOnDispose { get; set; }
	public double RenderScaling { get; set; }
	public PlatformRequestedDrawnDecoration RequestedDrawnDecorations { get; set; }

	public Action<Size, WindowResizeReason> ResizeHandler { get; set; }
	public Action<Size, WindowResizeReason> Resized { get; set; }
	public Action<double> ScalingChanged { get; set; }
	public StubScreenImpl Screens { get; set; }

	public Action<ICursorImpl> SetCursorHandler { get; set; }
	public IPlatformRenderSurface[] Surfaces { get; set; }
	public WindowTransparencyLevel TransparencyLevel { get; set; }
	public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

	public WindowState WindowState
	{
		get
		{
			WindowStateGetCount++;
			Calls.Add("get_WindowState");
			return WindowStateGetter != null ? WindowStateGetter() : _windowState;
		}
		set
		{
			Calls.Add("set_WindowState", value);
			if (WindowStateSetHandler != null)
			{
				WindowStateSetHandler(value);
			}
			else
			{
				_windowState = value;
				WindowStateChanged?.Invoke(value);
			}
		}
	}

	public Action<WindowState> WindowStateChanged { get; set; }
	public int WindowStateGetCount { get; private set; }

	public Func<WindowState> WindowStateGetter { get; set; }

	public bool WindowStateGetterIsUsable { get; set; }
	public Action<WindowState> WindowStateSetHandler { get; set; }

	#endregion

	#region Methods

	public void Activate()
	{
		Calls.Add(nameof(Activate));
		Activated?.Invoke();
	}

	public void BeginMoveDrag(PointerPressedEventArgs e)
	{
		Calls.Add(nameof(BeginMoveDrag), e);
	}

	public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
	{
		Calls.Add(nameof(BeginResizeDrag), edge, e);
	}

	public void CanResize(bool value)
	{
		Calls.Add(nameof(CanResize), value);
	}

	public IPopupImpl CreatePopup()
	{
		Calls.Add(nameof(CreatePopup));
		if (CreatePopupHandler != null)
		{
			var popup = CreatePopupHandler();
			TrackPopup(popup as StubWindowImpl);
			return popup;
		}

		var created = new StubWindowImpl(popupParent: this);
		TrackPopup(created);
		return created;
	}

	public void Dispose()
	{
		Calls.Add(nameof(Dispose));
		Disposing?.Invoke();
		if (RaiseClosedOnDispose)
		{
			LostFocus?.Invoke();
			Closed?.Invoke();
		}
	}

	public void Hide()
	{
		Calls.Add(nameof(Hide));
	}

	public void Move(PixelPoint point)
	{
		Calls.Add(nameof(Move), point);
		Position = point;
		PositionChanged?.Invoke(point);
	}

	public Point PointToClient(PixelPoint point)
	{
		if (PointToClientHandler != null)
		{
			return PointToClientHandler(point);
		}

		return (point - Position).ToPoint(1);
	}

	public PixelPoint PointToScreen(Point point)
	{
		if (PointToScreenHandler != null)
		{
			return PointToScreenHandler(point);
		}

		return PixelPoint.FromPoint(point, 1) + Position;
	}

	public void ReleaseCallbacks()
	{
		StubWindowImpl popup = null;
		_lastPopup?.TryGetTarget(out popup);
		_lastPopup = null;
		popup?.ReleaseCallbacks();
		Activated = null;
		Closed = null;
		Closing = null;
		CreatePopupHandler = null;
		Deactivated = null;
		GotInputWhenDisabled = null;
		Input = null;
		LostFocus = null;
		Paint = null;
		PositionChanged = null;
		Resized = null;
		ScalingChanged = null;
		TransparencyLevelChanged = null;
		WindowStateChanged = null;
		WindowStateSetHandler = null;
		WindowStateGetter = null;
		ClientSizeGetter = null;
		NeedsManagedDecorationsGetter = null;
		PointToClientHandler = null;
		PointToScreenHandler = null;
		ResizeHandler = null;
		Disposing = null;
		Compositor = null;
		Screens = null;
		Calls.Clear();
	}

	public void Resize(Size clientSize, WindowResizeReason reason = WindowResizeReason.Application)
	{
		Calls.Add(nameof(Resize), clientSize, reason);
		if (ResizeHandler != null)
		{
			ResizeHandler(clientSize, reason);
			return;
		}

		var constrained = clientSize.Constrain(MaxAutoSizeHint);
		if (constrained != ClientSize)
		{
			ClientSize = constrained;
			Resized?.Invoke(ClientSize, reason);
		}
	}

	public void SetCanMaximize(bool value)
	{
		Calls.Add(nameof(SetCanMaximize), value);
	}

	public void SetCanMinimize(bool value)
	{
		Calls.Add(nameof(SetCanMinimize), value);
	}

	public void SetClientSize(Size size)
	{
		ClientSize = size;
	}

	public void SetCursor(ICursorImpl cursor)
	{
		Calls.Add(nameof(SetCursor), cursor);
		SetCursorHandler?.Invoke(cursor);
	}

	public void SetEnabled(bool enable)
	{
		Calls.Add(nameof(SetEnabled), enable);
	}

	public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
	{
		Calls.Add(nameof(SetExtendClientAreaTitleBarHeightHint), titleBarHeight);
	}

	public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
	{
		Calls.Add(nameof(SetExtendClientAreaToDecorationsHint), extendIntoClientAreaHint);
	}

	public void SetFeature(Type featureType, object value)
	{
		_features[featureType] = value;
	}

	public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
	{
		Calls.Add(nameof(SetFrameThemeVariant), themeVariant);
	}

	public void SetHitTestVisible(bool isHitTestVisible)
	{
		Calls.Add(nameof(SetHitTestVisible), isHitTestVisible);
	}

	public void SetIcon(IWindowIconImpl icon)
	{
		Calls.Add(nameof(SetIcon), icon);
	}

	public void SetInputRoot(IInputRoot inputRoot)
	{
		Calls.Add(nameof(SetInputRoot), inputRoot);
	}

	public void SetMaxAutoSizeHint(Size size)
	{
		MaxAutoSizeHint = size;
	}

	public void SetMinMaxSize(Size minSize, Size maxSize)
	{
		Calls.Add(nameof(SetMinMaxSize), minSize, maxSize);
	}

	public void SetParent(IWindowImpl parent)
	{
		Calls.Add(nameof(SetParent), parent);
	}

	public void SetTitle(string title)
	{
		Calls.Add(nameof(SetTitle), title);
	}

	public void SetTopmost(bool value)
	{
		Calls.Add(nameof(SetTopmost), value);
	}

	public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
	{
		Calls.Add(nameof(SetTransparencyLevelHint), transparencyLevels);
	}

	public void SetWindowDecorations(WindowDecorations enabled)
	{
		Calls.Add(nameof(SetWindowDecorations), enabled);
	}

	public void SetWindowManagerAddShadowHint(bool enabled)
	{
		Calls.Add(nameof(SetWindowManagerAddShadowHint), enabled);
	}

	public void SetWindowStateBacking(WindowState value)
	{
		_windowState = value;
	}

	public void Show(bool activate, bool isDialog)
	{
		Calls.Add(nameof(Show), activate, isDialog);
		if (activate)
		{
			Resized?.Invoke(ClientSize, WindowResizeReason.Unspecified);
			if (AutoActivateOnShow)
			{
				Activated?.Invoke();
			}
		}
	}

	public void ShowTaskbarIcon(bool value)
	{
		Calls.Add(nameof(ShowTaskbarIcon), value);
	}

	public void TakeFocus()
	{
		Calls.Add(nameof(TakeFocus));
	}

	public object TryGetFeature(Type featureType)
	{
		if (_features.TryGetValue(featureType, out var value))
		{
			return value;
		}

		if (featureType == typeof(IScreenImpl))
		{
			return Screens;
		}

		return null;
	}

	private void TrackPopup(StubWindowImpl popup)
	{
		_lastPopup = popup == null ? null : new WeakReference<StubWindowImpl>(popup);
	}

	#endregion
}