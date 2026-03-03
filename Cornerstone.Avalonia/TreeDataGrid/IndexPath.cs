#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid;

public readonly struct IndexPath : IReadOnlyList<int>,
	IComparable<IndexPath>,
	IEquatable<IndexPath>
{
	#region Fields

	public static readonly IndexPath Unselected = default;

	private readonly int _index;
	private readonly int[] _path;

	#endregion

	#region Constructors

	public IndexPath(int index)
	{
		_index = index + 1;
		_path = null;
	}

	public IndexPath(params int[] indexes)
	{
		_index = 0;
		_path = indexes;
	}

	public IndexPath(IEnumerable<int> indexes)
	{
		if (indexes != null)
		{
			_index = 0;
			_path = indexes.ToArray();
		}
		else
		{
			_index = 0;
			_path = null;
		}
	}

	private IndexPath(int[] basePath, int index)
	{
		basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));

		_index = 0;
		_path = new int[basePath.Length + 1];
		Array.Copy(basePath, _path, basePath.Length);
		_path[basePath.Length] = index;
	}

	#endregion

	#region Properties

	public int Count => _path?.Length ?? (_index == 0 ? 0 : 1);

	public int this[int index]
	{
		get
		{
			if (index >= Count)
			{
				throw new IndexOutOfRangeException();
			}
			return _path?[index] ?? _index - 1;
		}
	}

	#endregion

	#region Methods

	public IndexPath Append(int childIndex)
	{
		if (childIndex < 0)
		{
			throw new ArgumentException("Invalid child index", nameof(childIndex));
		}

		if (_path != null)
		{
			return new IndexPath(_path, childIndex);
		}
		if (_index != 0)
		{
			return new IndexPath(_index - 1, childIndex);
		}
		return new IndexPath(childIndex);
	}

	public int CompareTo(IndexPath other)
	{
		var rhsPath = other;
		var compareResult = 0;
		var lhsCount = Count;
		var rhsCount = rhsPath.Count;

		if ((lhsCount == 0) || (rhsCount == 0))
		{
			// one of the paths are empty, compare based on size
			compareResult = lhsCount - rhsCount;
		}
		else
		{
			// both paths are non-empty, but can be of different size
			for (var i = 0; i < Math.Min(lhsCount, rhsCount); i++)
			{
				if (this[i] < rhsPath[i])
				{
					compareResult = -1;
					break;
				}
				if (this[i] > rhsPath[i])
				{
					compareResult = 1;
					break;
				}
			}

			// if both match upto min(lhsCount, rhsCount), compare based on size
			compareResult = compareResult == 0 ? lhsCount - rhsCount : compareResult;
		}

		if (compareResult != 0)
		{
			compareResult = compareResult > 0 ? 1 : -1;
		}

		return compareResult;
	}

	public override bool Equals(object obj)
	{
		return obj is IndexPath other && Equals(other);
	}

	public bool Equals(IndexPath other)
	{
		return CompareTo(other) == 0;
	}

	public IEnumerator<int> GetEnumerator()
	{
		static IEnumerator<int> EnumerateSingleOrEmpty(int index)
		{
			if (index != 0)
			{
				yield return index - 1;
			}
		}

		return ((IEnumerable<int>) _path)?.GetEnumerator() ?? EnumerateSingleOrEmpty(_index);
	}

	public override int GetHashCode()
	{
		var hashCode = -504981047;

		if (_path != null)
		{
			foreach (var i in _path)
			{
				hashCode = (hashCode * -1521134295) + i.GetHashCode();
			}
		}
		else
		{
			hashCode = (hashCode * -1521134295) + _index.GetHashCode();
		}

		return hashCode;
	}

	public bool IsAncestorOf(in IndexPath other)
	{
		if (other.Count <= Count)
		{
			return false;
		}

		var size = Count;

		for (var i = 0; i < size; i++)
		{
			if (this[i] != other[i])
			{
				return false;
			}
		}

		return true;
	}

	public bool IsParentOf(in IndexPath other)
	{
		var size = Count;

		if (other.Count == (size + 1))
		{
			for (var i = 0; i < size; ++i)
			{
				if (this[i] != other[i])
				{
					return false;
				}
			}

			return true;
		}

		return false;
	}

	public static bool operator ==(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) == 0;
	}

	public static bool operator ==(IndexPath? x, IndexPath? y)
	{
		return (x ?? default).CompareTo(y ?? default) == 0;
	}

	public static bool operator >(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) > 0;
	}

	public static bool operator >=(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) >= 0;
	}

	public static implicit operator IndexPath(int index)
	{
		return new(index);
	}

	public static bool operator !=(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) != 0;
	}

	public static bool operator !=(IndexPath? x, IndexPath? y)
	{
		return (x ?? default).CompareTo(y ?? default) != 0;
	}

	public static bool operator <(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) < 0;
	}

	public static bool operator <=(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) <= 0;
	}

	public IndexPath Slice(int start, int length)
	{
		if ((start < 0) || ((start + length) > Count))
		{
			throw new IndexOutOfRangeException("Invalid IndexPath slice.");
		}

		if (length == 0)
		{
			return default;
		}
		if (length == 1)
		{
			return new(this[start]);
		}
		var slice = new int[length];
		Array.Copy(_path!, start, slice, 0, length);
		return new(slice);
	}

	public int[] ToArray()
	{
		var result = new int[Count];

		if (_path is not null)
		{
			_path.CopyTo(result, 0);
		}
		else if (result.Length > 0)
		{
			result[0] = _index - 1;
		}

		return result;
	}

	public override string ToString()
	{
		if (_path != null)
		{
			return $"({string.Join(".", _path)})";
		}
		if (_index != 0)
		{
			return $"({_index - 1})";
		}
		return "()";
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}