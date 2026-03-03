#region References

using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

internal class TreeSelectionChangedItems<T> : IReadOnlyList<T>
{
	#region Fields

	private readonly TreeSelectionModelBase<T> _owner;
	private readonly IndexRanges _ranges;

	#endregion

	#region Constructors

	public TreeSelectionChangedItems(TreeSelectionModelBase<T> owner, IndexRanges ranges)
	{
		_owner = owner;
		_ranges = ranges;
	}

	#endregion

	#region Properties

	public int Count => _ranges.Count;

	public T this[int index] => _owner.GetSelectedItemAt(_ranges[index]);

	#endregion

	#region Methods

	public static TreeSelectionChangedItems<T> Create(TreeSelectionModelBase<T> owner, IndexRanges ranges)
	{
		return ranges is not null ? new TreeSelectionChangedItems<T>(owner, ranges) : null;
	}

	public IEnumerator<T> GetEnumerator()
	{
		foreach (var index in _ranges)
		{
			yield return _owner.GetSelectedItemAt(index);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}