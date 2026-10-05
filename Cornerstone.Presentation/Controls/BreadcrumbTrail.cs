#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;

#endregion

namespace Cornerstone.Presentation.Controls;

public partial class BreadcrumbTrail : TemplatedControl
{
	#region Fields

	private const double EllipsisPadding = 16;

	private const double ItemGap = 10;

	private const double SeparatorChrome = 30;

	private Button _ellipsis;

	private int _hidden = -1;

	#endregion

	#region Constructors

	public BreadcrumbTrail()
	{
		Items = new ObservableCollection<BreadcrumbSegment>();
		OverflowItems = new ObservableCollection<BreadcrumbSegment>();
		VisibleItems = new ObservableCollection<BreadcrumbSegmentView>();
		SelectCrumbCommand = new RelayCommand(OnSelectCrumb);
		Items.CollectionChanged += OnItemsChanged;
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial bool HasOverflow { get; set; }

	public ObservableCollection<BreadcrumbSegment> Items { get; }

	public ObservableCollection<BreadcrumbSegment> OverflowItems { get; }

	public ICommand SelectCrumbCommand { get; }

	public ObservableCollection<BreadcrumbSegmentView> VisibleItems { get; }

	#endregion

	#region Methods

	/// <summary>
	/// How many leading crumbs to hide so the rest, plus an ellipsis slot, fit in <paramref name="availableWidth"/>.
	/// The last crumb is never hidden. A single crumb never collapses.
	/// </summary>
	public static int HiddenPrefixCount(IReadOnlyList<double> segmentWidths, double itemGap, double availableWidth, double ellipsisSlot)
	{
		if ((segmentWidths is null) || (segmentWidths.Count <= 1))
		{
			return 0;
		}

		if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth))
		{
			return 0;
		}

		if (RowWidth(segmentWidths, 0, itemGap, 0) <= availableWidth)
		{
			return 0;
		}

		var count = segmentWidths.Count;
		for (var hidden = 1; hidden < count; hidden++)
		{
			if (RowWidth(segmentWidths, hidden, itemGap, ellipsisSlot) <= availableWidth)
			{
				return hidden;
			}
		}

		return count - 1;
	}

	public void SetItems(IEnumerable<BreadcrumbSegment> segments)
	{
		Items.Clear();
		if (segments is null)
		{
			return;
		}

		foreach (var segment in segments)
		{
			if (segment is not null)
			{
				Items.Add(segment);
			}
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		// Keep every crumb. The trail wraps onto the next line instead of collapsing a prefix.
		if (_hidden != 0)
		{
			_hidden = 0;
			ApplySplit(0);
		}

		return base.MeasureOverride(availableSize);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_ellipsis is not null)
		{
			_ellipsis.Click -= OnEllipsisClick;
		}

		base.OnApplyTemplate(e);
		_ellipsis = e.NameScope.Find<Button>("PART_Ellipsis");
		if (_ellipsis is not null)
		{
			_ellipsis.Click += OnEllipsisClick;
		}

		_hidden = -1;
		InvalidateMeasure();
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if ((change.Property == FontSizeProperty) || (change.Property == FontFamilyProperty) || (change.Property == PaddingProperty))
		{
			_hidden = -1;
			InvalidateMeasure();
		}
	}

	private void ApplySplit(int hidden)
	{
		if (hidden < 0)
		{
			hidden = 0;
		}

		if (hidden >= Items.Count)
		{
			hidden = Math.Max(0, Items.Count - 1);
		}

		OverflowItems.Clear();
		VisibleItems.Clear();
		for (var i = 0; i < Items.Count; i++)
		{
			var segment = Items[i];
			if (i < hidden)
			{
				OverflowItems.Add(segment);
				continue;
			}

			VisibleItems.Add(new BreadcrumbSegmentView(segment, segment.ShowSeparator || (i > 0)));
		}

		HasOverflow = OverflowItems.Count > 0;
	}

	private double MeasureEllipsisSlot()
	{
		return MeasureText("…") + EllipsisPadding + ItemGap;
	}

	private double[] MeasureSegments()
	{
		var widths = new double[Items.Count];
		for (var i = 0; i < Items.Count; i++)
		{
			var segment = Items[i];
			var chrome = (segment.ShowSeparator || (i > 0)) ? SeparatorChrome : 0;
			widths[i] = MeasureText(segment.Title) + chrome;
		}

		return widths;
	}

	private double MeasureText(string text)
	{
		var family = FontFamily ?? FontFamily.Default;
		var typeface = new Typeface(family, FontStyle, FontWeight);
		using var layout = new TextLayout(text ?? string.Empty, typeface, FontSize);
		return layout.WidthIncludingTrailingWhitespace;
	}

	private void OnEllipsisClick(object sender, RoutedEventArgs e)
	{
		if ((_ellipsis is null) || (OverflowItems.Count == 0))
		{
			return;
		}

		var menu = new MenuFlyout
		{
			Placement = PlacementMode.BottomEdgeAlignedLeft
		};

		foreach (var segment in OverflowItems)
		{
			menu.Items.Add(new MenuItem
			{
				Header = segment.Title,
				Command = SelectCrumbCommand,
				CommandParameter = segment
			});
		}

		menu.ShowAt(_ellipsis);
	}

	private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		_hidden = -1;
		InvalidateMeasure();
	}

	private void OnSelectCrumb(object parameter)
	{
		if (parameter is not BreadcrumbSegment segment || segment.IsCurrent)
		{
			return;
		}

		var index = Items.IndexOf(segment);
		if (index < 0)
		{
			return;
		}

		CrumbClicked?.Invoke(this, new BreadcrumbClickedEventArgs(segment, index));
	}

	private static double RowWidth(IReadOnlyList<double> segmentWidths, int hidden, double itemGap, double ellipsisSlot)
	{
		var width = hidden > 0 ? ellipsisSlot : 0;
		var visible = 0;
		for (var i = hidden; i < segmentWidths.Count; i++)
		{
			if (visible > 0)
			{
				width += itemGap;
			}

			width += segmentWidths[i];
			visible++;
		}

		return width;
	}

	#endregion

	#region Events

	public event EventHandler<BreadcrumbClickedEventArgs> CrumbClicked;

	#endregion
}
