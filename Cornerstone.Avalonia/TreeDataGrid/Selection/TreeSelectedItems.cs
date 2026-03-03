#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

internal class TreeSelectedItemsBase<T> : IReadOnlyList<T>
{
	#region Fields

	protected readonly TreeSelectionModelBase<T> _owner;

	#endregion

	#region Constructors

	public TreeSelectedItemsBase(TreeSelectionModelBase<T> owner)
	{
		_owner = owner;
	}

	#endregion

	#region Properties

	public int Count => _owner.Count;

	public T this[int index]
	{
		get
		{
			if ((index < 0) || (index >= Count))
			{
				throw new IndexOutOfRangeException("The index was out of range.");
			}

			if (_owner.SingleSelect)
			{
				return _owner.SelectedItem;
			}
			var next = 0;
			TryGetElementAt(_owner.Root, index, ref next, out var result);
			return result;
		}
	}

	#endregion

	#region Methods

	public IEnumerator<T> GetEnumerator()
	{
		if (_owner.SingleSelect)
		{
			if (_owner.SelectedIndex.Count > 0)
			{
				yield return _owner.SelectedItem;
			}
		}
		else
		{
			foreach (var i in EnumerateNode(_owner.Root))
			{
				yield return i;
			}
		}
	}

	private IEnumerable<T> EnumerateNode(TreeSelectionNode<T> node)
	{
		foreach (var range in node.Ranges)
		{
			for (var i = range.Begin; i <= range.End; ++i)
			{
				if (node.ItemsView is not null)
				{
					yield return node.ItemsView[i];
				}
			}
		}

		if (node.Children is not null)
		{
			foreach (var child in node.Children)
			{
				if (child is not null)
				{
					foreach (var i in EnumerateNode(child))
					{
						yield return i;
					}
				}
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private bool TryGetElementAt(TreeSelectionNode<T> node, int target, ref int next, out T result)
	{
		var nodeCount = IndexRange.GetCount(node.Ranges);

		if (target < (next + nodeCount))
		{
			result = node.ItemsView![IndexRange.GetAt(node.Ranges, target - next)];
			return true;
		}

		next += nodeCount;

		if (node.Children is not null)
		{
			foreach (var child in node.Children)
			{
				if (child is not null && TryGetElementAt(child, target, ref next, out result))
				{
					return true;
				}
			}
		}

		result = default;
		return false;
	}

	#endregion
}

internal class TreeSelectedItems<T> : TreeSelectedItemsBase<T>, IReadOnlyList<object>
{
	#region Constructors

	public TreeSelectedItems(TreeSelectionModelBase<T> root) : base(root)
	{
	}

	#endregion

	#region Properties

	object IReadOnlyList<object>.this[int index] => this[index];

	#endregion

	#region Methods

	IEnumerator<object> IEnumerable<object>.GetEnumerator()
	{
		foreach (var i in this)
		{
			yield return i;
		}
	}

	#endregion
}