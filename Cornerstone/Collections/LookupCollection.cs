#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Collections;

internal class LookupCollection<T, T2>
	where T2 : IProvider
{
	#region Fields

	private readonly IList<T2> _cannotLookup;

	private readonly Dictionary<T, Dictionary<T, T2>> _lookup;

	#endregion

	#region Constructors

	public LookupCollection()
	{
		_lookup = new Dictionary<T, Dictionary<T, T2>>();
		_cannotLookup = new List<T2>();
	}

	#endregion

	#region Methods

	public bool TryAdd(T2 value)
	{
		if (_cannotLookup.Contains(value) || _cannotLookup.Any(x => x.GetProviderId() == value.GetProviderId()))
		{
			return false;
		}

		_cannotLookup.Add(value);
		return true;
	}

	public bool TryAdd(T first, T then, T2 value)
	{
		var collection = _lookup.GetOrAdd(first, _ => new Dictionary<T, T2>());

		if (collection.ContainsKey(then))
		{
			return false;
		}

		collection.AddIfMissing(then, () => value);
		return true;
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

		for (var i = 0; i < _cannotLookup.Count; i++)
		{
			if (!manualLookup.Invoke(_cannotLookup[i]))
			{
				continue;
			}

			value = _cannotLookup[i];
			return true;
		}

		value = default;
		return false;
	}

	#endregion
}