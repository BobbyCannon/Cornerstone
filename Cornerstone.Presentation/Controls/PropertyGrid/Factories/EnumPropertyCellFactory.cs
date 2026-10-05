#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class EnumPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "PropertyDescriptor.PropertyType cannot flow DynamicallyAccessedMembers; enum values come from generated source reflection.")]
	public override Control HandleNewProperty(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		if (!propertyDescriptor.PropertyType.IsEnum)
		{
			return null;
		}

		var isFlags = Attribute.IsDefined(propertyDescriptor.PropertyType, typeof(FlagsAttribute));
		var control = new ComboBox
		{
			ItemsSource = isFlags
				? propertyDescriptor.PropertyType.GetFlagValues()
				: SourceReflector.GetEnumValues(propertyDescriptor.PropertyType),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};

		control.SelectionChanged += (_, _) => SetAndRaise(context, control.SelectedItem);
		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		var target = context.Target;
		var control = context.EditorControl;

		if (!propertyDescriptor.PropertyType.IsEnum)
		{
			return false;
		}

		ValidateProperty(control, propertyDescriptor, target);

		if (control is ComboBox comboBox)
		{
			comboBox.SelectedItem = propertyDescriptor.GetValue(target) as Enum;
			return true;
		}

		return false;
	}

	#endregion
}