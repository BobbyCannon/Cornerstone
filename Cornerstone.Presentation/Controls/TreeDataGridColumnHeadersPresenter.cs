#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Controls.TreeDataGrid.Models;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls;

public class TreeDataGridColumnHeadersPresenter : TreeDataGridColumnarPresenterBase<IColumn>, IChildIndexProvider
{
	#region Properties

	protected override Orientation Orientation => Orientation.Horizontal;

	#endregion

	#region Methods

	public int GetChildIndex(ILogical child)
	{
		if (child is TreeDataGridColumnHeader header)
		{
			return header.ColumnIndex;
		}
		return -1;
	}

	public bool TryGetTotalCount(out int count)
	{
		if (Items is null)
		{
			count = 0;
			return false;
		}

		count = Items.Count;
		return true;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		(Items as IColumns)?.CommitActualWidths();
		return base.ArrangeOverride(finalSize);
	}

	protected override Size MeasureElement(int index, Control element, Size availableSize)
	{
		var columns = (IColumns) Items!;
		element.Measure(availableSize);
		return columns.CellMeasured(index, -1, element.DesiredSize);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if (change.Property == ItemsProperty)
		{
			var oldValue = change.GetOldValue<IReadOnlyList<IColumn>>();
			var newValue = change.GetNewValue<IReadOnlyList<IColumn>>();

			if (oldValue is IColumns oldColumns)
			{
				oldColumns.LayoutInvalidated -= OnColumnLayoutInvalidated;
			}
			if (newValue is IColumns newColumns)
			{
				newColumns.LayoutInvalidated += OnColumnLayoutInvalidated;
			}
		}

		base.OnPropertyChanged(change);
	}

	protected override void RealizeElement(Control element, IColumn column, int index)
	{
		if (TemplatedParent is global::Cornerstone.Presentation.Controls.TreeDataGrid.TreeDataGrid grid)
		{
			global::Cornerstone.Presentation.Controls.TreeDataGrid.TreeDataGrid.ApplyRowHeight(element, grid.MinRowHeight, grid.UseFixedRowHeight);
		}
		((TreeDataGridColumnHeader) element).Realize((IColumns) Items!, index);
		ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, index));
	}

	protected override void UnrealizeElement(Control element)
	{
		((TreeDataGridColumnHeader) element).Unrealize();
		ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, ((TreeDataGridColumnHeader) element).ColumnIndex));
	}

	protected override void UpdateElementIndex(Control element, int oldIndex, int newIndex)
	{
		((TreeDataGridColumnHeader) element).UpdateColumnIndex(newIndex);
		ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, newIndex));
	}

	private void OnColumnLayoutInvalidated(object sender, EventArgs e)
	{
		InvalidateMeasure();
	}

	#endregion

	#region Events

	public event EventHandler<ChildIndexChangedEventArgs> ChildIndexChanged;

	#endregion
}