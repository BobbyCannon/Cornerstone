#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;

#endregion

namespace Cornerstone.Avalonia.Controls;

public partial class LayoutGrid : Grid
{
	#region Constructors

	public LayoutGrid()
	{
		IsHorizontal = true;
		SplitGrid = true;
		MinHorizontalSize = 100;
		MinVerticalSize = 100;
		InitializeDefaults();
		this[!BackgroundProperty] = new DynamicResourceExtension("Background03");
	}

	static LayoutGrid()
	{
		IsHorizontalProperty.Changed.AddClassHandler<LayoutGrid>((o, _) => o.RefreshLayout());
		SplitGridProperty.Changed.AddClassHandler<LayoutGrid>((o, _) => o.RefreshLayout());
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial bool IsHorizontal { get; set; }

	/// <summary>
	/// Gets or sets the minimum size for content panes when IsHorizontal is true.
	/// </summary>
	public double MinHorizontalSize { get; set; }

	/// <summary>
	/// Gets or sets the minimum size for content panes when IsHorizontal is false.
	/// </summary>
	public double MinVerticalSize { get; set; }

	[StyledProperty]
	public partial bool SplitGrid { get; set; }

	protected override Type StyleKeyOverride => typeof(Grid);

	#endregion

	#region Methods

	public override void ApplyTemplate()
	{
		base.ApplyTemplate();
		RefreshLayout();
	}

	public static GridLength CalculateLength(double percent, bool first)
	{
		return percent is < 0 or >= 1
			? GridLength.Star
			: new GridLength(
				!first ? 1 - percent : percent,
				GridUnitType.Star
			);
	}

	public void RestoreSize(Size size)
	{
		RestoreSize(size.Height, size.Width);
	}

	/// <summary>
	/// Restore first-pane star shares for both orientations.
	/// </summary>
	/// <param name="heightPercent">First row share when vertical (0–1).</param>
	/// <param name="widthPercent">First column share when horizontal (0–1).</param>
	public void RestoreSize(double heightPercent, double widthPercent)
	{
		InitializeDefaults();
		RowDefinitions[0].Height = CalculateLength(heightPercent, true);
		RowDefinitions[2].Height = CalculateLength(heightPercent, false);
		ColumnDefinitions[0].Width = CalculateLength(widthPercent, true);
		ColumnDefinitions[2].Width = CalculateLength(widthPercent, false);
	}

	/// <summary>
	/// First content column star share (0–1), or 0.5 if not star-based.
	/// </summary>
	public double GetFirstColumnShare()
	{
		return GetFirstShare(
			ColumnDefinitions.Count > 0 ? ColumnDefinitions[0].Width : GridLength.Star,
			ColumnDefinitions.Count > 2 ? ColumnDefinitions[2].Width : GridLength.Star);
	}

	/// <summary>
	/// First content row star share (0–1), or 0.5 if not star-based.
	/// </summary>
	public double GetFirstRowShare()
	{
		return GetFirstShare(
			RowDefinitions.Count > 0 ? RowDefinitions[0].Height : GridLength.Star,
			RowDefinitions.Count > 2 ? RowDefinitions[2].Height : GridLength.Star);
	}

	private static double GetFirstShare(GridLength first, GridLength second)
	{
		if (first.IsStar && second.IsStar)
		{
			var total = first.Value + second.Value;
			return total > 0 ? first.Value / total : LayoutGridSettings.DefaultFirstShare;
		}

		return LayoutGridSettings.DefaultFirstShare;
	}

	private void InitializeDefaults()
	{
		if (ColumnDefinitions.Count == 0)
		{
			ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) });
			ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
		}

		if (RowDefinitions.Count == 0)
		{
			RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Pixel) });
			RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
		}
	}

	private void RefreshLayout()
	{
		var rowSpan = RowDefinitions.Count;
		var colSpan = ColumnDefinitions.Count;

		if (IsHorizontal)
		{
			// Enforce minimum widths on content columns
			if (ColumnDefinitions.Count > 0)
			{
				ColumnDefinitions[0].MinWidth = MinHorizontalSize;
			}
			if (ColumnDefinitions.Count > 2)
			{
				ColumnDefinitions[2].MinWidth = MinHorizontalSize;
			}

			// Clear row constraints to prevent interference
			foreach (var rowDef in RowDefinitions)
			{
				rowDef.MinHeight = 0;
			}

			for (var i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				child.SetValue(RowProperty, 0);
				child.SetValue(RowSpanProperty, rowSpan);
				child.SetValue(ColumnProperty, i);
				child.SetValue(ColumnSpanProperty, (i == 0) && !SplitGrid ? colSpan : 1);
				child.IsVisible = (i == 0) || SplitGrid;
			}
		}
		else
		{
			// Enforce minimum heights on content rows
			if (RowDefinitions.Count > 0)
			{
				RowDefinitions[0].MinHeight = MinVerticalSize;
			}
			if (RowDefinitions.Count > 2)
			{
				RowDefinitions[2].MinHeight = MinVerticalSize;
			}

			// Clear column constraints to prevent interference
			foreach (var colDef in ColumnDefinitions)
			{
				colDef.MinWidth = 0;
			}

			for (var i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				child.SetValue(RowProperty, i);
				child.SetValue(RowSpanProperty, (i == 0) && !SplitGrid ? rowSpan : 1);
				child.SetValue(ColumnProperty, 0);
				child.SetValue(ColumnSpanProperty, colSpan);
				child.IsVisible = (i == 0) || SplitGrid;
			}
		}

		InvalidateArrange();
	}

	#endregion
}