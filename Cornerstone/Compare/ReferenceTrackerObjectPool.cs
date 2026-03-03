#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Compare;

public class ReferenceTrackerObjectPool<T> where T : class
{
	#region Fields

	private readonly Func<T> _factory;
	private readonly Stack<T> _pool;
	private readonly Action<T> _reset;

	#endregion

	#region Constructors

	public ReferenceTrackerObjectPool(Func<T> factory, Action<T> reset)
	{
		_factory = factory;
		_pool = new();
		_reset = reset;
	}

	#endregion

	#region Methods

	public T Get()
	{
		lock (_pool)
		{
			if (_pool.Count > 0)
			{
				return _pool.Pop();
			}
		}
		return _factory();
	}

	public void Return(T item)
	{
		_reset(item);

		lock (_pool)
		{
			_pool.Push(item);
		}
	}

	#endregion
}