#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class NavigationMenu : TabControl
{
	#region Fields

	public static readonly StyledProperty<bool> AutoCollapseOnSelectionChangeProperty;
	public static readonly StyledProperty<bool> AutoExpandOnResizeProperty;
	public static readonly StyledProperty<IBrush> ContentBackgroundProperty;
	public static readonly StyledProperty<SplitViewDisplayMode> DisplayModeProperty;
	public static readonly StyledProperty<int> ExpandedModeThresholdWidthProperty;
	public static readonly StyledProperty<string> IconNameProperty;
	public static readonly StyledProperty<object> InnerBottomContentProperty;
	public static readonly StyledProperty<object> InnerTopContentProperty;
	public static readonly StyledProperty<bool> IsOpenProperty;
	public static readonly StyledProperty<IBrush> PaneBackgroundProperty;
	public static readonly StyledProperty<double> PaneCollapsedWidthProperty;
	public static readonly StyledProperty<double> PaneExpandedWidthProperty;
	public static readonly StyledProperty<bool> ShowMenuButtonProperty;
	private SplitView _splitView;

	#endregion

	#region Constructors

	static NavigationMenu()
	{
		AutoCollapseOnSelectionChangeProperty = AvaloniaProperty.Register<NavigationMenu, bool>(nameof(AutoCollapseOnSelectionChange), true);
		AutoExpandOnResizeProperty = AvaloniaProperty.Register<NavigationMenu, bool>(nameof(AutoExpandOnResize));
		ContentBackgroundProperty = AvaloniaProperty.Register<NavigationMenu, IBrush>(nameof(ContentBackground));
		DisplayModeProperty = AvaloniaProperty.Register<NavigationMenu, SplitViewDisplayMode>(nameof(DisplayMode), SplitViewDisplayMode.CompactInline);
		ExpandedModeThresholdWidthProperty = AvaloniaProperty.Register<NavigationMenu, int>(nameof(ExpandedModeThresholdWidth), 1024);
		ShowMenuButtonProperty = AvaloniaProperty.Register<NavigationMenu, bool>(nameof(ShowMenuButton), true);
		IsOpenProperty = AvaloniaProperty.Register<NavigationMenu, bool>(nameof(IsOpen));
		PaneExpandedWidthProperty = AvaloniaProperty.Register<NavigationMenu, double>(nameof(PaneExpandedWidth));
		PaneCollapsedWidthProperty = AvaloniaProperty.Register<NavigationMenu, double>(nameof(PaneCollapsedWidth));
		IconNameProperty = AvaloniaProperty.Register<NavigationMenu, string>(nameof(IconName), "Icons.Grid");
		PaneBackgroundProperty = AvaloniaProperty.Register<NavigationMenu, IBrush>(nameof(PaneBackground));
		InnerBottomContentProperty = AvaloniaProperty.Register<NavigationMenu, object>(nameof(InnerBottomContent));
		InnerTopContentProperty = AvaloniaProperty.Register<NavigationMenu, object>(nameof(InnerTopContent));
	}

	#endregion

	#region Properties

	public bool AutoCollapseOnSelectionChange
	{
		get => GetValue(AutoCollapseOnSelectionChangeProperty);
		set => SetValue(AutoCollapseOnSelectionChangeProperty, value);
	}

	public bool AutoExpandOnResize
	{
		get => GetValue(AutoExpandOnResizeProperty);
		set => SetValue(AutoExpandOnResizeProperty, value);
	}

	public IBrush ContentBackground
	{
		get => GetValue(ContentBackgroundProperty);
		set => SetValue(ContentBackgroundProperty, value);
	}

	public SplitViewDisplayMode DisplayMode
	{
		get => GetValue(DisplayModeProperty);
		set => SetValue(DisplayModeProperty, value);
	}

	public int ExpandedModeThresholdWidth
	{
		get => GetValue(ExpandedModeThresholdWidthProperty);
		set => SetValue(ExpandedModeThresholdWidthProperty, value);
	}

	public string IconName
	{
		get => GetValue(IconNameProperty);
		set => SetValue(IconNameProperty, value);
	}

	public object InnerBottomContent
	{
		get => GetValue(InnerBottomContentProperty);
		set => SetValue(InnerBottomContentProperty, value);
	}

	public object InnerTopContent
	{
		get => GetValue(InnerTopContentProperty);
		set => SetValue(InnerTopContentProperty, value);
	}

	public bool IsOpen
	{
		get => GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	public IBrush PaneBackground
	{
		get => GetValue(PaneBackgroundProperty);
		set => SetValue(PaneBackgroundProperty, value);
	}

	public double PaneCollapsedWidth
	{
		get => GetValue(PaneCollapsedWidthProperty);
		set => SetValue(PaneCollapsedWidthProperty, value);
	}

	public double PaneExpandedWidth
	{
		get => GetValue(PaneExpandedWidthProperty);
		set => SetValue(PaneExpandedWidthProperty, value);
	}

	public bool ShowMenuButton
	{
		get => GetValue(ShowMenuButtonProperty);
		set => SetValue(ShowMenuButtonProperty, value);
	}

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_splitView = e.NameScope.Find<SplitView>("PART_NavigationPane");
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == BoundsProperty) && _splitView is not null)
		{
			var (oldBounds, newBounds) = change.GetOldAndNewValue<Rect>();
			EnsureSplitViewMode(oldBounds, newBounds);
		}

		if (change.Property == SelectedItemProperty)
		{
			if (_splitView is not null
				&& ((_splitView.DisplayMode == SplitViewDisplayMode.Overlay)
					|| AutoCollapseOnSelectionChange))
			{
				IsOpen = false;
			}
		}
	}

	private void EnsureSplitViewMode(Rect oldBounds, Rect newBounds)
	{
		if (_splitView is not null && AutoExpandOnResize)
		{
			var threshold = ExpandedModeThresholdWidth;

			if (newBounds.Width >= threshold)
			{
				IsOpen = true;
			}
			else if (newBounds.Width < threshold)
			{
				IsOpen = false;
			}
		}
	}

	#endregion
}