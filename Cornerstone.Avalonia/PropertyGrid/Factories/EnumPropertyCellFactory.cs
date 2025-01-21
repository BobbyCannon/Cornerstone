#region References

using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid.Factories;

public class EnumPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;

		if (!propertyDescriptor.PropertyType.IsEnum)
		{
			return null;
		}

		var isFlags = propertyDescriptor.PropertyType.IsDefined<FlagsAttribute>();
		//Enum value = propertyDescriptor.GetValue(target) as Enum;

		if (isFlags)
		{
			var control = new ComboBox
			{
				ItemsSource = propertyDescriptor.PropertyType.GetFlagValues(),
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			control.SelectionChanged += (s, e) => SetAndRaise(context, control.SelectedItem);

			return control;
		}
		else
		{
			var control = new ComboBox
			{
				ItemsSource = EnumExtensions.GetEnumValues(propertyDescriptor.PropertyType),
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			control.SelectionChanged += (s, e) => SetAndRaise(context, control.SelectedItem);

			return control;
		}
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		var target = context.Target;
		var control = context.EditorControl!;

		if (!propertyDescriptor.PropertyType.IsEnum)
		{
			return false;
		}

		ValidateProperty(control, propertyDescriptor, target);

		if (control is ComboBox cb)
		{
			var value = propertyDescriptor.GetValue(target) as Enum;
			cb.SelectedItem = value;
			return true;
		}

		return false;
	}

	#endregion
}