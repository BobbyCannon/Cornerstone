﻿#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Compare;

/// <summary>
/// Exposes a method that compares two objects.
/// </summary>
/// <typeparam name="T"> The type of the object to compare. </typeparam>
public class GenericComparer<T> : EqualityComparer<T>, IComparer, IComparer<T>
{
	#region Fields

	private readonly Func<T, T, int> _compare;
	private readonly Func<T, int> _getHashCode;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the comparer.
	/// </summary>
	/// <param name="getHashCode"> An optional override for GetHashCode. If not provided then use the T.GetHashCode. </param>
	public GenericComparer(Func<T, int> getHashCode = null) : this((x, y) => object.Equals(x, y) ? 0 : 1, getHashCode)
	{
	}

	/// <summary>
	/// Create an instance of the comparer.
	/// </summary>
	/// <param name="compare"> The function to compare two objects. </param>
	/// <param name="getHashCode"> An optional override for GetHashCode. If not provided then use the T.GetHashCode. </param>
	public GenericComparer(Func<T, T, int> compare, Func<T, int> getHashCode = null)
	{
		_compare = compare;
		_getHashCode = getHashCode;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public int Compare(T x, T y)
	{
		return _compare(x, y);
	}

	/// <inheritdoc />
	public int Compare(object x, object y)
	{
		return _compare((T) x, (T) y);
	}

	/// <inheritdoc />
	public override bool Equals(T x, T y)
	{
		return _compare(x, y) == 0;
	}

	/// <inheritdoc />
	public override int GetHashCode(T obj)
	{
		return _getHashCode?.Invoke(obj) ?? obj.GetHashCode();
	}

	#endregion
}