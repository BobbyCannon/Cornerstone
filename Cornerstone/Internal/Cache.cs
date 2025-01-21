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
		UpdateableActionOptionsPerType = new ConcurrentDictionary<Type, ConcurrentDictionary<UpdateableAction, UpdateableSettings>>();
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
	private static ConcurrentDictionary<Type, ConcurrentDictionary<UpdateableAction, UpdateableSettings>> UpdateableActionOptionsPerType { get; }

	/// <summary>
	/// A list of all updateable actions.
	/// </summary>
	private static IReadOnlyDictionary<UpdateableAction, EnumExtensions.EnumDetails> UpdateableActionTypes { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Get cached updateable setting for the provided type.
	/// </summary>
	/// <typeparam name="T"> The type to get settings for. </typeparam>
	/// <param name="action"> The action to get settings for. </param>
	/// <returns> The settings. </returns>
	public static IncludeExcludeSettings GetSettings<T>(UpdateableAction action)
	{
		return GetSettings(typeof(T), action);
	}

	/// <summary>
	/// Get cached updateable setting for the provided type.
	/// </summary>
	/// <param name="type"> The type to get settings for. </param>
	/// <param name="action"> The action to get settings for. </param>
	/// <returns> The settings. </returns>
	public static IncludeExcludeSettings GetSettings(Type type, UpdateableAction action)
	{
		var realType = type.GetRealTypeUsingReflection();
		if (!realType.ImplementsType<IUpdateable>())
		{
			return IncludeExcludeSettings.Empty;
		}

		if (UpdateableActionOptionsPerType.TryGetValue(realType, out var actionOptions))
		{
			return actionOptions.TryGetValue(action, out var options)
				? options
				: IncludeExcludeSettings.Empty;
		}

		return IncludeExcludeSettings.Empty;
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
				.Where(x =>
					x.CanWrite
					&& (x.SetMethod?.IsPublic == true)
					&& !x.IsIndexer()
				)
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
			_ => new ConcurrentDictionary<UpdateableAction, UpdateableSettings>());

		foreach (var actionType in UpdateableActionTypes.Keys)
		{
			if (actionType == UpdateableAction.None)
			{
				continue;
			}

			typeOptions.GetOrAdd(actionType, x =>
			{
				var included = value.GetDefaultIncludedProperties(x);
				var excluded = properties.Except(included).ToList();
				return new UpdateableSettings(x, included, excluded);
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