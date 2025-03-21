#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a setting.
/// </summary>
/// <typeparam name="TKey"> The type of the Value of the setting. </typeparam>
/// <typeparam name="TModel"> The type of the sync model. </typeparam>
public class Setting<TKey, TModel>
	: Setting<TKey>
	where TModel : SyncModel
{
	#region Methods

	/// <inheritdoc />
	public override HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
	{
		var response = base.GetDefaultIncludedProperties(action);

		switch (action)
		{
			case UpdateableAction.SyncIncomingAdd:
			case UpdateableAction.SyncIncomingUpdate:
			case UpdateableAction.SyncOutgoing:
			{
				var syncProperties = typeof(TModel)
					.GetCachedProperties()
					.Select(x => x.Name)
					.ToList();
				response.Add(syncProperties);
				break;
			}
		}

		return response;
	}

	#endregion
}

/// <summary>
/// Represents a setting.
/// </summary>
/// <typeparam name="TKey"> The type of the Value of the setting. </typeparam>
public class Setting<TKey>
	: SyncEntity<TKey>, ISetting
{
	#region Properties

	/// <summary>
	/// Set to mark this setting as a syncable setting.
	/// </summary>
	public bool CanSync { get; set; }

	/// <summary>
	/// The category for the settings.
	/// </summary>
	public string Category { get; set; }

	/// <summary>
	/// Optionally expires on value, DateTime.MinValue means there is no expiration.
	/// </summary>
	public DateTime ExpiresOn { get; set; }

	/// <inheritdoc />
	public override TKey Id { get; set; }

	/// <summary>
	/// The name of the setting.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The value of the setting in JSON format.
	/// </summary>
	public virtual string Value { get; set; }

	/// <inheritdoc />
	public string ValueType { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
	{
		var response = base.GetDefaultIncludedProperties(action);

		if (action.IsSyncAction())
		{
			// Properties should be included in every sync (incoming add / modify or outgoing).
			response.AddRange(nameof(Category), nameof(ExpiresOn), nameof(Name), nameof(Value), nameof(ValueType));
		}

		switch (action)
		{
			case UpdateableAction.UnwrapProxyEntity:
			{
				response.AddRange(nameof(Value));
				break;
			}
		}

		return response;
	}

	/// <summary>
	/// Get the type of the value.
	/// </summary>
	public virtual Type GetValueType()
	{
		return Type.GetType(ValueType);
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
		Value = value.ToRawJson();
		ValueType = typeof(TData).ToAssemblyName();
	}

	/// <summary>
	/// Update the Setting with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(Setting<TKey> update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <summary>
	/// Update the Setting with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(Setting<TKey> update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(CanSync, update.CanSync, settings.ShouldProcessProperty(nameof(CanSync)), x => CanSync = x);
		UpdateProperty(Category, update.Category, settings.ShouldProcessProperty(nameof(Category)), x => Category = x);
		UpdateProperty(CreatedOn, update.CreatedOn, settings.ShouldProcessProperty(nameof(CreatedOn)), x => CreatedOn = x);
		UpdateProperty(ExpiresOn, update.ExpiresOn, settings.ShouldProcessProperty(nameof(ExpiresOn)), x => ExpiresOn = x);
		UpdateProperty(Id, update.Id, settings.ShouldProcessProperty(nameof(Id)), x => Id = x);
		UpdateProperty(IsDeleted, update.IsDeleted, settings.ShouldProcessProperty(nameof(IsDeleted)), x => IsDeleted = x);
		UpdateProperty(ModifiedOn, update.ModifiedOn, settings.ShouldProcessProperty(nameof(ModifiedOn)), x => ModifiedOn = x);
		UpdateProperty(Name, update.Name, settings.ShouldProcessProperty(nameof(Name)), x => Name = x);
		UpdateProperty(SyncId, update.SyncId, settings.ShouldProcessProperty(nameof(SyncId)), x => SyncId = x);
		UpdateProperty(Value, update.Value, settings.ShouldProcessProperty(nameof(Value)), x => Value = x);
		UpdateProperty(ValueType, update.ValueType, settings.ShouldProcessProperty(nameof(ValueType)), x => ValueType = x);

		// Code Generated - /UpdateWith

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			Setting<TKey> value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
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