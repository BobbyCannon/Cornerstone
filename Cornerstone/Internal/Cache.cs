#region References

using System;
using System.Collections.Concurrent;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

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
		EnumDetails = new ConcurrentDictionary<Type, EnumDetails[]>();
		UpdateableActionOptionsPerType = new ConcurrentDictionary<Type, ConcurrentDictionary<UpdateableAction, UpdateableSettings>>();
		UpdateableActionTypes = UpdateableExtensions.GetUpdateableActions();
	}

	#endregion

	#region Properties

	public static ConcurrentDictionary<Type, EnumDetails[]> EnumDetails { get; }

	/// <summary>
	/// The cache all types with all action options.
	/// </summary>
	private static ConcurrentDictionary<Type, ConcurrentDictionary<UpdateableAction, UpdateableSettings>> UpdateableActionOptionsPerType { get; }

	/// <summary>
	/// A list of all updateable actions.
	/// </summary>
	private static UpdateableAction[] UpdateableActionTypes { get; }

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
	/// <param name="updateable"> The instance to get settings for. </param>
	/// <param name="action"> The action to get settings for. </param>
	/// <returns> The settings. </returns>
	public static IncludeExcludeSettings GetSettings(IUpdateable updateable, UpdateableAction action)
	{
		var realType = updateable.GetType();
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

		return Initialize(updateable, action);
	}

	/// <summary>
	/// Get cached updateable setting for the provided type.
	/// </summary>
	/// <param name="type"> The type to get settings for. </param>
	/// <param name="action"> The action to get settings for. </param>
	/// <returns> The settings. </returns>
	public static IncludeExcludeSettings GetSettings(Type type, UpdateableAction action)
	{
		var realType = type;
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

	internal static IncludeExcludeSettings Initialize(IUpdateable value, UpdateableAction action = UpdateableAction.Updateable)
	{
		var realType = value.GetType();

		if (UpdateableActionOptionsPerType.TryGetValue(realType, out var found))
		{
			return found.TryGetValue(action, out var settings)
				? settings
				: IncludeExcludeSettings.Empty;
		}

		var properties = SourceReflector.GetRequiredSourceType(realType)
			.GetProperties().Select(x => x.Name).ToList();

		var typeOptions = UpdateableActionOptionsPerType.GetOrAdd(realType,
			_ => new ConcurrentDictionary<UpdateableAction, UpdateableSettings>());

		foreach (var actionType in UpdateableActionTypes)
		{
			typeOptions.GetOrAdd(actionType, x =>
			{
				var included = value.GetDefaultIncludedProperties(x);
				var excluded = properties.Except(included).ToList();
				return new UpdateableSettings(x, included, excluded);
			});
		}

		return typeOptions.TryGetValue(action, out var settings2)
			? settings2
			: IncludeExcludeSettings.Empty;
	}

	internal static bool ShouldProcessProperty<T>(T entity, UpdateableAction action, string propertyName)
	{
		return ShouldProcessProperty(entity.GetType(), action, propertyName);
	}

	internal static bool ShouldProcessProperty(Type type, UpdateableAction action, string propertyName)
	{
		return UpdateableActionOptionsPerType.TryGetValue(type, out var actionOptions)
			&& actionOptions.TryGetValue(action, out var actions)
			&& actions.ShouldProcessProperty(propertyName);
	}

	#endregion
}