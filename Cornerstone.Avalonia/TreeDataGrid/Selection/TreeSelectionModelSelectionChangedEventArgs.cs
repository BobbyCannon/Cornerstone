#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

public abstract class TreeSelectionModelSelectionChangedEventArgs : EventArgs
{
	#region Properties

	/// <summary>
	/// Gets the indexes of the items that were removed from the selection.
	/// </summary>
	public abstract IReadOnlyList<IndexPath> DeselectedIndexes { get; }

	/// <summary>
	/// Gets the items that were removed from the selection.
	/// </summary>
	public IReadOnlyList<object> DeselectedItems => GetUntypedDeselectedItems();

	/// <summary>
	/// Gets the indexes of the items that were added to the selection.
	/// </summary>
	public abstract IReadOnlyList<IndexPath> SelectedIndexes { get; }

	/// <summary>
	/// Gets the items that were added to the selection.
	/// </summary>
	public IReadOnlyList<object> SelectedItems => GetUntypedSelectedItems();

	#endregion

	#region Methods

	protected abstract IReadOnlyList<object> GetUntypedDeselectedItems();
	protected abstract IReadOnlyList<object> GetUntypedSelectedItems();

	#endregion
}

public class TreeSelectionModelSelectionChangedEventArgs<T> : TreeSelectionModelSelectionChangedEventArgs
{
	#region Fields

	private IReadOnlyList<object> _deselectedItems;
	private IReadOnlyList<object> _selectedItems;

	#endregion

	#region Constructors

	public TreeSelectionModelSelectionChangedEventArgs(
		IReadOnlyList<IndexPath> deselectedIndexes = null,
		IReadOnlyList<IndexPath> selectedIndexes = null,
		IReadOnlyList<T> deselectedItems = null,
		IReadOnlyList<T> selectedItems = null)
	{
		DeselectedIndexes = deselectedIndexes ?? [];
		SelectedIndexes = selectedIndexes ?? [];
		DeselectedItems = deselectedItems ?? [];
		SelectedItems = selectedItems ?? [];
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the indexes of the items that were removed from the selection.
	/// </summary>
	public override IReadOnlyList<IndexPath> DeselectedIndexes { get; }

	/// <summary>
	/// Gets the items that were removed from the selection.
	/// </summary>
	public new IReadOnlyList<T> DeselectedItems { get; }

	/// <summary>
	/// Gets the indexes of the items that were added to the selection.
	/// </summary>
	public override IReadOnlyList<IndexPath> SelectedIndexes { get; }

	/// <summary>
	/// Gets the items that were added to the selection.
	/// </summary>
	public new IReadOnlyList<T> SelectedItems { get; }

	#endregion

	#region Methods

	protected override IReadOnlyList<object> GetUntypedDeselectedItems()
	{
		return _deselectedItems ??= DeselectedItems as IReadOnlyList<object> ??
			new Untyped(DeselectedItems);
	}

	protected override IReadOnlyList<object> GetUntypedSelectedItems()
	{
		return _selectedItems ??= SelectedItems as IReadOnlyList<object> ??
			new Untyped(SelectedItems);
	}

	#endregion

	#region Classes

	private class Untyped : IReadOnlyList<object>
	{
		#region Fields

		private readonly IReadOnlyList<T> _source;

		#endregion

		#region Constructors

		public Untyped(IReadOnlyList<T> source)
		{
			_source = source;
		}

		#endregion

		#region Properties

		public int Count => _source.Count;

		public object this[int index] => _source[index];

		#endregion

		#region Methods

		public IEnumerator<object> GetEnumerator()
		{
			foreach (var i in _source)
			{
				yield return i;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	#endregion
}