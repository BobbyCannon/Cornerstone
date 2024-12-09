#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Collections;

/// <inheritdoc cref="ReadOnlySet{T}" />
public class ReadOnlySet<T> :
	#if (!NETSTANDARD2_0)
	IReadOnlySet<T>,
	#endif
	ISet<T>
{
	#region Fields

	private readonly ISet<T> _set;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an empty readonly set.
	/// </summary>
	public ReadOnlySet() : this(new HashSet<T>())
	{
	}

	/// <summary>
	/// Initializes a readonly set with the provided values.
	/// </summary>
	/// <param name="values"> The values to include in a read only set. </param>
	/// <param name="comparer"> An optional comparer to compare set values. </param>
	public ReadOnlySet(IEnumerable<T> values, IEqualityComparer<T> comparer = null)
		: this(new HashSet<T>(values, comparer))
	{
	}

	/// <summary>
	/// Initializes a readonly set with the provided values.
	/// </summary>
	/// <param name="values"> The values to include in a read only set. </param>
	public ReadOnlySet(params T[] values)
		: this(new HashSet<T>(values))
	{
	}

	/// <summary>
	/// Initializes a readonly set with the provided values.
	/// </summary>
	/// <param name="comparer"> An optional comparer to compare set values. </param>
	/// <param name="values"> The values to include in a read only set. </param>
	public ReadOnlySet(IEqualityComparer<T> comparer, params T[] values)
		: this(new HashSet<T>(values, comparer))
	{
	}

	/// <summary>
	/// Initializes a readonly set with the provided values.
	/// </summary>
	/// <param name="values"> The values to include in a read only set. </param>
	/// <param name="additionalValues"> Any additional values to include in a read only set. </param>
	public ReadOnlySet(T[] values, params T[][] additionalValues)
		: this(new HashSet<T>(values.Combine(additionalValues).Distinct()))
	{
	}

	/// <summary>
	/// Initializes a readonly version of the provided set.
	/// </summary>
	/// <param name="set"> The set to make readonly. </param>
	public ReadOnlySet(ISet<T> set)
	{
		_set = set;
	}

	static ReadOnlySet()
	{
		Empty = [];
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="ReadOnlySet{T}" />
	public int Count => _set.Count;

	/// <summary>
	/// Represents an empty collection.
	/// </summary>
	public static ReadOnlySet<T> Empty { get; }

	/// <inheritdoc />
	public bool IsReadOnly => true;

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Clear()
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc cref="ReadOnlySet{T}" />
	public bool Contains(T item)
	{
		return _set.Contains(item);
	}

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
	{
		_set.CopyTo(array, arrayIndex);
	}

	/// <inheritdoc />
	public void ExceptWith(IEnumerable<T> other)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
	{
		return _set.GetEnumerator();
	}

	/// <inheritdoc />
	public void IntersectWith(IEnumerable<T> other)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc cref="ReadOnlySet{T}" />
	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		return _set.IsProperSubsetOf(other);
	}

	/// <inheritdoc cref="ReadOnlySet{T}" />
	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		return _set.IsProperSupersetOf(other);
	}

	/// <inheritdoc cref="ReadOnlySet{T}" />
	public bool IsSubsetOf(IEnumerable<T> other)
	{
		return _set.IsSubsetOf(other);
	}

	/// <inheritdoc cref="ReadOnlySet{T}" />
	public bool IsSupersetOf(IEnumerable<T> other)
	{
		return _set.IsSupersetOf(other);
	}

	/// <inheritdoc cref="ReadOnlySet{T}" />
	public bool Overlaps(IEnumerable<T> other)
	{
		return _set.Overlaps(other);
	}

	/// <inheritdoc />
	public bool Remove(T item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc cref="ReadOnlySet{T}" />
	public bool SetEquals(IEnumerable<T> other)
	{
		return _set.SetEquals(other);
	}

	/// <inheritdoc />
	public void SymmetricExceptWith(IEnumerable<T> other)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public void UnionWith(IEnumerable<T> other)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	bool ISet<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}