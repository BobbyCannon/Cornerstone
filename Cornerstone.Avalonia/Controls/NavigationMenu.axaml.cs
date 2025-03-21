#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.Controls;

[DoNotNotify]
public class NavigationMenu : TabControl
{
	#region Fields

	public static readonly StyledProperty<bool> AutoCollapseOnSelectionChangeProperty;
	public static readonly StyledProperty<bool> AutoExpandOnResizeProperty;
	public static readonly StyledProperty<Color> ContentBackgroundProperty;
	public static readonly StyledProperty<object> CustomContentProperty;
	public static readonly StyledProperty<SplitViewDisplayMode> DisplayModeProperty;
	public static readonly StyledProperty<int> ExpandedModeThresholdWidthProperty;
	public static readonly StyledProperty<bool> IsOpenProperty;
	public static readonly StyledProperty<Color> PaneBackgroundProperty;
	private SplitView _splitView;

	#endregion

	#region Constructors

	static NavigationMenu()
	{
		AutoCollapseOnSelectionChangeProperty = AvaloniaProperty.Register<NavigationMenu, bool>(nameof(AutoCollapseOnSelectionChange), true);
		AutoExpandOnResizeProperty = AvaloniaProperty.Register<NavigationMenu, bool>(nameof(AutoExpandOnResize));
		ContentBackgroundProperty = AvaloniaProperty.Register<NavigationMenu, Color>(nameof(ContentBackground));
		CustomContentProperty = AvaloniaProperty.Register<NavigationMenu, object>(nameof(CustomContent));
		DisplayModeProperty = AvaloniaProperty.Register<NavigationMenu, SplitViewDisplayMode>(nameof(DisplayMode), SplitViewDisplayMode.CompactInline);
		ExpandedModeThresholdWidthProperty = AvaloniaProperty.Register<NavigationMenu, int>(nameof(ExpandedModeThresholdWidth), 1024);
		IsOpenProperty = AvaloniaProperty.Register<NavigationMenu, bool>(nameof(IsOpen));
		PaneBackgroundProperty = AvaloniaProperty.Register<NavigationMenu, Color>(nameof(PaneBackground));
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

	public Color ContentBackground
	{
		get => GetValue(ContentBackgroundProperty);
		set => SetValue(ContentBackgroundProperty, value);
	}

	public object CustomContent
	{
		get => GetValue(CustomContentProperty);
		set => SetValue(CustomContentProperty, value);
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

	public bool IsOpen
	{
		get => GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	public Color PaneBackground
	{
		get => GetValue(PaneBackgroundProperty);
		set => SetValue(PaneBackgroundProperty, value);
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