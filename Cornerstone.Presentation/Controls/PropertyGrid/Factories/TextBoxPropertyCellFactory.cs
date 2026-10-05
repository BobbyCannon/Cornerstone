#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class TextBoxPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		if (context.Property.PropertyType != typeof(string))
		{
			return null;
		}

		var control = new TextBox();
		control.LostFocus += (_, _) => SetAndRaise(context, control.Text);
		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		if (propertyDescriptor.PropertyType != typeof(string))
		{
			return false;
		}

		if (context.EditorControl is not TextBox textBox)
		{
			return false;
		}

		ValidateProperty(textBox, propertyDescriptor, context.Target);
		var value = propertyDescriptor.GetValue(context.Target);
		textBox.Text = value?.ToString() ?? string.Empty;
		return true;
	}

	#endregion
}