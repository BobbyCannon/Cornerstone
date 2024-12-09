#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Attributes;
using Cornerstone.Compare;
using Cornerstone.Extensions;
using Cornerstone.Input;

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
			or UpdateableAction.SyncIncomingModified
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
		return !Comparer.Compare(value, update, new ComparerSettings
		{
			IncludeExcludeOptions = new Dictionary<Type, IncludeExcludeSettings> { { value?.GetType(), settings } }
		});
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
			updateable.UpdateWith(update, IncludeExcludeSettings.FromExclusions(exclusions));
		}

		return UpdateWithUsingReflection(value, update, IncludeExcludeSettings.FromExclusions(exclusions));
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
			updateable.UpdateWith(update, IncludeExcludeSettings.OnlyIncluding(including));
		}

		return UpdateWithUsingReflection(value, update, IncludeExcludeSettings.OnlyIncluding(including));
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="update"> The source of the updates. </param>
	/// <param name="exclusions"> An optional list of members to exclude. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	internal static bool UpdateWithUsingReflection(this object value, object update, params string[] exclusions)
	{
		return UpdateWithUsingReflection(value, update, IncludeExcludeSettings.FromExclusions(exclusions));
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="update"> The source of the updates. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	internal static bool UpdateWithUsingReflection(this object value, object update, IncludeExcludeSettings settings)
	{
		if ((value == null) || (update == null))
		{
			return false;
		}

		settings ??= IncludeExcludeSettings.Empty;

		var destinationType = value.GetRealTypeUsingReflection();
		var sourceType = update.GetRealTypeUsingReflection();
		var destinationProperties = destinationType.GetCachedProperties();
		var sourceProperties = sourceType.GetCachedProperties();

		foreach (var thisProperty in destinationProperties)
		{
			// Ensure the source can read this property
			var canRead = (thisProperty.GetMethod != null) && thisProperty.CanRead && thisProperty.GetMethod.IsPublic;
			if (!canRead)
			{
				continue;
			}

			// Ensure the destination can write this property
			var canWrite = (thisProperty.SetMethod != null) && thisProperty.CanWrite && thisProperty.SetMethod.IsPublic;
			if (!canWrite)
			{
				continue;
			}

			// Do not try and set computed properties as it will just modify other properties
			if (thisProperty.GetCustomAttribute<ComputedPropertyAttribute>() != null)
			{
				continue;
			}

			var shouldProcess = settings.ShouldProcessProperty(thisProperty.Name);
			if (!shouldProcess)
			{
				continue;
			}

			// Check to see if the update source entity has the property
			var updateProperty = sourceProperties.FirstOrDefault(x =>
				(x.Name == thisProperty.Name)
				&& (x.PropertyType == thisProperty.PropertyType)
			);

			if (updateProperty == null)
			{
				// Skip this because target type does not have correct property name and / or type.
				continue;
			}

			var updateValue = updateProperty.GetValue(update);
			var thisValue = thisProperty.GetValue(value);

			if (!Equals(updateValue, thisValue))
			{
				thisProperty.SetValue(value, updateValue);
			}
		}

		return true;
	}

	#endregion
}