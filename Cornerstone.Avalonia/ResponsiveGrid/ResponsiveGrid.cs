#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.ResponsiveGrid;

public partial class ResponsiveGrid : Grid
{
	#region Constructors

	public ResponsiveGrid()
	{
		MaxDivision = 24;
		Thresholds = new SizeThresholds();
	}

	#endregion

	#region Methods

	protected override Size ArrangeOverride(Size finalSize)
	{
		var visible = AssignCells(finalSize.Width);
		GetTrackMetrics(finalSize.Width, out var trackWidth, out var columnSpacing);
		var group = visible
			.GroupBy(GetActualRow)
			.OrderBy(o => o.Key)
			.ToList();

		double y = 0;
		for (var i = 0; i < group.Count; i++)
		{
			var rows = group[i];
			var rowHeight = rows.Max(o => o.DesiredSize.Height);

			foreach (var element in rows)
			{
				var column = GetActualColumn(element);
				var columnSpan = GetSpan(element, finalSize.Width);
				var rect = new Rect(
					GetCellX(column, trackWidth, columnSpacing),
					y,
					GetCellWidth(columnSpan, trackWidth, columnSpacing),
					rowHeight);
				element.Arrange(rect);
			}

			y += rowHeight;
			if (i < (group.Count - 1))
			{
				y += RowSpacing;
			}
		}

		return finalSize;
	}

	/// <summary>
	/// Width of a cell spanning the given number of tracks, including gutters between those tracks.
	/// </summary>
	protected double GetCellWidth(int span, double trackWidth, double columnSpacing)
	{
		if (span <= 0)
		{
			return 0;
		}

		return (span * trackWidth) + ((span - 1) * columnSpacing);
	}

	/// <summary>
	/// X origin of a cell starting at the given column track index.
	/// </summary>
	protected double GetCellX(int column, double trackWidth, double columnSpacing)
	{
		if (column <= 0)
		{
			return 0;
		}

		return column * (trackWidth + columnSpacing);
	}

	/// <summary>
	/// Content width of one division track after reserving gutters between tracks.
	/// Gutters shrink when (MaxDivision - 1) times ColumnSpacing exceeds totalWidth
	/// so stretch children cannot overflow the grid.
	/// </summary>
	protected void GetTrackMetrics(double totalWidth, out double trackWidth, out double columnSpacing)
	{
		columnSpacing = ColumnSpacing;

		if (MaxDivision <= 0)
		{
			trackWidth = 0;
			return;
		}

		if (double.IsPositiveInfinity(totalWidth))
		{
			trackWidth = double.PositiveInfinity;
			return;
		}

		var gutterCount = Math.Max(0, MaxDivision - 1);
		var gutterTotal = gutterCount * columnSpacing;
		if (gutterTotal > totalWidth)
		{
			columnSpacing = gutterCount > 0 ? totalWidth / gutterCount : 0;
			trackWidth = 0;
			return;
		}

		trackWidth = (totalWidth - gutterTotal) / MaxDivision;
	}

	protected int GetOffset(Control element, double width)
	{
		int span;

		var getXS = new Func<Control, int>(o =>
		{
			var x = GetXS_Offset(o);
			return x != 0 ? x : 0;
		});
		var getSM = new Func<Control, int>(o =>
		{
			var x = GetSM_Offset(o);
			return x != 0 ? x : getXS(o);
		});
		var getMD = new Func<Control, int>(o =>
		{
			var x = GetMD_Offset(o);
			return x != 0 ? x : getSM(o);
		});
		var getLG = new Func<Control, int>(o =>
		{
			var x = GetLG_Offset(o);
			return x != 0 ? x : getMD(o);
		});

		if (width < Thresholds.XSmallToSmall)
		{
			span = getXS(element);
		}
		else if (width < Thresholds.SmallToMedium)
		{
			span = getSM(element);
		}
		else if (width < Thresholds.MediumToLarge)
		{
			span = getMD(element);
		}
		else
		{
			span = getLG(element);
		}

		return Math.Min(Math.Max(0, span), MaxDivision);
	}

	protected int GetPull(Control element, double width)
	{
		int span;

		var getXS = new Func<Control, int>(o =>
		{
			var x = GetXS_Pull(o);
			return x != 0 ? x : 0;
		});
		var getSM = new Func<Control, int>(o =>
		{
			var x = GetSM_Pull(o);
			return x != 0 ? x : getXS(o);
		});
		var getMD = new Func<Control, int>(o =>
		{
			var x = GetMD_Pull(o);
			return x != 0 ? x : getSM(o);
		});
		var getLG = new Func<Control, int>(o =>
		{
			var x = GetLG_Pull(o);
			return x != 0 ? x : getMD(o);
		});

		if (width < Thresholds.XSmallToSmall)
		{
			span = getXS(element);
		}
		else if (width < Thresholds.SmallToMedium)
		{
			span = getSM(element);
		}
		else if (width < Thresholds.MediumToLarge)
		{
			span = getMD(element);
		}
		else
		{
			span = getLG(element);
		}

		return Math.Min(Math.Max(0, span), MaxDivision);
	}

	protected int GetPush(Control element, double width)
	{
		int span;

		var getXS = new Func<Control, int>(o =>
		{
			var x = GetXS_Push(o);
			return x != 0 ? x : 0;
		});
		var getSM = new Func<Control, int>(o =>
		{
			var x = GetSM_Push(o);
			return x != 0 ? x : getXS(o);
		});
		var getMD = new Func<Control, int>(o =>
		{
			var x = GetMD_Push(o);
			return x != 0 ? x : getSM(o);
		});
		var getLG = new Func<Control, int>(o =>
		{
			var x = GetLG_Push(o);
			return x != 0 ? x : getMD(o);
		});

		if (width < Thresholds.XSmallToSmall)
		{
			span = getXS(element);
		}
		else if (width < Thresholds.SmallToMedium)
		{
			span = getSM(element);
		}
		else if (width < Thresholds.MediumToLarge)
		{
			span = getMD(element);
		}
		else
		{
			span = getLG(element);
		}

		return Math.Min(Math.Max(0, span), MaxDivision);
	}

	protected int GetSpan(Control element, double width)
	{
		int span;

		var getXS = new Func<Control, int>(o =>
		{
			var x = GetXS(o);
			return x != 0 ? x : MaxDivision;
		});
		var getSM = new Func<Control, int>(o =>
		{
			var x = GetSM(o);
			return x != 0 ? x : getXS(o);
		});
		var getMD = new Func<Control, int>(o =>
		{
			var x = GetMD(o);
			return x != 0 ? x : getSM(o);
		});
		var getLG = new Func<Control, int>(o =>
		{
			var x = GetLG(o);
			return x != 0 ? x : getMD(o);
		});

		if (width < Thresholds.XSmallToSmall)
		{
			span = getXS(element);
		}
		else if (width < Thresholds.SmallToMedium)
		{
			span = getSM(element);
		}
		else if (width < Thresholds.MediumToLarge)
		{
			span = getMD(element);
		}
		else
		{
			span = getLG(element);
		}

		return Math.Min(Math.Max(0, span), MaxDivision);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		GetTrackMetrics(availableSize.Width, out var trackWidth, out var columnSpacing);
		var remainingHeight = availableSize.Height;
		var rowHeights = new List<double>();
		var visible = AssignCells(availableSize.Width);

		// Determine number of rows
		var rowCount = visible.Select(GetActualRow).DefaultIfEmpty(-1).Max() + 1;

		// Reserve vertical gutters between rows when height is constrained
		if (!double.IsPositiveInfinity(remainingHeight) && (rowCount > 1))
		{
			remainingHeight = Math.Max(0, remainingHeight - ((rowCount - 1) * RowSpacing));
		}

		// Calculate initial height per row (for all but the last row)
		var heightPerRow = rowCount > 0 ? remainingHeight / rowCount : remainingHeight;

		// Second pass: Measure children with constrained height
		var group = visible
			.GroupBy(GetActualRow)
			.OrderBy(o => o.Key)
			.ToList();
		for (var i = 0; i < group.Count; i++)
		{
			var row = group[i];
			double maxRowHeight = 0;

			// Use remainingHeight for last row
			var rowHeight = i == (group.Count - 1)
				? remainingHeight
				: heightPerRow;

			foreach (var child in row)
			{
				var span = GetSpan(child, availableSize.Width);
				var size = new Size(GetCellWidth(span, trackWidth, columnSpacing), rowHeight);
				child.Measure(size);
				maxRowHeight = Math.Max(maxRowHeight, child.DesiredSize.Height);
			}

			rowHeights.Add(maxRowHeight);
			remainingHeight -= maxRowHeight;
			remainingHeight = Math.Max(0, remainingHeight);
		}

		double totalWidth;
		if (double.IsPositiveInfinity(availableSize.Width))
		{
			totalWidth = group.Any()
				? group.Max(rows =>
				{
					var ordered = rows.OrderBy(GetActualColumn).ToList();
					double width = 0;
					for (var i = 0; i < ordered.Count; i++)
					{
						width += ordered[i].DesiredSize.Width;
						if (i < (ordered.Count - 1))
						{
							width += ColumnSpacing;
						}
					}
					return width;
				})
				: 0;
		}
		else
		{
			totalWidth = availableSize.Width;
		}

		var totalHeight = rowHeights.Sum();
		if (rowHeights.Count > 1)
		{
			totalHeight += (rowHeights.Count - 1) * RowSpacing;
		}

		return new Size(totalWidth, totalHeight);
	}

	private List<Control> AssignCells(double width)
	{
		var visible = GetVisibleChildren();
		var count = 0;
		var currentRow = 0;

		foreach (var child in visible)
		{
			var span = GetSpan(child, width);
			var offset = GetOffset(child, width);
			var push = GetPush(child, width);
			var pull = GetPull(child, width);

			if ((count + span + offset) > MaxDivision)
			{
				currentRow++;
				count = 0;
			}

			var column = (count + offset + push) - pull;
			var maxColumn = Math.Max(0, MaxDivision - span);
			column = Math.Min(Math.Max(0, column), maxColumn);
			SetActualColumn(child, column);
			SetActualRow(child, currentRow);

			count += span + offset;
		}

		return visible;
	}

	private List<Control> GetVisibleChildren()
	{
		return Children.Where(c => c.IsVisible).ToList();
	}

	#endregion
}