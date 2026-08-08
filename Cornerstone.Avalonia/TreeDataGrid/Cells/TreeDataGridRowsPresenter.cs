#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Cells;

public class TreeDataGridRowsPresenter : TreeDataGridPresenterBase<IRow>, IChildIndexProvider
{
	#region Fields

	public static readonly DirectProperty<TreeDataGridRowsPresenter, IColumns> ColumnsProperty =
		AvaloniaProperty.RegisterDirect<TreeDataGridRowsPresenter, IColumns>(
			nameof(Columns),
			o => o.Columns,
			(o, v) => o.Columns = v);

	private IColumns _columns;

	#endregion

	#region Properties

	public IColumns Columns
	{
		get => _columns;
		set => SetAndRaise(ColumnsProperty, ref _columns, value);
	}

	protected override Orientation Orientation => Orientation.Vertical;

	/// <summary>
	/// When the host uses fixed row height, primary-axis size is MinRowHeight (stable scroll extent).
	/// When UseFixedRowHeight is false, returns null so heights are estimated from realized rows.
	/// </summary>
	protected override double? FixedElementSizeU
	{
		get
		{
			if (TemplatedParent is not TreeDataGrid { UseFixedRowHeight: true } grid)
			{
				return null;
			}

			var height = grid.MinRowHeight;
			return height > 0 ? height : null;
		}
	}

	#endregion

	#region Methods

	public int GetChildIndex(ILogical child)
	{
		if (child is TreeDataGridRow row)
		{
			return row.RowIndex;
		}
		return -1;
	}

	public bool TryGetTotalCount(out int count)
	{
		if (Items != null)
		{
			count = Items.Count;
			return true;
		}
		count = 0;
		return false;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Columns?.CommitActualWidths();
		return base.ArrangeOverride(finalSize);
	}

	protected override (int index, double position) GetElementAt(double position)
	{
		// Prefer exact fixed-height geometry (GetRowAt is still a stub for Auto-height rows).
		if ((Items is { Count: > 0 }) && (FixedElementSizeU is { } rowHeight) && (rowHeight > 0))
		{
			if (position <= 0)
			{
				return (0, 0);
			}

			var index = Math.Min((int) (position / rowHeight), Items.Count - 1);
			if (index < 0)
			{
				index = 0;
			}

			return (index, index * rowHeight);
		}

		return ((IRows) Items!).GetRowAt(position);
	}

	protected override Size MeasureElement(int index, Control element, Size availableSize)
	{
		var size = base.MeasureElement(index, element, availableSize);
		// Force primary-axis size into SizeU used for extent/arrange (content may still clip).
		if (FixedElementSizeU is { } rowHeight)
		{
			return new Size(size.Width, rowHeight);
		}

		return size;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var result = base.MeasureOverride(availableSize);

		// If we have no rows, then get the width from the columns.
		if (Columns is not null && (Items is null || (Items.Count == 0)))
		{
			result = result.WithWidth(Columns.GetEstimatedWidth(availableSize.Width));
		}

		return result;
	}

	protected override void OnEffectiveViewportChanged(object sender, EffectiveViewportChangedEventArgs e)
	{
		base.OnEffectiveViewportChanged(sender, e);
		Columns?.ViewportChanged(Viewport);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ColumnsProperty)
		{
			var oldValue = change.GetOldValue<IColumns>();
			var newValue = change.GetNewValue<IColumns>();

			if (oldValue is not null)
			{
				oldValue.LayoutInvalidated -= OnColumnLayoutInvalidated;
			}
			if (newValue is not null)
			{
				newValue.LayoutInvalidated += OnColumnLayoutInvalidated;
			}

			// When for existing Presenter Columns would be recreated they won't get Viewport set so we need to track that
			// and pass Viewport for a newly created object. 
			if ((oldValue != null) && (newValue != null))
			{
				newValue.ViewportChanged(Viewport);
			}
		}

		base.OnPropertyChanged(change);
	}

	protected override void RealizeElement(Control element, IRow rowModel, int index)
	{
		var row = (TreeDataGridRow) element;
		// Prefer owner MinRowHeight over fragile ancestor bindings on the row theme.
		if (TemplatedParent is TreeDataGrid grid)
		{
			TreeDataGrid.ApplyRowHeight(row, grid.MinRowHeight, grid.UseFixedRowHeight);
		}
		row.Realize(ElementFactory, GetSelection(), Columns, (IRows) Items, index);
		ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, index));
	}

	protected override void UnrealizeElement(Control element)
	{
		((TreeDataGridRow) element).Unrealize();
		ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, ((TreeDataGridRow) element).RowIndex));
	}

	protected override void UnrealizeElementOnItemRemoved(Control element)
	{
		((TreeDataGridRow) element).UnrealizeOnItemRemoved();
		ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, ((TreeDataGridRow) element).RowIndex));
	}

	protected override void UpdateElementIndex(Control element, int oldIndex, int newIndex)
	{
		((TreeDataGridRow) element).UpdateIndex(newIndex);
		ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, newIndex));
	}

	internal void UpdateSelection(ITreeDataGridSelectionInteraction selection)
	{
		foreach (var element in RealizedElements)
		{
			if (element is TreeDataGridRow { RowIndex: >= 0 } row)
			{
				row.UpdateSelection(selection);
			}
		}
	}

	private ITreeDataGridSelectionInteraction GetSelection()
	{
		return this.FindAncestorOfType<TreeDataGrid>()?.SelectionInteraction;
	}

	private void OnColumnLayoutInvalidated(object sender, EventArgs e)
	{
		InvalidateMeasure();

		foreach (var element in RealizedElements)
		{
			if (element is TreeDataGridRow row)
			{
				row.CellsPresenter?.InvalidateMeasure();
			}
		}
	}

	#endregion

	#region Events

	public event EventHandler<ChildIndexChangedEventArgs> ChildIndexChanged;

	#endregion
}