#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Storage.Configuration;

public static class TypeConfigurationCache
{
	#region Fields

	private static readonly Lazy<Dictionary<Type, TypeConfiguration>> _cache;

	#endregion

	#region Constructors

	static TypeConfigurationCache()
	{
		_cache = new();
	}

	#endregion

	#region Properties

	public static IReadOnlyDictionary<Type, TypeConfiguration> All => _cache.Value;

	#endregion

	#region Methods

	public static void Add(Type entityType, Action<TypeConfiguration> configure)
	{
		if (All.TryGetValue(entityType, out _))
		{
		}
	}

	public static TypeConfiguration Get(Type entityType)
	{
		return All.TryGetValue(entityType, out var model)
			? model
			: throw new KeyNotFoundException($"No configuration for {entityType}");
	}

	public static bool TryGet(Type entityType, out TypeConfiguration configuration)
	{
		return All.TryGetValue(entityType, out configuration);
	}

	#endregion
}