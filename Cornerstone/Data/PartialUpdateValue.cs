#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// A value of a partial update.
/// </summary>
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[SourceReflection]
public partial class PartialUpdateValue : CornerstoneObject, IUpdateable<PartialUpdateValue>
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
	public partial string Name { get; set; }

	/// <summary>
	/// The type of the property.
	/// </summary>
	public partial Type Type { get; set; }

	/// <summary>
	/// The value of the member.
	/// </summary>
	public partial object Value { get; set; }

	#endregion

	#region Methods

	public object GetValue()
	{
		return Value.TryConvertTo(Type, out var result)
			? result
			: SourceReflector.CreateInstance(Type);
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return base.HasChanges(settings)
			|| (Value is ITrackPropertyChanges pValue 
				&& pValue.HasChanges());
	}

	public override void ResetHasChanges()
	{
		if (Value is ITrackPropertyChanges pValue)
		{
			pValue.ResetHasChanges();
		}
		base.ResetHasChanges();
	}

	#endregion
}