#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a setting.
/// </summary>
/// <typeparam name="T"> The type of the Value of the setting. </typeparam>
/// <typeparam name="T2"> The type of the Id of the setting. </typeparam>
public class Setting<T, T2> : Setting<T2>
{
	#region Fields

	private readonly T _defaultValue;

	#endregion

	#region Constructors

	/// <summary>
	/// Represents a setting.
	/// </summary>
	public Setting()
	{
	}

	/// <summary>
	/// Represents a setting.
	/// </summary>
	/// <param name="name"> The name of the setting. </param>
	/// <param name="defaultValue"> The default value for the setting. </param>
	public Setting(string name, T defaultValue)
	{
		_defaultValue = defaultValue;

		Name = name;
		Data = defaultValue;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The typed data of the value property.
	/// </summary>
	public T Data { get; set; }

	/// <inheritdoc />
	public override string Value
	{
		get => Data.ToRawJson();
		set => Data = value.FromJson<T>();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override Type GetValueType()
	{
		return typeof(T);
	}

	/// <inheritdoc />
	public override bool HasChanges(params string[] exclusions)
	{
		if (Data is ITrackPropertyChanges changeable && changeable.HasChanges())
		{
			return true;
		}

		return base.HasChanges(exclusions);
	}

	/// <inheritdoc />
	public override void ResetHasChanges()
	{
		if (Data is ITrackPropertyChanges changeable)
		{
			changeable.ResetHasChanges();
		}

		base.ResetHasChanges();
	}

	/// <inheritdoc />
	public override void ResetToDefault()
	{
		Data = _defaultValue;

		ResetHasChanges();
	}

	/// <summary>
	/// Update the Setting with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(Setting<T, T2> update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			Data = update.Data;
			Value = update.Value;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Data)), x => x.Data = update.Data);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Value)), x => x.Value = update.Value);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			Setting<T, T2> value => UpdateWith(value, options),
			Setting<T2> value => UpdateWith(value, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}

/// <summary>
/// Represents a setting.
/// </summary>
/// <typeparam name="T"> The type of the Value of the setting. </typeparam>
public class Setting<T> : SyncEntity<T>, ISetting
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
	public override T Id { get; set; }

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
			response.AddRange(nameof(Category), nameof(ExpiresOn), nameof(Name), nameof(Value));
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
		Value = default;
		ResetHasChanges();
	}

	/// <summary>
	/// Update the Setting with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(Setting<T> update)
	{
		return UpdateWith(update, UpdateableOptions.Empty);
	}

	/// <summary>
	/// Update the Setting with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(Setting<T> update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			CanSync = update.CanSync;
			Category = update.Category;
			CreatedOn = update.CreatedOn;
			ExpiresOn = update.ExpiresOn;
			Id = update.Id;
			IsDeleted = update.IsDeleted;
			ModifiedOn = update.ModifiedOn;
			Name = update.Name;
			SyncId = update.SyncId;
			Value = update.Value;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(CanSync)), x => x.CanSync = update.CanSync);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Category)), x => x.Category = update.Category);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(CreatedOn)), x => x.CreatedOn = update.CreatedOn);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(ExpiresOn)), x => x.ExpiresOn = update.ExpiresOn);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Id)), x => x.Id = update.Id);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(IsDeleted)), x => x.IsDeleted = update.IsDeleted);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(ModifiedOn)), x => x.ModifiedOn = update.ModifiedOn);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Name)), x => x.Name = update.Name);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SyncId)), x => x.SyncId = update.SyncId);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Value)), x => x.Value = update.Value);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			Setting<T> value => UpdateWith(value, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}

/// <summary>
/// Represents a setting.
/// </summary>
public interface ISetting : IModifiableEntity
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