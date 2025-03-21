#region References

using System.Diagnostics;
using Avalonia.Controls;
using Cornerstone.Avalonia.PropertyGrid.Descriptors;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid.Factories;

public class BooleanPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;

		if ((propertyDescriptor.PropertyType != typeof(bool))
			&& (propertyDescriptor.PropertyType != typeof(bool?)))
		{
			return null;
		}

		var control = new CheckBox
		{
			IsThreeState = (propertyDescriptor.PropertyType == typeof(bool?))
				|| propertyDescriptor is MultiObjectPropertyDescriptor
		};

		// todo: add weak event.
		control.IsCheckedChanged += (s, e) => { SetAndRaise(context, control.IsChecked); };

		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		var target = context.Target;
		var control = context.EditorControl;

		if ((propertyDescriptor.PropertyType != typeof(bool)) && (propertyDescriptor.PropertyType != typeof(bool?)))
		{
			return false;
		}

		Debug.Assert(control != null);

		ValidateProperty(control, propertyDescriptor, target);

		if (control is CheckBox checkBox)
		{
			if (checkBox.IsThreeState)
			{
				var obj = propertyDescriptor.GetValue(target);

				if (obj != null)
				{
					checkBox.IsChecked = (bool) obj;
				}
				else
				{
					checkBox.IsChecked = null;
				}
			}
			else
			{
				checkBox.IsChecked = (bool) propertyDescriptor.GetValue(target)!;
			}

			return true;
		}

		return false;
	}

	#endregion
}