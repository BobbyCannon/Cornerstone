#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// A thread-safe dictionary.
/// </summary>
/// <typeparam name="T"> The type of key. </typeparam>
/// <typeparam name="T2"> The type of value. </typeparam>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(SpeedyDictionary<,>))]
public class SpeedyDictionary<T, T2> : ReaderWriterLockBindable, ISpeedyDictionary<T, T2>
{
	#region Fields

	private readonly IDictionary<T, T2> _dictionary;

	#endregion

	#region Constructors

	public SpeedyDictionary() : this(null)
	{
	}

	public SpeedyDictionary(IDispatcher dispatcher) : base(null, dispatcher)
	{
		_dictionary = new Dictionary<T, T2>();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int Count => _dictionary.Count;

	/// <inheritdoc />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	[SuppressPropertyChangedWarnings]
	public T2 this[T key]
	{
		get
		{
			try
			{
				EnterReadLock();

				return _dictionary[key];
			}
			finally
			{
				ExitReadLock();
			}
		}
		set
		{
			try
			{
				EnterWriteLock();

				_dictionary[key] = value;
			}
			finally
			{
				ExitWriteLock();
			}
		}
	}

	/// <inheritdoc />
	public ICollection<T> Keys
	{
		get
		{
			try
			{
				EnterReadLock();

				return _dictionary.Keys;
			}
			finally
			{
				ExitReadLock();
			}
		}
	}

	/// <inheritdoc />
	public ICollection<T2> Values
	{
		get
		{
			try
			{
				EnterReadLock();

				return _dictionary.Values;
			}
			finally
			{
				ExitReadLock();
			}
		}
	}

	IEnumerable<T> IReadOnlyDictionary<T, T2>.Keys => Keys;

	IEnumerable<T2> IReadOnlyDictionary<T, T2>.Values => Values;

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Add(KeyValuePair<T, T2> item)
	{
		Add(item.Key, item.Value);
	}

	/// <inheritdoc />
	public void Add(T key, T2 value)
	{
		try
		{
			EnterWriteLock();

			_dictionary.Add(key, value);
		}
		finally
		{
			ExitWriteLock();
		}
	}

	public void AddOrUpdate(T key, Func<T2> value, Func<T2, T2> update)
	{
		try
		{
			EnterWriteLock();

			if (_dictionary.ContainsKey(key))
			{
				var oldValue = _dictionary[key];
				var newValue = update(oldValue);
				_dictionary[key] = newValue;
			}
			else
			{
				var newValue = value.Invoke();
				_dictionary.Add(key, newValue);
			}
		}
		finally
		{
			ExitWriteLock();
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		try
		{
			EnterWriteLock();

			_dictionary.Clear();
		}
		finally
		{
			ExitWriteLock();
		}
	}

	/// <inheritdoc />
	public bool Contains(KeyValuePair<T, T2> item)
	{
		return ContainsKey(item.Key);
	}

	/// <inheritdoc cref="IDictionary" />
	public bool ContainsKey(T key)
	{
		try
		{
			EnterReadLock();

			return _dictionary.ContainsKey(key);
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <inheritdoc />
	public void CopyTo(KeyValuePair<T, T2>[] array, int arrayIndex)
	{
		try
		{
			EnterReadLock();

			_dictionary.CopyTo(array, arrayIndex);
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <inheritdoc />
	public IEnumerator<KeyValuePair<T, T2>> GetEnumerator()
	{
		try
		{
			EnterReadLock();

			return _dictionary.GetEnumerator();
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <inheritdoc cref="IDictionary" />
	public T2 GetOrAdd(T key, T2 value)
	{
		try
		{
			EnterUpgradeableReadLock();

			if (_dictionary.TryGetValue(key, out var result))
			{
				return result;
			}

			try
			{
				EnterWriteLock();
				_dictionary.Add(key, value);
				return value;
			}
			finally
			{
				ExitWriteLock();
			}
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	public async Task<T2> GetOrAdd(T key, Func<T, Task<T2>> create)
	{
		try
		{
			EnterUpgradeableReadLock();

			if (_dictionary.TryGetValue(key, out var result))
			{
				return result;
			}

			try
			{
				EnterWriteLock();
				result = await create(key);
				_dictionary.Add(key, result);
				return result;
			}
			finally
			{
				ExitWriteLock();
			}
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	/// <inheritdoc />
	public bool Remove(KeyValuePair<T, T2> item)
	{
		return Remove(item.Key);
	}

	/// <inheritdoc />
	public bool Remove(T key)
	{
		try
		{
			EnterWriteLock();

			return _dictionary.Remove(key);
		}
		finally
		{
			ExitWriteLock();
		}
	}

	/// <inheritdoc cref="IDictionary" />
	public bool TryGetValue(T key, out T2 value)
	{
		try
		{
			EnterReadLock();

			return _dictionary.TryGetValue(key, out value);
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		if (update is IDictionary<T, T2> list)
		{
			this.Reconcile(list);
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}

public interface ISpeedyDictionary<T, T2> : IDictionary<T, T2>, IReadOnlyDictionary<T, T2>
{
}