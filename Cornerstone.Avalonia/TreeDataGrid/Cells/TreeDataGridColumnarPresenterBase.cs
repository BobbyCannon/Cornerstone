#region References

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Cells;

/// <summary>
/// Base class for presenters which display data in virtualized columns.
/// </summary>
/// <typeparam name="TItem"> The item type. </typeparam>
/// <remarks>
/// Implements common layout functionality between <see cref="TreeDataGridCellsPresenter" />
/// and <see cref="TreeDataGridColumnHeadersPresenter" />.
/// </remarks>
public abstract class TreeDataGridColumnarPresenterBase<TItem> : TreeDataGridPresenterBase<TItem>
{
	#region Properties

	protected IColumns Columns => Items as IColumns;

	#endregion

	#region Methods

	protected sealed override double CalculateSizeU(Size availableSize)
	{
		return Columns?.GetEstimatedWidth(availableSize.Width) ?? 0;
	}

	protected sealed override (int index, double position) GetElementAt(double position)
	{
		return ((IColumns) Items!).GetColumnAt(position);
	}

	protected sealed override Size GetFinalConstraint(Control element, int index, Size availableSize)
	{
		var column = Columns![index];
		return new(column.ActualWidth, double.PositiveInfinity);
	}

	protected sealed override Size GetInitialConstraint(Control element, int index, Size availableSize)
	{
		var column = (IUpdateColumnLayout) Columns![index];
		return new Size(Math.Min(availableSize.Width, column.MaxActualWidth), availableSize.Height);
	}

	protected override (int index, double position) GetOrEstimateAnchorElementForViewport(
		double viewportStart,
		double viewportEnd,
		int itemCount)
	{
		if (Columns?.GetColumnAt(viewportStart) is var (index, position) && (index >= 0))
		{
			return (index, position);
		}
		return base.GetOrEstimateAnchorElementForViewport(viewportStart, viewportEnd, itemCount);
	}

	protected sealed override bool NeedsFinalMeasurePass(int firstIndex, IReadOnlyList<Control> elements)
	{
		var columns = Columns!;

		columns.CommitActualWidths();

		// We need to do a second measure pass if any of the controls were measured with a width
		// that is greater than the final column width.
		for (var i = 0; i < elements.Count; i++)
		{
			var e = elements[i];
			if (e is not null)
			{
				var previous = LayoutInformation.GetPreviousMeasureConstraint(e)!.Value;
				if (previous.Width > columns[i + firstIndex].ActualWidth)
				{
					return true;
				}
			}
		}

		return false;
	}

	#endregion
}