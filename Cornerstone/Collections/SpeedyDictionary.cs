#region References

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
public class SpeedyDictionary<T, T2> : ReaderWriterLockBindable, IDictionary<T, T2>
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}