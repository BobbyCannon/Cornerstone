#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Document;

/// <summary>
/// Represents a disjoint union of binary ranges.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
public class BitRangeUnion : IReadOnlyBitRangeUnion, ICollection<BitRange>
{
	#region Fields

	private readonly ObservableCollection<BitRange> _ranges = new();

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new empty union.
	/// </summary>
	public BitRangeUnion()
	{
		_ranges.CollectionChanged += (sender, args) => CollectionChanged?.Invoke(this, args);
	}

	/// <summary>
	/// Initializes a new union of bit ranges.
	/// </summary>
	/// <param name="ranges"> The ranges to unify. </param>
	public BitRangeUnion(IEnumerable<BitRange> ranges)
		: this()
	{
		foreach (var range in ranges)
		{
			Add(range);
		}
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
	public int Count => _ranges.Count;

	/// <inheritdoc />
	public BitRange EnclosingRange => _ranges.Count == 0 ? BitRange.Empty : new(_ranges[0].Start, _ranges[^1].End);

	/// <inheritdoc />
	public bool IsFragmented => _ranges.Count > 1;

	/// <inheritdoc />
	public bool IsReadOnly => false;

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Add(BitRange item)
	{
		var (result, index) = FindFirstOverlappingRange(item);

		switch (result)
		{
			case SearchResult.PresentBeforeIndex:
				_ranges.Insert(index + 1, item);
				break;

			case SearchResult.PresentAfterIndex:
			case SearchResult.NotPresentAtIndex:
				_ranges.Insert(index, item);
				break;

			default:
				throw new ArgumentOutOfRangeException();
		}

		MergeRanges(index);
	}

	/// <summary>
	/// Wraps the union into a <see cref="ReadOnlyBitRangeUnion" />.
	/// </summary>
	/// <returns> The resulting read-only union. </returns>
	public ReadOnlyBitRangeUnion AsReadOnly()
	{
		return new(this);
	}

	/// <inheritdoc />
	public void Clear()
	{
		_ranges.Clear();
	}

	/// <inheritdoc />
	public bool Contains(BitRange item)
	{
		return _ranges.Contains(item);
	}

	/// <inheritdoc />
	public bool Contains(BitLocation location)
	{
		return IsSuperSetOf(new BitRange(location, location.NextOrMax()));
	}

	/// <inheritdoc />
	public void CopyTo(BitRange[] array, int arrayIndex)
	{
		_ranges.CopyTo(array, arrayIndex);
	}

	/// <inheritdoc />
	public Enumerator GetEnumerator()
	{
		return new(this);
	}

	/// <inheritdoc />
	public bool IntersectsWith(BitRange range)
	{
		var (result, index) = FindFirstOverlappingRange(range);
		if (result == SearchResult.NotPresentAtIndex)
		{
			return false;
		}

		return _ranges[index].OverlapsWith(range);
	}

	/// <inheritdoc />
	public bool IsSuperSetOf(BitRange range)
	{
		var (result, index) = FindFirstOverlappingRange(range);
		if (result == SearchResult.NotPresentAtIndex)
		{
			return false;
		}

		return _ranges[index].Contains(range);
	}

	/// <inheritdoc />
	public bool Remove(BitRange item)
	{
		var (result, index) = FindFirstOverlappingRange(item);

		if (result == SearchResult.NotPresentAtIndex)
		{
			return false;
		}

		for (var i = index; i < _ranges.Count; i++)
		{
			// Is this an overlapping range?
			if (!_ranges[i].OverlapsWith(item))
			{
				break;
			}

			if (_ranges[i].Contains(new BitRange(item.Start, item.End.NextOrMax())))
			{
				// The range contains the entire range-to-remove, split up the range.
				var (a, rest) = _ranges[i].Split(item.Start);
				var (b, c) = rest.Split(item.End);

				if (a.IsEmpty)
				{
					_ranges.RemoveAt(i--);
				}
				else
				{
					_ranges[i] = a;
				}

				if (!c.IsEmpty)
				{
					_ranges.Insert(i + 1, c);
				}
				break;
			}

			if (item.Contains(_ranges[i]))
			{
				// The range-to-remove contains the entire current range.
				_ranges.RemoveAt(i--);
			}
			else if (item.Start < _ranges[i].Start)
			{
				// We are truncating the current range from the left.
				_ranges[i] = _ranges[i].Clamp(new BitRange(item.End, BitLocation.Maximum));
			}
			else if (item.End >= _ranges[i].End)
			{
				// We are truncating the current range from the right.
				_ranges[i] = _ranges[i].Clamp(new BitRange(BitLocation.Minimum, item.Start));
			}
		}

		return true;
	}

	private (SearchResult Result, int Index) FindFirstOverlappingRange(BitRange range)
	{
		range = new BitRange(range.Start, range.End.NextOrMax());
		for (var i = 0; i < _ranges.Count; i++)
		{
			if (_ranges[i].ExtendTo(_ranges[i].End.NextOrMax()).OverlapsWith(range))
			{
				if (_ranges[i].Start >= range.Start)
				{
					return (SearchResult.PresentAfterIndex, i);
				}
				return (SearchResult.PresentBeforeIndex, i);
			}

			if (_ranges[i].Start > range.End)
			{
				return (SearchResult.NotPresentAtIndex, i);
			}
		}

		return (SearchResult.NotPresentAtIndex, _ranges.Count);
	}

	IEnumerator<BitRange> IEnumerable<BitRange>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void MergeRanges(int startIndex)
	{
		for (var i = startIndex; i < (_ranges.Count - 1); i++)
		{
			if (!_ranges[i].ExtendTo(_ranges[i].End.Next()).OverlapsWith(_ranges[i + 1]))
			{
				return;
			}

			_ranges[i] = _ranges[i]
				.ExtendTo(_ranges[i + 1].Start)
				.ExtendTo(_ranges[i + 1].End);

			_ranges.RemoveAt(i + 1);
			i--;
		}
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	#endregion

	#region Structures

	/// <summary>
	/// An implementation of an enumerator that enumerates all disjoint ranges within a bit range union.
	/// </summary>
	public struct Enumerator : IEnumerator<BitRange>
	{
		#region Fields

		private int _index;
		private readonly BitRangeUnion _union;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new disjoint bit range union enumerator.
		/// </summary>
		/// <param name="union"> The disjoint union to enumerate. </param>
		public Enumerator(BitRangeUnion union) : this()
		{
			_union = union;
			_index = -1;
		}

		#endregion

		#region Properties

		/// <inheritdoc />
		public BitRange Current =>
			_index < _union._ranges.Count
				? _union._ranges[_index]
				: default;

		/// <inheritdoc />
		object IEnumerator.Current => Current;

		#endregion

		#region Methods

		/// <inheritdoc />
		public void Dispose()
		{
		}

		/// <inheritdoc />
		public bool MoveNext()
		{
			_index++;
			return _index < _union._ranges.Count;
		}

		/// <inheritdoc />
		void IEnumerator.Reset()
		{
		}

		#endregion
	}

	#endregion

	#region Enumerations

	private enum SearchResult
	{
		PresentBeforeIndex,
		PresentAfterIndex,
		NotPresentAtIndex
	}

	#endregion
}