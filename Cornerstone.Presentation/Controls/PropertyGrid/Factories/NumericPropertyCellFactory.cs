#region References

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Convert;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class NumericPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		if (!IsNumericType(propertyDescriptor.PropertyType))
		{
			return null;
		}

		var control = new NumericUpDown();
		var attr = propertyDescriptor.GetCustomAttribute<RangeAttribute>();
		if (attr != null)
		{
			control.Minimum = attr.Minimum.ConvertTo<decimal>();
			control.Maximum = attr.Maximum.ConvertTo<decimal>();
		}

		if (IsIntegerType(propertyDescriptor.PropertyType))
		{
			control.Increment = 1;
		}
		else
		{
			control.Increment = 0.01m;
			control.FormatString = "{0:0.00}";
		}

		control.ValueChanged += (_, e) =>
			SetAndRaise(context, e.NewValue.ConvertTo(propertyDescriptor.PropertyType));

		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		var target = context.Target;
		var control = context.EditorControl;

		if (!IsNumericType(propertyDescriptor.PropertyType))
		{
			return false;
		}

		ValidateProperty(control, propertyDescriptor, target);

		if (control is not NumericUpDown numeric)
		{
			return false;
		}

		var value = propertyDescriptor.GetValue(target);
		if (value == null)
		{
			numeric.Value = 0;
			return true;
		}

		var dValue = (double) System.Convert.ChangeType(value, typeof(double));
		numeric.Value = decimal.TryParse(dValue.ToString(CultureInfo.InvariantCulture), out var d) ? d : 0;
		return true;
	}

	private static bool IsIntegerType(Type type)
	{
		return (type == typeof(sbyte))
			|| (type == typeof(byte))
			|| (type == typeof(short))
			|| (type == typeof(ushort))
			|| (type == typeof(int))
			|| (type == typeof(uint))
			|| (type == typeof(long))
			|| (type == typeof(ulong));
	}

	private static bool IsNumericType(Type type)
	{
		if (type == null)
		{
			return false;
		}

		return IsIntegerType(type)
			|| (type == typeof(float))
			|| (type == typeof(double))
			|| (type == typeof(decimal));
	}

	#endregion
}