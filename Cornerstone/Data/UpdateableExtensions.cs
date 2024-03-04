#region References

using System.Linq;
using System.Reflection;
using Cornerstone.Attributes;
using Cornerstone.Extensions;

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
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	public static bool ShouldUpdate(this IUpdateable value, object update, UpdateableOptions options)
	{
		return !Equals(value, update);
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="update"> The source of the updates. </param>
	/// <param name="exclusions"> An optional list of members to exclude. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public static bool UpdateWith(this object value, object update, params string[] exclusions)
	{
		if (value is IUpdateable updateable)
		{
			updateable.UpdateWith(update, UpdateableOptions.FromExclusions(exclusions));
		}

		return UpdateWithUsingReflection(value, update, UpdateableOptions.FromExclusions(exclusions));
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="update"> The source of the updates. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public static bool UpdateWith(this object value, object update, UpdateableOptions options)
	{
		if (value is IUpdateable updateable)
		{
			updateable.UpdateWith(update, options);
		}

		return UpdateWithUsingReflection(value, update, options);
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
		return UpdateWithUsingReflection(value, update, UpdateableOptions.FromExclusions(exclusions));
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="update"> The source of the updates. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	internal static bool UpdateWithUsingReflection(this object value, object update, UpdateableOptions options)
	{
		if ((value == null) || (update == null))
		{
			return false;
		}

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

			var shouldProcess = options.ShouldProcessProperty(thisProperty.Name);
			if (!shouldProcess)
			{
				continue;
			}

			// Check to see if the update source entity has the property
			var updateProperty = sourceProperties.FirstOrDefault(x => (x.Name == thisProperty.Name) && (x.PropertyType == thisProperty.PropertyType));
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