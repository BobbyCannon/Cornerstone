#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

internal class IndexRanges : IReadOnlyList<IndexPath>
{
	#region Fields

	private readonly SortedList<IndexPath, List<IndexRange>> _ranges;

	#endregion

	#region Constructors

	public IndexRanges()
	{
		_ranges = new();
	}

	#endregion

	#region Properties

	public int Count { get; private set; }

	public IndexPath this[int index]
	{
		get
		{
			foreach (var r in _ranges!)
			{
				var parent = r.Key;
				var ranges = r.Value;
				var count = IndexRange.GetCount(ranges);

				if (index < count)
				{
					return parent.Append(IndexRange.GetAt(ranges, index));
				}

				index -= count;
			}

			throw new IndexOutOfRangeException();
		}
	}

	public IDictionary<IndexPath, List<IndexRange>> Ranges => _ranges;

	#endregion

	#region Methods

	public void Add(in IndexPath index)
	{
		var parent = index[..^1];

		if (!_ranges.TryGetValue(parent, out var ranges))
		{
			ranges = [];
			_ranges.Add(parent, ranges);
		}

		Count += IndexRange.Add(ranges, new IndexRange(index[^1]));
	}

	public void Add(in IndexPath parent, in IndexRange range)
	{
		if (!_ranges.TryGetValue(parent, out var ranges))
		{
			ranges = [];
			_ranges.Add(parent, ranges);
		}

		Count += IndexRange.Add(ranges, range);
	}

	public void Add(in IndexPath parent, List<IndexRange> ranges)
	{
		if (!_ranges.TryGetValue(parent, out var r))
		{
			_ranges.Add(parent, ranges);
			Count += IndexRange.GetCount(ranges);
		}
		else
		{
			Count += IndexRange.Add(ranges, r);
		}
	}

	public bool Contains(in IndexPath index)
	{
		var parent = index[..^1];

		if (_ranges.TryGetValue(parent, out var ranges))
		{
			return IndexRange.Contains(ranges, index[^1]);
		}

		return false;
	}

	public bool ContainsDescendents(in IndexPath index)
	{
		foreach (var i in _ranges.Keys)
		{
			if ((index == i) || index.IsAncestorOf(i))
			{
				return true;
			}
		}

		return false;
	}

	public IEnumerator<IndexPath> GetEnumerator()
	{
		if (_ranges is not null)
		{
			foreach (var r in _ranges)
			{
				var parent = r.Key;
				var ranges = r.Value;
				var count = IndexRange.GetCount(ranges);

				for (var i = 0; i < count; ++i)
				{
					yield return parent.Append(IndexRange.GetAt(ranges, i));
				}
			}
		}
	}

	public bool Remove(in IndexPath index)
	{
		var parent = index[..^1];

		if (_ranges.TryGetValue(parent, out var ranges))
		{
			if (IndexRange.Remove(ranges, new IndexRange(index[^1])) > 0)
			{
				--Count;
				return true;
			}
		}

		return false;
	}

	public bool Remove(in IndexPath parent, IndexRange range)
	{
		if (_ranges.TryGetValue(parent, out var existing))
		{
			var removed = IndexRange.Remove(existing, range);
			Count -= removed;
			return removed > 0;
		}

		return false;
	}

	public bool Remove(in IndexPath parent, IReadOnlyList<IndexRange> ranges)
	{
		if (_ranges.TryGetValue(parent, out var existing))
		{
			var removed = IndexRange.Remove(existing, ranges);
			Count -= removed;
			return removed > 0;
		}

		return false;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}