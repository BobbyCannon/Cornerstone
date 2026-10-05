#region References

#region References

using System;
using System.Collections.Generic;
using WmiLight;

#endregion

namespace Cornerstone.Agent.Hardware;

#endregion

internal class WmiLightQueryProvider : IWmiQueryProvider
{
	#region Fields

	private readonly TimeSpan? _enumerationOptionsTimeout;
	private readonly Dictionary<string, WmiConnection> _wmiConnectionDict = new();

	#endregion

	#region Constructors

	public WmiLightQueryProvider(TimeSpan? enumerationOptionsTimeout = null)
	{
		_enumerationOptionsTimeout = enumerationOptionsTimeout;
	}

	#endregion

	#region Methods

	public IEnumerable<IWmiPropertySource> Query(string scope, string query)
	{
		if (!_wmiConnectionDict.TryGetValue(scope, out var wmiConnection))
		{
			wmiConnection = new WmiConnection(scope);
			_wmiConnectionDict[scope] = wmiConnection;
		}

		if (_enumerationOptionsTimeout.HasValue)
		{
			foreach (var mo in wmiConnection.CreateQuery(query, _enumerationOptionsTimeout.Value))
			{
				yield return new WmiLightObjectAdapter(mo);
			}
		}
		else
		{
			foreach (var mo in wmiConnection.CreateQuery(query))
			{
				yield return new WmiLightObjectAdapter(mo);
			}
		}
	}

	public IEnumerable<IWmiPropertySource> QueryRelated(string scope, IWmiPropertySource wmiPropertySource, string relatedClass)
	{
		if (!_wmiConnectionDict.TryGetValue(scope, out var wmiConnection))
		{
			wmiConnection = new WmiConnection(scope);
			_wmiConnectionDict[scope] = wmiConnection;
		}

		var wmiLightObjectAdapter = wmiPropertySource as WmiLightObjectAdapter;

		if (wmiLightObjectAdapter is null)
		{
			yield break;
		}

		var wmiObject = wmiLightObjectAdapter.GetWmiObject();

		if (_enumerationOptionsTimeout.HasValue)
		{
			foreach (var mo in wmiConnection.CreateQueryForRelated(wmiObject, relatedClass, _enumerationOptionsTimeout.Value))
			{
				yield return new WmiLightObjectAdapter(mo);
			}
		}
		else
		{
			foreach (var mo in wmiConnection.CreateQueryForRelated(wmiObject, relatedClass))
			{
				yield return new WmiLightObjectAdapter(mo);
			}
		}
	}

	#endregion
}

internal interface IWmiQueryProvider
{
	#region Methods

	IEnumerable<IWmiPropertySource> Query(string scope, string query);

	IEnumerable<IWmiPropertySource> QueryRelated(string scope, IWmiPropertySource wmiPropertySource, string relatedClass);

	#endregion
}

internal interface IWmiPropertySource
{
	#region Properties

	object this[string propertyName] { get; }

	#endregion
}