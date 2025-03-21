#region References

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Document;

/// <summary>
/// Represents a read-only disjoint union of binary ranges in a document.
/// </summary>
public class ReadOnlyBitRangeUnion : IReadOnlyBitRangeUnion
{
	#region Fields

	/// <summary>
	/// The empty union.
	/// </summary>
	public static readonly ReadOnlyBitRangeUnion Empty = new(new BitRangeUnion());

	private readonly BitRangeUnion _union;

	#endregion

	#region Constructors

	/// <summary>
	/// Wraps an existing disjoint binary range union into a <see cref="ReadOnlyBitRangeUnion" />.
	/// </summary>
	/// <param name="union"> The union to wrap. </param>
	public ReadOnlyBitRangeUnion(BitRangeUnion union)
	{
		_union = union;
		_union.CollectionChanged += UnionOnCollectionChanged;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int Count => _union.Count;

	/// <inheritdoc />
	public BitRange EnclosingRange => _union.EnclosingRange;

	/// <inheritdoc />
	public bool IsFragmented => _union.IsFragmented;

	#endregion

	#region Methods

	/// <inheritdoc />
	public bool Contains(BitLocation location)
	{
		return _union.Contains(location);
	}

	/// <inheritdoc />
	public BitRangeUnion.Enumerator GetEnumerator()
	{
		return _union.GetEnumerator();
	}

	/// <inheritdoc />
	public bool IntersectsWith(BitRange range)
	{
		return _union.IntersectsWith(range);
	}

	/// <inheritdoc />
	public bool IsSuperSetOf(BitRange range)
	{
		return _union.IsSuperSetOf(range);
	}

	IEnumerator<BitRange> IEnumerable<BitRange>.GetEnumerator()
	{
		return _union.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable) _union).GetEnumerator();
	}

	private void UnionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		CollectionChanged?.Invoke(this, e);
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	#endregion
}