#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Layout mode for <see cref="AdaptiveFillLayout" />.
/// </summary>
public enum AdaptiveFillLayoutMode
{
	/// <summary> Choose fill vs compact from viewport size (default). </summary>
	Auto = 0,

	/// <summary> Header Auto; body fills remaining height; page scroll off. </summary>
	Fill = 1,

	/// <summary> Page scroll on; body uses a finite height so nested scrollers work. </summary>
	Compact = 2
}

/// <summary>
/// Headered body layout that fills remaining height on large viewports and switches to
/// page-scroll + constrained body height on small (phone) viewports.
/// </summary>
/// <remarks>
/// Use when a page has chrome (docs, charts) above an interactive region (list, editor, tabs)
/// that must scroll internally instead of growing the page forever.
/// </remarks>
public class AdaptiveFillLayout : HeaderedContentControl
{
	#region Fields

	public static readonly StyledProperty<double> BodyHeightFractionProperty =
		AvaloniaProperty.Register<AdaptiveFillLayout, double>(nameof(BodyHeightFraction), 0.48);

	public static readonly StyledProperty<double> BodyMaxHeightProperty =
		AvaloniaProperty.Register<AdaptiveFillLayout, double>(nameof(BodyMaxHeight), 560);

	public static readonly StyledProperty<double> BodyMinHeightProperty =
		AvaloniaProperty.Register<AdaptiveFillLayout, double>(nameof(BodyMinHeight), 220);

	public static readonly DirectProperty<AdaptiveFillLayout, double> BodyHeightProperty =
		AvaloniaProperty.RegisterDirect<AdaptiveFillLayout, double>(nameof(BodyHeight), o => o.BodyHeight);

	public static readonly StyledProperty<double> CompactEnterHeightProperty =
		AvaloniaProperty.Register<AdaptiveFillLayout, double>(nameof(CompactEnterHeight), 700);

	public static readonly StyledProperty<double> CompactEnterWidthProperty =
		AvaloniaProperty.Register<AdaptiveFillLayout, double>(nameof(CompactEnterWidth), 600);

	public static readonly StyledProperty<double> CompactExitHeightProperty =
		AvaloniaProperty.Register<AdaptiveFillLayout, double>(nameof(CompactExitHeight), 740);

	public static readonly StyledProperty<double> CompactExitWidthProperty =
		AvaloniaProperty.Register<AdaptiveFillLayout, double>(nameof(CompactExitWidth), 640);

	public static readonly StyledProperty<AdaptiveFillLayoutMode> ForceModeProperty =
		AvaloniaProperty.Register<AdaptiveFillLayout, AdaptiveFillLayoutMode>(nameof(ForceMode));

	public static readonly DirectProperty<AdaptiveFillLayout, bool> IsCompactProperty =
		AvaloniaProperty.RegisterDirect<AdaptiveFillLayout, bool>(nameof(IsCompact), o => o.IsCompact);

	private double _bodyHeight;
	private ContentPresenter _bodyPresenter;
	private bool _isCompact = true;
	private bool _layoutIsCompact = true;
	private Grid _layoutRoot;
	private ScrollViewer _scrollViewer;

	#endregion

	#region Constructors

	static AdaptiveFillLayout()
	{
		AffectsArrange<AdaptiveFillLayout>(
			BodyHeightFractionProperty,
			BodyMinHeightProperty,
			BodyMaxHeightProperty,
			CompactEnterHeightProperty,
			CompactEnterWidthProperty,
			CompactExitHeightProperty,
			CompactExitWidthProperty,
			ForceModeProperty);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Applied body height in compact mode (also available for diagnostics). Clear in fill mode.
	/// </summary>
	public double BodyHeight
	{
		get => _bodyHeight;
		private set => SetAndRaise(BodyHeightProperty, ref _bodyHeight, value);
	}

	/// <summary> Fraction of viewport height used for the body in compact mode. </summary>
	public double BodyHeightFraction
	{
		get => GetValue(BodyHeightFractionProperty);
		set => SetValue(BodyHeightFractionProperty, value);
	}

	/// <summary> Maximum body height in compact mode. </summary>
	public double BodyMaxHeight
	{
		get => GetValue(BodyMaxHeightProperty);
		set => SetValue(BodyMaxHeightProperty, value);
	}

	/// <summary> Minimum body height in compact mode. </summary>
	public double BodyMinHeight
	{
		get => GetValue(BodyMinHeightProperty);
		set => SetValue(BodyMinHeightProperty, value);
	}

	/// <summary> Enter compact when viewport height is below this (when <see cref="ForceMode" /> is Auto). </summary>
	public double CompactEnterHeight
	{
		get => GetValue(CompactEnterHeightProperty);
		set => SetValue(CompactEnterHeightProperty, value);
	}

	/// <summary> Enter compact when viewport width is below this (when <see cref="ForceMode" /> is Auto). </summary>
	public double CompactEnterWidth
	{
		get => GetValue(CompactEnterWidthProperty);
		set => SetValue(CompactEnterWidthProperty, value);
	}

	/// <summary> Leave compact only when height is at least this (hysteresis). </summary>
	public double CompactExitHeight
	{
		get => GetValue(CompactExitHeightProperty);
		set => SetValue(CompactExitHeightProperty, value);
	}

	/// <summary> Leave compact only when width is at least this (hysteresis). </summary>
	public double CompactExitWidth
	{
		get => GetValue(CompactExitWidthProperty);
		set => SetValue(CompactExitWidthProperty, value);
	}

	/// <summary> Override automatic mode selection (useful for tests). </summary>
	public AdaptiveFillLayoutMode ForceMode
	{
		get => GetValue(ForceModeProperty);
		set => SetValue(ForceModeProperty, value);
	}

	/// <summary> True when using page scroll and a constrained body height. </summary>
	public bool IsCompact
	{
		get => _isCompact;
		private set => SetAndRaise(IsCompactProperty, ref _isCompact, value);
	}

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
		_layoutRoot = e.NameScope.Find<Grid>("PART_LayoutRoot");
		_bodyPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");

		UpdateLayoutMode(Bounds.Size, force: true);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == BodyHeightFractionProperty)
			|| (change.Property == BodyMinHeightProperty)
			|| (change.Property == BodyMaxHeightProperty)
			|| (change.Property == CompactEnterHeightProperty)
			|| (change.Property == CompactEnterWidthProperty)
			|| (change.Property == CompactExitHeightProperty)
			|| (change.Property == CompactExitWidthProperty)
			|| (change.Property == ForceModeProperty))
		{
			UpdateLayoutMode(Bounds.Size, force: true);
		}
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		base.OnSizeChanged(e);
		UpdateLayoutMode(e.NewSize, force: false);
	}

	/// <summary>
	/// Fill: header Auto + body *; page scroll off.
	/// Compact: stack + fixed body height; page scroll on.
	/// </summary>
	private void ApplyLayoutMode(bool compact, Size viewport)
	{
		_layoutIsCompact = compact;
		IsCompact = compact;

		if ((_scrollViewer is null) || (_layoutRoot is null) || (_bodyPresenter is null))
		{
			return;
		}

		if (compact)
		{
			_scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			_layoutRoot.RowDefinitions = new RowDefinitions("Auto,Auto");
			_bodyPresenter.VerticalAlignment = VerticalAlignment.Top;

			var height = Math.Clamp(
				viewport.Height * BodyHeightFraction,
				BodyMinHeight,
				BodyMaxHeight);
			BodyHeight = height;
			_bodyPresenter.Height = height;
		}
		else
		{
			_scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			_layoutRoot.RowDefinitions = new RowDefinitions("Auto,*");
			_bodyPresenter.ClearValue(HeightProperty);
			_bodyPresenter.VerticalAlignment = VerticalAlignment.Stretch;
			BodyHeight = double.NaN;
		}
	}

	private bool ResolveCompact(Size size)
	{
		return ForceMode switch
		{
			AdaptiveFillLayoutMode.Fill => false,
			AdaptiveFillLayoutMode.Compact => true,
			_ => ShouldUseCompactLayout(_layoutIsCompact, size)
		};
	}

	private bool ShouldUseCompactLayout(bool currentlyCompact, Size size)
	{
		if ((size.Width <= 0) || (size.Height <= 0))
		{
			return currentlyCompact;
		}

		// Hysteresis: harder to leave compact than to enter, avoids flicker at the boundary.
		if (currentlyCompact)
		{
			var canLeaveCompact = (size.Height >= CompactExitHeight) && (size.Width >= CompactExitWidth);
			return !canLeaveCompact;
		}

		return (size.Height < CompactEnterHeight) || (size.Width < CompactEnterWidth);
	}

	private void UpdateLayoutMode(Size viewport, bool force)
	{
		if ((viewport.Width <= 0) || (viewport.Height <= 0))
		{
			return;
		}

		var compact = ResolveCompact(viewport);
		if (force || (compact != _layoutIsCompact))
		{
			ApplyLayoutMode(compact, viewport);
			return;
		}

		// Stay compact: still refresh body height when the viewport changes.
		if (compact && (_bodyPresenter is not null))
		{
			var height = Math.Clamp(
				viewport.Height * BodyHeightFraction,
				BodyMinHeight,
				BodyMaxHeight);
			if (Math.Abs(BodyHeight - height) > 0.5)
			{
				BodyHeight = height;
				_bodyPresenter.Height = height;
			}
		}
	}

	#endregion
}
