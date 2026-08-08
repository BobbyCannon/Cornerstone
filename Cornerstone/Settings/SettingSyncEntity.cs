#region References

using System;
using System.Diagnostics;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Serialization;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a setting.
/// </summary>
/// <typeparam name="TKey"> The type of the Value of the setting. </typeparam>
public partial class SettingSyncEntity<TKey> : SyncEntity<TKey>, ISetting
{
	#region Properties

	/// <summary>
	/// Set to mark this setting as a syncable setting.
	/// </summary>
	[UpdateableAction(UpdateableAction.EverythingExceptSync | UpdateableAction.SyncOutgoing)]
	public bool CanSync { get; set; }

	/// <summary>
	/// The category for the settings.
	/// </summary>
	[UpdateableAction(UpdateableAction.EverythingExceptSyncUpdate)]
	public string Category { get; set; }

	/// <summary>
	/// Optionally expires on value, DateTime.MinValue means there is no expiration.
	/// </summary>
	[UpdateableAction(UpdateableAction.EverythingExceptSync)]
	public DateTime ExpiresOn { get; set; }

	/// <summary>
	/// The name of the setting.
	/// </summary>
	[UpdateableAction(UpdateableAction.EverythingExceptSyncUpdate)]
	public string Name { get; set; }

	/// <summary>
	/// The value of the setting in JSON format.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public virtual string Value { get; set; }

	[UpdateableAction(UpdateableAction.EverythingExceptSyncUpdate)]
	public string ValueType { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Get the type of the value.
	/// </summary>
	public virtual Type GetValueType()
	{
		if (TypeExtensions.TryGetType(ValueType, out var type))
		{
			return type;
		}
		
		Debugger.Break();
		return null;
	}

	/// <summary>
	/// Reset the setting back to default.
	/// </summary>
	public virtual void ResetToDefault()
	{
		Value = null;
		ResetHasChanges();
	}

	public void SetData<TData>(TData value)
	{
		Value = value.ToJson();
		ValueType = typeof(TData).ToAssemblyName();
	}

	#endregion
}

/// <summary>
/// Represents a setting.
/// </summary>
public interface ISetting : ISyncEntity
{
	#region Properties

	/// <summary>
	/// Set to mark this setting as a syncable setting.
	/// </summary>
	public bool CanSync { get; }

	/// <summary>
	/// The category for the settings.
	/// </summary>
	public string Category { get; set; }

	/// <summary>
	/// Optionally expires on value, DateTime.MinValue means there is no expiration.
	/// </summary>
	public DateTime ExpiresOn { get; set; }

	/// <summary>
	/// The name of the setting.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The value of the setting in JSON format.
	/// </summary>
	public string Value { get; set; }

	/// <summary>
	/// The full namespace of the type.
	/// </summary>
	public string ValueType { get; set; }

	#endregion
}