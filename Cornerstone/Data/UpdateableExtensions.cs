#region References

using System;
using System.Collections.Generic;
using System.Reflection;
using Cornerstone.Attributes;
using Cornerstone.Compare;
using Cornerstone.Extensions;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Extensions for the IUpdateable interface
/// </summary>
public static class UpdateableExtensions
{
	#region Methods

	/// <summary>
	/// Returns true if the action is a sync action (Add, Modified, or Outgoing)
	/// </summary>
	/// <param name="action"> The action to check. </param>
	/// <returns> True if sync action otherwise false. </returns>
	public static bool IsSyncAction(this UpdateableAction action)
	{
		return action is UpdateableAction.SyncIncomingAdd
			or UpdateableAction.SyncIncomingUpdate
			or UpdateableAction.SyncOutgoing;
	}

	/// <summary>
	/// Determine if the update should be applied.
	/// </summary>
	/// <param name="value"> The value to be tested. </param>
	/// <param name="update"> The update to be tested. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	public static bool ShouldUpdate(this IUpdateable value, object update, IncludeExcludeSettings settings)
	{
		var session = Comparer.Compare(value, update, new ComparerSettings
		{
			TypeIncludeExcludeSettings = new Dictionary<Type, IncludeExcludeSettings> { { value?.GetType(), settings } }
		});

		return session.Result != CompareResult.AreEqual;
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="update"> The source of the updates. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public static bool UpdateWith(this object value, object update, IncludeExcludeSettings settings)
	{
		if (value is IUpdateable updateable)
		{
			updateable.UpdateWith(update, settings);
		}

		return UpdateWithUsingReflection(value, update, settings);
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="update"> The source of the updates. </param>
	/// <param name="exclusions"> An optional list of members to exclude. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public static bool UpdateWithExcept(this object value, object update, params string[] exclusions)
	{
		if (value is IUpdateable updateable)
		{
			updateable.UpdateWith(update, exclusions.ToOnlyExcludingSettings());
		}

		return UpdateWithUsingReflection(value, update, exclusions.ToOnlyExcludingSettings());
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="update"> The source of the updates. </param>
	/// <param name="including"> A list of properties to include. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public static bool UpdateWithOnly(this object value, object update, params string[] including)
	{
		if (value is IUpdateable updateable)
		{
			updateable.UpdateWith(update, including.ToOnlyIncludingSettings());
		}

		return UpdateWithUsingReflection(value, update, including.ToOnlyIncludingSettings());
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="destination"> The value to be updated. </param>
	/// <param name="source"> The source of the updates. </param>
	/// <param name="action"> The action of the updateable. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public static bool UpdateWithUsingReflection(this object destination, object source, UpdateableAction action)
	{
		if (destination == null)
		{
			return false;
		}

		var settings = Cache.GetSettings(destination.GetType(), action);
		return UpdateWithUsingReflection(destination, source, settings);
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="destination"> The value to be updated. </param>
	/// <param name="source"> The source of the updates. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	internal static bool UpdateWithUsingReflection(this object destination, object source, IncludeExcludeSettings settings)
	{
		if ((destination == null) || (source == null))
		{
			return false;
		}

		settings ??= IncludeExcludeSettings.Empty;

		var response = false;
		var destinationType = destination.GetRealTypeUsingReflection();
		var sourceType = source.GetRealTypeUsingReflection();
		var destinationProperties = destinationType.GetCachedProperties();
		var sourceProperties = sourceType.GetCachedPropertyDictionary();

		foreach (var destinationProperty in destinationProperties)
		{
			var shouldProcess = settings.ShouldProcessProperty(destinationProperty.Name);
			if (!shouldProcess)
			{
				continue;
			}

			// Do not try and set computed properties as it will just modify other properties
			if (destinationProperty.GetCustomAttribute<ComputedPropertyAttribute>() != null)
			{
				continue;
			}

			if (!sourceProperties.TryGetValue(destinationProperty.Name, out var sourceProperty))
			{
				continue;
			}

			// Ensure the source can read this property
			var canRead = (sourceProperty.GetMethod != null)
				&& sourceProperty.CanRead
				&& sourceProperty.GetMethod.IsPublic;
			if (!canRead)
			{
				continue;
			}

			// Ensure the destination can write this property
			var canWrite = (destinationProperty.SetMethod != null)
				&& destinationProperty.CanWrite
				&& destinationProperty.SetMethod.IsPublic;
			if (!canWrite)
			{
				continue;
			}

			if (sourceProperty.PropertyType != destinationProperty.PropertyType)
			{
				// Skip this because target type does not have correct property name and / or type.
				continue;
			}

			var sourceValue = sourceProperty.GetValue(source);
			var destinationValue = destinationProperty.GetValue(destination);

			if (!Equals(sourceValue, destinationValue))
			{
				destinationProperty.SetValue(destination, sourceValue);
				response = true;
			}
		}

		return response;
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="destination"> The value to be updated. </param>
	/// <param name="source"> The source of the updates. </param>
	/// <param name="exclusions"> An optional list of members to exclude. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	internal static bool UpdateWithUsingReflection(this object destination, object source, params string[] exclusions)
	{
		return UpdateWithUsingReflection(destination, source, exclusions.ToOnlyExcludingSettings());
	}

	#endregion
}