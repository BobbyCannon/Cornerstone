#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Convert;

internal class ConverterCollection<T, T2>
{
	#region Fields

	private readonly Dictionary<T, Dictionary<T, T2>> _lookup;
	private readonly IList<T2> _manualLookup;

	#endregion

	#region Constructors

	public ConverterCollection()
	{
		_lookup = new Dictionary<T, Dictionary<T, T2>>();
		_manualLookup = new List<T2>();
	}

	#endregion

	#region Methods

	public void AddConverter(T2 value)
	{
		if (_manualLookup.Contains(value))
		{
			return;
		}

		_manualLookup.Add(value);
	}

	public bool TryAdd(T from, T to, T2 value)
	{
		Dictionary<T, T2> dictionary;
		if (_lookup.TryGetValue(from, out var d))
		{
			dictionary = d;
		}
		else
		{
			dictionary = new Dictionary<T, T2>();
			_lookup.Add(from, dictionary);

		}

		return dictionary.TryAdd(to, value);
	}

	public bool TryGetValue(T first, T then, Func<T2, bool> manualLookup, out T2 value)
	{
		if (_lookup.TryGetValue(first, out var collection))
		{
			if (collection.TryGetValue(then, out value))
			{
				return true;
			}
		}

		for (var i = 0; i < _manualLookup.Count; i++)
		{
			if (!manualLookup.Invoke(_manualLookup[i]))
			{
				continue;
			}

			value = _manualLookup[i];
			return true;
		}

		value = default;
		return false;
	}

	#endregion
}