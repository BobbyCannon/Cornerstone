#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Diagnostics;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Resources;

#endregion

namespace Cornerstone.Presentation.Diagnostics;

public partial class AppWindow : Window, IStyleHost
{
	#region Fields

	private readonly HashSet<Popup> _frozenPopupStates;
	private HotKeyConfiguration _hotKeys;
	private readonly IDisposable _inputSubscription;
	private PixelPoint _lastPointerPosition;
	private PresentationObject _root;

	#endregion

	#region Constructors

	public AppWindow()
	{
		InitializeComponent();

		// Apply the SimpleTheme.Window theme; this must be done after the XAML is parsed as
		// the theme is included in the AppWindow's XAML.
		if (Theme is null && this.FindResource(typeof(Window)) is ControlTheme windowTheme)
		{
			Theme = windowTheme;
		}

		_inputSubscription = Observable.Subscribe(InputManager.Instance?.Process, x =>
			{
				if (x is RawPointerEventArgs pointerEventArgs
					&& x.Root.RootElement is {} rootElement)
				{
					_lastPointerPosition = rootElement.PointToScreen(pointerEventArgs.Position);
				}
				else if (x is RawKeyEventArgs keyEventArgs && (keyEventArgs.Type == RawKeyEventType.KeyDown))
				{
					RawKeyDown(keyEventArgs);
				}
			});

		_frozenPopupStates = [];

		EventHandler lh = null;
		lh = (s, e) =>
		{
			Opened -= lh;
			if ((DataContext as AppViewModel)?.StartupScreenIndex is { } index)
			{
				var screens = Screens;
				if ((index > -1) && (index < screens.ScreenCount))
				{
					var screen = screens.All[index];
					Position = screen.Bounds.TopLeft;
					WindowState = WindowState.Maximized;
				}
			}
		};
		Opened += lh;
	}

	#endregion

	#region Properties

	public PresentationObject Root
	{
		get => _root;
		set
		{
			if (_root != value)
			{
				if (_root is ICloseable oldClosable)
				{
					oldClosable.Closed -= RootClosed;
				}

				_root = value;

				if (_root is ICloseable newClosable)
				{
					newClosable.Closed += RootClosed;
					DataContext = new AppViewModel(_root);
				}
				else
				{
					DataContext = null;
				}
			}
		}
	}

	IStyleHost IStyleHost.StylingParent => null;

	#endregion

	#region Methods

	public void SelectedControl(Control control)
	{
		if (control is not null)
		{
			(DataContext as AppViewModel)?.SelectControl(control);
		}
	}

	public void SetOptions(DiagnosticToolsOptions options)
	{
		_hotKeys = options.HotKeys;

		(DataContext as AppViewModel)?.SetOptions(options);
		if (options.ThemeVariant is { } themeVariant)
		{
			RequestedThemeVariant = themeVariant;
		}
	}

	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);
		_inputSubscription?.Dispose();

		foreach (var state in _frozenPopupStates)
		{
			state.Closing -= PopupOnClosing;
		}

		_frozenPopupStates.Clear();

		if (_root is ICloseable cloneable)
		{
			cloneable.Closed -= RootClosed;
			_root = null;
		}

		((AppViewModel) DataContext)?.Dispose();
	}

	private void FreezeValueFrames(AppViewModel vm)
	{
		vm.EnableSnapshotStyles(true);
	}

	private Control GetHoveredControl(TopLevel topLevel)
	{
		var point = topLevel.PointToClient(_lastPointerPosition);

		return (Control) topLevel.GetVisualsAt(point, x =>
			{
				if (x is AdornerLayer || !x.IsVisible)
				{
					return false;
				}

				return !(x is IInputElement ie) || ie.IsHitTestVisible;
			})
			.FirstOrDefault();
	}

	private static List<PopupRoot> GetPopupRoots(TopLevel root)
	{
		var popupRoots = new List<PopupRoot>();

		void ProcessProperty<T>(Control control, PresentationProperty<T> property)
		{
			if (control.GetValue(property) is IPopupHostProvider popupProvider
				&& popupProvider.PopupHost is PopupRoot popupRoot)
			{
				popupRoots.Add(popupRoot);
			}
		}

		foreach (var control in root.GetVisualDescendants().OfType<Control>())
		{
			if (control is Popup p && p.Host is PopupRoot popupRoot)
			{
				popupRoots.Add(popupRoot);
			}

			ProcessProperty(control, ContextFlyoutProperty);
			ProcessProperty(control, ContextMenuProperty);
			ProcessProperty(control, FlyoutBase.AttachedFlyoutProperty);
			ProcessProperty(control, ToolTipDiagnostics.ToolTipProperty);
			ProcessProperty(control, Button.FlyoutProperty);
		}

		return popupRoots;
	}

	private void InspectHoveredControl(TopLevel root, AppViewModel vm)
	{
		Control control = null;

		foreach (var popupRoot in GetPopupRoots(root))
		{
			control = GetHoveredControl(popupRoot);

			if (control != null)
			{
				break;
			}
		}

		control ??= GetHoveredControl(root);

		if (control != null)
		{
			vm.SelectControl(control);
		}
	}

	private void PopupOnClosing(object sender, CancelEventArgs e)
	{
		var vm = (AppViewModel) DataContext;
		if (vm?.FreezePopups == true)
		{
			e.Cancel = true;
		}
	}

	private void RawKeyDown(RawKeyEventArgs e)
	{
		var root = Application.GetTopLevel();
		if (_hotKeys is null ||
			DataContext is not AppViewModel vm)
		{
			return;
		}

		//if (root is PopupRoot pr && (pr.ParentTopLevel != null))
		//{
		//	root = pr.ParentTopLevel;
		//}

		var modifiers = MergeModifiers(e.Key, e.Modifiers.ToKeyModifiers());

		//if (IsMatched(_hotKeys.ValueFramesFreeze, e.Key, modifiers))
		//{
		//	FreezeValueFrames(vm);
		//}
		//else if (IsMatched(_hotKeys.ValueFramesUnfreeze, e.Key, modifiers))
		//{
		//	UnfreezeValueFrames(vm);
		//}
		//else if (IsMatched(_hotKeys.TogglePopupFreeze, e.Key, modifiers))
		//{
		//	ToggleFreezePopups(root, vm);
		//}
		//else
		if (IsMatched(_hotKeys.InspectHoveredControl, e.Key, modifiers))
		{
			InspectHoveredControl(root, vm);
		}

		static bool IsMatched(KeyGesture gesture, Key key, KeyModifiers modifiers)
		{
			return ((gesture.Key == key) || (gesture.Key == Key.None)) && modifiers.HasAllFlags(gesture.KeyModifiers);
		}

		// When Control, Shift, or Alt are initially pressed, they are the Key and not part of Modifiers
		// This merges so modifier keys alone can more easily trigger actions
		static KeyModifiers MergeModifiers(Key key, KeyModifiers modifiers)
		{
			return key switch
			{
				Key.LeftCtrl or Key.RightCtrl => modifiers | KeyModifiers.Control,
				Key.LeftShift or Key.RightShift => modifiers | KeyModifiers.Shift,
				Key.LeftAlt or Key.RightAlt => modifiers | KeyModifiers.Alt,
				_ => modifiers
			};
		}
	}

	private void RootClosed(object sender, EventArgs e)
	{
		Close();
	}

	private void ToggleFreezePopups(TopLevel root, AppViewModel vm)
	{
		vm.FreezePopups = !vm.FreezePopups;

		foreach (var popupRoot in GetPopupRoots(root))
		{
			if (popupRoot.Parent is Popup popup)
			{
				if (vm.FreezePopups)
				{
					popup.Closing += PopupOnClosing;
					_frozenPopupStates.Add(popup);
				}
				else
				{
					popup.Closing -= PopupOnClosing;
					_frozenPopupStates.Remove(popup);
				}
			}
		}
	}

	private void UnfreezeValueFrames(AppViewModel vm)
	{
		vm.EnableSnapshotStyles(false);
	}

	#endregion
}