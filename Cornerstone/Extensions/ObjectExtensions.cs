#region References

using System;
using System.Collections.Concurrent;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Extensions;

public static class ObjectExtensions
{
	#region Fields

	private static readonly ConcurrentDictionary<SourceTypeInfo, object> _defaultValuesCache;

	#endregion

	#region Constructors

	static ObjectExtensions()
	{
		_defaultValuesCache = new();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Checks to see if the value is the default value of the type provide.
	/// Ex. IsEnabled = true, check the property to see if it is the default value of true
	/// Name = null, check the property to see if it is the default value of null
	/// </summary>
	public static bool IsDefaultValue(SourceTypeInfo typeInfo, SourcePropertyInfo propertyInfo, object typeValue)
	{
		// Cache the default value for this exact value type (created only once)
		var defaultInstance = _defaultValuesCache.GetOrAdd(typeInfo, static t => SourceReflector.CreateInstance(t));
		var defaultPropertyValue = propertyInfo.GetValue(defaultInstance);
		var propertyValue = propertyInfo.GetValue(typeValue);
		return Equals(propertyValue, defaultPropertyValue);
	}

	#endregion
}