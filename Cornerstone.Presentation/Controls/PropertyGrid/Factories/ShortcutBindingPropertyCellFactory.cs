#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class ShortcutBindingPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		if (context.Property.PropertyType != typeof(ShortcutBinding))
		{
			return null;
		}

		var control = new ShortcutBox();
		var existing = context.Property.GetValue(context.Target) as ShortcutBinding;
		if (existing == null)
		{
			existing = new ShortcutBinding();
			SetAndRaise(context, existing);
		}

		control.Binding = existing;
		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		if (context.Property.PropertyType != typeof(ShortcutBinding))
		{
			return false;
		}

		if (context.EditorControl is not ShortcutBox box)
		{
			return false;
		}

		ValidateProperty(box, context.Property, context.Target);
		var value = context.Property.GetValue(context.Target) as ShortcutBinding;
		if (value == null)
		{
			value = new ShortcutBinding();
			SetAndRaise(context, value);
		}

		box.Binding = value;
		return true;
	}

	#endregion
}