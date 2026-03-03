#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Input;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid;

/// <summary>
/// A data source for a <see cref="TreeDataGrid" /> which displays a flat grid.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
public class FlatTreeDataGridSource<TModel> : Notifiable,
	ITreeDataGridSource<TModel>,
	IDisposable
	where TModel : class
{
	#region Fields

	private IComparer<TModel> _comparer;
	private bool _isSelectionSet;
	private IEnumerable<TModel> _items;
	private TreeDataGridItemsSourceView<TModel> _itemsView;
	private AnonymousSortableRows<TModel> _rows;
	private ITreeDataGridSelection _selection;

	#endregion

	#region Constructors

	public FlatTreeDataGridSource() : this([])
	{
	}

	public FlatTreeDataGridSource(IEnumerable<TModel> items)
	{
		_items = items;
		_itemsView = TreeDataGridItemsSourceView<TModel>.GetOrCreate(items);

		Columns = [];
	}

	#endregion

	#region Properties

	public ITreeDataGridCellSelectionModel<TModel> CellSelection => Selection as ITreeDataGridCellSelectionModel<TModel>;

	public ColumnList<TModel> Columns { get; }

	public bool IsHierarchical => false;

	public bool IsSorted => _comparer is not null;

	public IEnumerable<TModel> Items
	{
		get => _items;
		set
		{
			if (_items != value)
			{
				_items = value;
				_itemsView = TreeDataGridItemsSourceView<TModel>.GetOrCreate(value);
				_rows?.SetItems(_itemsView);
				if (_selection is not null)
				{
					_selection.Source = value;
				}
				OnPropertyChanged();
			}
		}
	}

	public IRows Rows => _rows ??= CreateRows();

	public ITreeDataGridRowSelectionModel<TModel> RowSelection => Selection as ITreeDataGridRowSelectionModel<TModel>;

	public ITreeDataGridSelection Selection
	{
		get
		{
			if ((_selection == null) && !_isSelectionSet)
			{
				_selection = new TreeDataGridRowSelectionModel<TModel>(this);
			}
			return _selection;
		}
		set
		{
			if (_selection != value)
			{
				if (value?.Source != _items)
				{
					throw new InvalidOperationException("Selection source must be set to Items.");
				}
				_selection = value;
				_isSelectionSet = true;
				OnPropertyChanged();
			}
		}
	}

	IColumns ITreeDataGridSource.Columns => Columns;

	IEnumerable<object> ITreeDataGridSource.Items => Items;

	#endregion

	#region Methods

	public void Dispose()
	{
		_rows?.Dispose();
		GC.SuppressFinalize(this);
	}

	private AnonymousSortableRows<TModel> CreateRows()
	{
		return new AnonymousSortableRows<TModel>(_itemsView, _comparer);
	}

	void ITreeDataGridSource.DragDropRows(
		ITreeDataGridSource source,
		IEnumerable<IndexPath> indexes,
		IndexPath targetIndex,
		TreeDataGridRowDropPosition position,
		DragDropEffects effects)
	{
		if (effects != DragDropEffects.Move)
		{
			throw new NotSupportedException("Only move is currently supported for drag/drop.");
		}
		if (IsSorted)
		{
			throw new NotSupportedException("Drag/drop is not supported on sorted data.");
		}
		if (position == TreeDataGridRowDropPosition.Inside)
		{
			throw new ArgumentException("Invalid drop position.", nameof(position));
		}
		if (indexes.Any(x => x.Count != 1))
		{
			throw new ArgumentException("Invalid source index.", nameof(indexes));
		}
		if (targetIndex.Count != 1)
		{
			throw new ArgumentException("Invalid target index.", nameof(targetIndex));
		}
		if (_items is not IList<TModel> items)
		{
			throw new InvalidOperationException("Items does not implement IList<T>.");
		}

		if (position == TreeDataGridRowDropPosition.None)
		{
			return;
		}

		var ti = targetIndex[0];

		if (position == TreeDataGridRowDropPosition.After)
		{
			++ti;
		}

		var sourceItems = new List<TModel>();

		foreach (var src in indexes.OrderByDescending(x => x))
		{
			var i = src[0];
			sourceItems.Add(items[i]);
			items.RemoveAt(i);

			if (i < ti)
			{
				--ti;
			}
		}

		for (var si = sourceItems.Count - 1; si >= 0; --si)
		{
			items.Insert(ti++, sourceItems[si]);
		}
	}

	IEnumerable<object> ITreeDataGridSource.GetModelChildren(object model)
	{
		return [];
	}

	bool ITreeDataGridSource.SortBy(IColumn column, ListSortDirection direction)
	{
		if (column is IColumn<TModel> typedColumn)
		{
			if (!Columns.Contains(typedColumn))
			{
				return true;
			}

			var comparer = typedColumn.GetComparison(direction);

			if (comparer is not null)
			{
				_comparer = comparer is not null ? new FuncComparer<TModel>(comparer) : null;
				_rows?.Sort(_comparer);
				Sorted?.Invoke();
				foreach (var c in Columns)
				{
					c.SortDirection = c == column ? direction : null;
				}
			}
			return true;
		}

		return false;
	}

	#endregion

	#region Events

	public event Action Sorted;

	#endregion
}