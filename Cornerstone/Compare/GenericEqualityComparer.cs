#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Compare;

/// <summary>
/// Exposes a method that compares two objects.
/// </summary>
/// <typeparam name="T"> The type of the object to compare. </typeparam>
public class GenericEqualityComparer<T> : IEqualityComparer<T>
{
	#region Fields

	private readonly Func<T, T, bool> _compare;
	private readonly Func<T, int> _hashCode;

	#endregion

	#region Constructors

	public GenericEqualityComparer(Func<T, T, bool> compare = null, Func<T, int> hashCode = null)
	{
		_compare = compare ?? EqualityComparer<T>.Default.Equals;
		_hashCode = hashCode ?? EqualityComparer<T>.Default.GetHashCode;
	}

	#endregion

	#region Methods

	public bool Equals(T x, T y)
	{
		if (ReferenceEquals(x, y))
		{
			return true;
		}

		if ((x is null) || (y is null))
		{
			return false;
		}

		return _compare(x, y);
	}

	public int GetHashCode(T obj)
	{
		// Dictionary forbids null keys; still be safe if callers invoke GetHashCode directly.
		if (obj is null)
		{
			return 0;
		}

		return _hashCode.Invoke(obj);
	}

	#endregion
}