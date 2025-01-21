#region References

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=net-9.0
/// </summary>
public abstract class PropertyCellFactory : Bindable
{
	#region Methods

	/// <summary>
	/// Handles the new property.
	/// </summary>
	/// <param name="context"> The context. </param>
	/// <returns> Control. </returns>
	public abstract Control HandleNewProperty(PropertyCellContext context);

	/// <summary>
	/// Handles the property changed.
	/// </summary>
	/// <param name="context"> The context. </param>
	/// <returns> True if handled otherwise false. </returns>
	public abstract bool HandlePropertyChanged(PropertyCellContext context);

	protected void SetAndRaise(PropertyCellContext context, object value)
	{
		try
		{
			context.Target?.SetMemberValue(context.Property.Name, value);
		}
		catch (Exception ex)
		{
			DataValidationErrors.SetErrors(context.EditorControl, [ex.Message]);
		}
	}

	/// <summary>
	/// Validates the property.
	/// </summary>
	/// <param name="sourceControl"> The source control. </param>
	/// <param name="propertyDescriptor"> The property descriptor. </param>
	/// <param name="component"> The component. </param>
	protected virtual void ValidateProperty(Control sourceControl, PropertyDescriptor propertyDescriptor, object component)
	{
		if (!ValidatorUtils.TryValidateProperty(component, propertyDescriptor, out var message))
		{
			DataValidationErrors.SetErrors(sourceControl, [message]);
		}
		else
		{
			DataValidationErrors.ClearErrors(sourceControl);
		}
	}

	#endregion
}