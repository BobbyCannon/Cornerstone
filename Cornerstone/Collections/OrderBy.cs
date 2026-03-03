#region References

using System;
using System.Linq.Expressions;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents an order by value.
/// </summary>
/// <typeparam name="T"> The type of the item to order. </typeparam>
public class OrderBy<T>
{
	#region Constructors

	/// <summary>
	/// Instantiate an instance of the order by value.
	/// </summary>
	/// <param name="descending"> True to order descending and otherwise sort ascending. Default value is false for ascending order. </param>
	public OrderBy(bool descending = false) : this(x => x, descending)
	{
		if (!typeof(T).ImplementsType(typeof(IComparable)))
		{
			throw new InvalidOperationException("The type must implement IComparable to use this constructor.");
		}
	}

	/// <summary>
	/// Instantiate an instance of the order by value.
	/// </summary>
	/// <param name="keySelector"> The key selector expression. </param>
	/// <param name="descending"> True to order descending and otherwise sort ascending. Default value is false for ascending order. </param>
	public OrderBy(Expression<Func<T, object>> keySelector, bool descending = false)
	{
		keySelector ??= x => x;

		KeySelector = keySelector;
		CompiledSelector = keySelector.Compile();
		Descending = descending;
	}

	#endregion

	#region Properties

	public Func<T, object> CompiledSelector { get; }

	public bool Descending { get; }

	public Expression<Func<T, object>> KeySelector { get; }

	#endregion
}