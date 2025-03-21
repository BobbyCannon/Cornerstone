#region References

using System;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// A value of a partial update.
/// </summary>
public class PartialUpdateValue : Notifiable, IUpdateable<PartialUpdateValue>
{
	#region Constructors

	/// <summary>
	/// Initializes a partial update value.
	/// </summary>
	public PartialUpdateValue()
	{
	}

	/// <summary>
	/// Initializes a partial update value.
	/// </summary>
	/// <param name="name"> The property name of the update. </param>
	/// <param name="value"> The value to set the property to. </param>
	public PartialUpdateValue(string name, object value)
		: this(name, value.GetType(), value)
	{
	}

	/// <summary>
	/// Initializes a partial update value.
	/// </summary>
	/// <param name="name"> The property name of the update. </param>
	/// <param name="type"> The type for the property. </param>
	/// <param name="value"> The value to set the property to. </param>
	public PartialUpdateValue(string name, Type type, object value)
	{
		Name = name;
		Type = type;
		Value = value;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The name of the member for the update.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The type of the property.
	/// </summary>
	public Type Type { get; set; }

	/// <summary>
	/// The value of the member.
	/// </summary>
	public object Value { get; set; }

	#endregion

	#region Methods

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return base.HasChanges(settings)
			|| (Value is ITrackPropertyChanges pValue && pValue.HasChanges());
	}

	public override void ResetHasChanges()
	{
		if (Value is ITrackPropertyChanges pValue)
		{
			pValue.ResetHasChanges();
		}
		base.ResetHasChanges();
	}

	/// <summary>
	/// Update the PartialUpdateValue with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(PartialUpdateValue update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith - PartialUpdateValue

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(Name, update.Name, settings.ShouldProcessProperty(nameof(Name)), x => Name = x);
		UpdateProperty(Type, update.Type, settings.ShouldProcessProperty(nameof(Type)), x => Type = x);
		UpdateProperty(Value, update.Value, settings.ShouldProcessProperty(nameof(Value)), x => Value = x);

		// Code Generated - /UpdateWith - PartialUpdateValue

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			PartialUpdateValue value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}