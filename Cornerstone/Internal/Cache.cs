#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Internal;

/// <summary>
/// Internal static class for caching items that are internal to Cornerstone.
/// </summary>
internal static class Cache
{
	#region Constructors

	static Cache()
	{
		SettableProperties = new ConcurrentDictionary<Type, ReadOnlySet<PropertyInfo>>();
		SettablePropertiesPublicOnly = new ConcurrentDictionary<Type, ReadOnlySet<PropertyInfo>>();
		PropertyDictionaryForType = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>>();
		UpdateableActionOptionsPerType = new ConcurrentDictionary<Type, ConcurrentDictionary<UpdateableAction, UpdateableOptions>>();
		UpdateableActionTypes = EnumExtensions.GetAllEnumDetails<UpdateableAction>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Property dictionary for a sync object, this is for optimization
	/// </summary>
	private static ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> PropertyDictionaryForType { get; }

	/// <summary>
	/// Property dictionary for a sync object, this is for optimization
	/// </summary>
	private static ConcurrentDictionary<Type, ReadOnlySet<PropertyInfo>> SettableProperties { get; }

	/// <summary>
	/// Property dictionary for a sync object, this is for optimization
	/// </summary>
	private static ConcurrentDictionary<Type, ReadOnlySet<PropertyInfo>> SettablePropertiesPublicOnly { get; }

	/// <summary>
	/// The cache all types with all action options.
	/// </summary>
	private static ConcurrentDictionary<Type, ConcurrentDictionary<UpdateableAction, UpdateableOptions>> UpdateableActionOptionsPerType { get; }

	/// <summary>
	/// A list of all updateable actions.
	/// </summary>
	private static IReadOnlyDictionary<UpdateableAction, EnumExtensions.EnumDetails> UpdateableActionTypes { get; }

	#endregion

	#region Methods

	/// <summary>
	/// </summary>
	/// <typeparam name="T"> </typeparam>
	/// <param name="action"> </param>
	/// <returns> </returns>
	public static IncludeExcludeOptions GetOptions<T>(UpdateableAction action)
	{
		return GetOptions(typeof(T), action);
	}

	/// <summary>
	/// Get cached updateable option for the provided type.
	/// </summary>
	/// <param name="type"> </param>
	/// <param name="action"> </param>
	/// <returns> </returns>
	public static IncludeExcludeOptions GetOptions(Type type, UpdateableAction action)
	{
		var realType = type.GetRealTypeUsingReflection();
		if (!realType.ImplementsType<IUpdateable>())
		{
			return IncludeExcludeOptions.Empty;
		}

		if (UpdateableActionOptionsPerType.TryGetValue(realType, out var actionOptions))
		{
			return actionOptions.TryGetValue(action, out var options)
				? options
				: IncludeExcludeOptions.Empty;
		}

		return IncludeExcludeOptions.Empty;
	}

	/// <summary>
	/// Get the property dictionary for a provided type.
	/// </summary>
	/// <param name="type"> The type to get the properties for. </param>
	/// <returns> The properties in a dictionary. The key is the property name. </returns>
	public static IReadOnlyDictionary<string, PropertyInfo> GetPropertyDictionary(Type type)
	{
		return PropertyDictionaryForType.GetOrAdd(type, x =>
		{
			var properties = x.GetCachedProperties().OrderBy(p => p.Name).ToArray();
			return properties.ToDictionary(p => p.Name, p => p);
		});
	}

	/// <summary>
	/// Get settable properties for an object.
	/// </summary>
	/// <param name="value"> The value to process. </param>
	/// <returns> The settable properties information. </returns>
	public static ReadOnlySet<PropertyInfo> GetSettableProperties(object value)
	{
		var realType = value is Notifiable notifiable
			? notifiable.GetRealType()
			: value.GetRealTypeUsingReflection();

		return SettableProperties
			.GetOrAdd(realType, t => t
				.GetCachedProperties()
				.Where(x => x.CanWrite)
				.ToHashSet()
				.ToReadOnlySet()
			);
	}

	internal static void Initialize(IUpdateable value)
	{
		var realType = value.GetRealTypeUsingReflection();

		if (UpdateableActionOptionsPerType.ContainsKey(realType))
		{
			return;
		}

		var properties = GetPropertyDictionary(realType).Keys;
		var typeOptions = UpdateableActionOptionsPerType.GetOrAdd(realType,
			_ => new ConcurrentDictionary<UpdateableAction, UpdateableOptions>());

		foreach (var actionType in UpdateableActionTypes.Keys)
		{
			if (actionType == UpdateableAction.Unknown)
			{
				continue;
			}

			typeOptions.GetOrAdd(actionType, x =>
			{
				var included = value.GetDefaultIncludedProperties(x);
				var excluded = properties.Except(included).ToList();
				return new UpdateableOptions(x, included, excluded);
			});
		}
	}

	internal static bool ShouldProcessProperty<T>(T entity, UpdateableAction action, string propertyName) where T : IEntity
	{
		return ShouldProcessProperty(entity.GetRealType(), action, propertyName);
	}

	internal static bool ShouldProcessProperty(Type type, UpdateableAction action, string propertyName)
	{
		return UpdateableActionOptionsPerType.TryGetValue(type, out var actionOptions)
			&& actionOptions.TryGetValue(action, out var actions)
			&& actions.ShouldProcessProperty(propertyName);
	}

	#endregion
}