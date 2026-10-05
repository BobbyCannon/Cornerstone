#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class GuidPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		if (!IsSupported(context.Property.PropertyType))
		{
			return null;
		}

		var control = new TextBox();
		control.LostFocus += (_, _) =>
		{
			var text = control.Text;
			if (string.IsNullOrWhiteSpace(text))
			{
				SetAndRaise(context, context.Property.PropertyType == typeof(Guid?) ? null : Guid.Empty);
				return;
			}

			if (Guid.TryParse(text, out var guid))
			{
				SetAndRaise(context, guid);
			}
		};

		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		if (!IsSupported(context.Property.PropertyType))
		{
			return false;
		}

		if (context.EditorControl is not TextBox textBox)
		{
			return false;
		}

		ValidateProperty(textBox, context.Property, context.Target);
		var value = context.Property.GetValue(context.Target);
		textBox.Text = value?.ToString() ?? string.Empty;
		return true;
	}

	private static bool IsSupported(Type type)
	{
		return (type == typeof(Guid)) || (type == typeof(Guid?));
	}

	#endregion
}