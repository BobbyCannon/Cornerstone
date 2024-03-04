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

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the comparer.
	/// </summary>
	public GenericEqualityComparer() : this((x, y) => object.Equals(x, y))
	{
	}

	/// <summary>
	/// Create an instance of the comparer.
	/// </summary>
	/// <param name="compare"> The function to compare two objects. </param>
	public GenericEqualityComparer(Func<T, T, bool> compare)
	{
		_compare = compare;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual bool Equals(T x, T y)
	{
		return _compare(x, y);
	}

	/// <inheritdoc />
	public virtual int GetHashCode(T obj)
	{
		return obj.GetHashCode();
	}

	#endregion
}