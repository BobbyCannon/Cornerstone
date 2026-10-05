#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Controls.DockingManager;
using Cornerstone.Presentation.VisualTree;
using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;

#endregion

namespace Cornerstone.Presentation.Controls.Input;

/// <summary>
/// When a parent is shown (active dock pane, selected tab, open popup/flyout),
/// moves keyboard focus to the first marked descendant.
/// </summary>
public static class DefaultFocus
{
	#region Fields

	public static readonly AttachedProperty<bool> IsTargetProperty;
	public static readonly AttachedProperty<bool> ReapplyWhenHiddenProperty;
	private static bool _registered;

	#endregion

	#region Constructors

	static DefaultFocus()
	{
		IsTargetProperty = PresentationProperty.RegisterAttached<Control, bool>("IsTarget", typeof(DefaultFocus));
		ReapplyWhenHiddenProperty = PresentationProperty.RegisterAttached<Control, bool>("ReapplyWhenHidden", typeof(DefaultFocus));
		EnsureRegistered();
	}

	#endregion

	#region Methods

	/// <summary>
	/// After the next layout pass, focus the first IsTarget descendant of scope.
	/// </summary>
	public static void Apply(Visual scope)
	{
		EnsureRegistered();
		if (scope == null)
		{
			return;
		}

		Dispatcher.UIThread.Post(() => ApplyCore(scope), DispatcherPriority.Loaded);
	}

	public static void EnsureRegistered()
	{
		if (_registered)
		{
			return;
		}

		_registered = true;

		DockingTabControl.IsActiveProperty.Changed.AddClassHandler<DockingTabControl>(OnDockingIsActiveChanged);
		TabControl.SelectedItemProperty.Changed.AddClassHandler<TabControl>(OnTabSelectedItemChanged);
		PopupView.DataContextProperty.Changed.AddClassHandler<PopupView>(OnPopupViewDataContextChanged);
		Popup.IsOpenProperty.Changed.AddClassHandler<Popup>(OnPopupIsOpenChanged);
		FlyoutBase.IsOpenProperty.Changed.AddClassHandler<FlyoutBase>(OnFlyoutIsOpenChanged);
		ReapplyWhenHiddenProperty.Changed.AddClassHandler<Control>(OnReapplyWhenHiddenChanged);
	}

	public static bool GetIsTarget(Control control)
	{
		return control.GetValue(IsTargetProperty);
	}

	public static bool GetReapplyWhenHidden(Control control)
	{
		return control.GetValue(ReapplyWhenHiddenProperty);
	}

	public static void SetIsTarget(Control control, bool value)
	{
		control.SetValue(IsTargetProperty, value);
	}

	public static void SetReapplyWhenHidden(Control control, bool value)
	{
		control.SetValue(ReapplyWhenHiddenProperty, value);
	}

	private static void ApplyCore(Visual scope)
	{
		if (scope is DockingTabControl dock)
		{
			if (dock.IsToolbar || !dock.IsActive)
			{
				return;
			}

			var overlay = FindVisiblePopupView(dock);
			if (overlay != null)
			{
				TryFocus(overlay, false);
				return;
			}

			TryFocus(dock, true);
			return;
		}

		if (scope is Control control && !control.IsEffectivelyVisible)
		{
			return;
		}

		TryFocus(scope, false);
	}

	private static Visual FindScope(Visual from)
	{
		foreach (var ancestor in from.GetVisualAncestors())
		{
			switch (ancestor)
			{
				case DockingTabControl dockingTabControl:
				{
					return dockingTabControl;
				}
				case PopupView popupView:
				{
					return popupView;
				}
				case Popup popup:
				{
					return popup.Child ?? popup;
				}
				case TabControl tabControl:
				{
					return tabControl;
				}
			}
		}

		return TopLevel.GetTopLevel(from) ?? from;
	}

	private static Control FindTarget(Visual root, bool skipVisiblePopupView)
	{
		if (root is Control control)
		{
			if (!control.IsEffectivelyVisible)
			{
				return null;
			}

			if (GetIsTarget(control) && control.Focusable)
			{
				return control;
			}
		}

		var children = root.GetVisualChildren();
		foreach (var child in children)
		{
			if (skipVisiblePopupView
				&& child is PopupView popupView
				&& popupView.IsEffectivelyVisible)
			{
				continue;
			}

			var found = FindTarget(child, skipVisiblePopupView);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private static PopupView FindVisiblePopupView(Visual root)
	{
		if (root is PopupView popupView
			&& popupView.IsEffectivelyVisible
			&& popupView.DataContext is PopupViewModel)
		{
			return popupView;
		}

		foreach (var child in root.GetVisualChildren())
		{
			var found = FindVisiblePopupView(child);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private static void OnDockingIsActiveChanged(DockingTabControl tabControl, PresentationPropertyChangedEventArgs e)
	{
		if (e.GetNewValue<bool>())
		{
			Apply(tabControl);
		}
	}

	private static void OnFlyoutIsOpenChanged(FlyoutBase flyout, PresentationPropertyChangedEventArgs e)
	{
		if (!e.GetNewValue<bool>())
		{
			return;
		}

		if (flyout is PopupFlyoutBase popupFlyout)
		{
			Apply(popupFlyout.Popup?.Child ?? popupFlyout.Popup);
		}
	}

	private static void OnHiddenControlPropertyChanged(object sender, PresentationPropertyChangedEventArgs e)
	{
		if ((e.Property != Visual.IsVisibleProperty) || e.GetNewValue<bool>())
		{
			return;
		}

		if (sender is Visual visual)
		{
			Apply(FindScope(visual));
		}
	}

	private static void OnPopupIsOpenChanged(Popup popup, PresentationPropertyChangedEventArgs e)
	{
		if (e.GetNewValue<bool>())
		{
			Apply(popup.Child ?? popup);
		}
	}

	private static void OnPopupViewDataContextChanged(PopupView popupView, PresentationPropertyChangedEventArgs e)
	{
		if (e.NewValue is PopupViewModel)
		{
			Apply(popupView);
		}
	}

	private static void OnReapplyWhenHiddenChanged(Control control, PresentationPropertyChangedEventArgs e)
	{
		if (e.GetNewValue<bool>())
		{
			control.PropertyChanged += OnHiddenControlPropertyChanged;
		}
		else
		{
			control.PropertyChanged -= OnHiddenControlPropertyChanged;
		}
	}

	private static void OnTabSelectedItemChanged(TabControl tabControl, PresentationPropertyChangedEventArgs e)
	{
		if (tabControl is DockingTabControl dockingTabControl)
		{
			if (dockingTabControl.IsActive)
			{
				Apply(dockingTabControl);
			}

			return;
		}

		Apply(tabControl);
	}

	private static bool ShouldSkipBecauseAlreadyFocused(Visual root)
	{
		if (root is not InputElement input || !input.IsKeyboardFocusWithin)
		{
			return false;
		}

		var focused = TopLevel.GetTopLevel(root)?.FocusManager?.GetFocusedElement() as Visual;
		return (focused == null) || focused.IsEffectivelyVisible;
	}

	private static void TryFocus(Visual root, bool skipVisiblePopupView)
	{
		if (ShouldSkipBecauseAlreadyFocused(root))
		{
			return;
		}

		FindTarget(root, skipVisiblePopupView)?.Focus();
	}

	#endregion
}