#region References

using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Extensions for the IUpdateable interface
/// </summary>
public static class UpdateableExtensions
{
	#region Fields

	private static UpdateableAction[] _updateableActions;

	#endregion

	#region Methods

	/// <summary>
	/// Get only the flagged values for the updateable action.
	/// </summary>
	/// <returns> The updateable action flagged values. </returns>
	public static UpdateableAction[] GetUpdateableActions()
	{
		_updateableActions ??= SourceReflector
			.GetEnumValues<UpdateableAction>().Where(x => x.IsSingleFlag()).ToArray();
		return _updateableActions;
	}

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
	/// Shallow clone the IUpdateable using Updatable settings.
	/// </summary>
	/// <param name="value"> The value to be shallow cloned. </param>
	/// <returns> The cloned object. </returns>
	public static T ShallowClone<T>(this T value) where T : IUpdateable
	{
		var type = value.GetType();
		var response = (T) SourceReflector.CreateInstance(type);
		response.UpdateWith(value, IncludeExcludeSettingsExtensions.GetIncludeExcludeSettings(type, UpdateableAction.Updateable));
		return response;
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
		// todo: implement?
		return true;
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

		return false;
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

		return false;
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

		return false;
	}

	#endregion
}