#region References

using System;
using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Data;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid;

public abstract class PropertyCellFactory : CornerstoneObject
{
	#region Methods

	public abstract Control HandleNewProperty(PropertyCellContext context);

	public abstract bool HandlePropertyChanged(PropertyCellContext context);

	protected void SetAndRaise(PropertyCellContext context, object value)
	{
		try
		{
			context.Property.SetValue(context.Target, value);
		}
		catch (Exception ex)
		{
			DataValidationErrors.SetErrors(context.EditorControl, [ex.Message]);
		}
	}

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