#region References

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Avalonia.Controls;
using Cornerstone.Attributes;
using Cornerstone.Convert;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid.Factories;

public class NumericPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	/// <inheritdoc />
	public override Control HandleNewProperty(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		if (!propertyDescriptor.PropertyType.IsNumericType())
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
		
		var formatAttr = propertyDescriptor.GetCustomAttribute<FormatStringAttribute>();

		if ((propertyDescriptor.PropertyType == typeof(sbyte)) ||
			(propertyDescriptor.PropertyType == typeof(byte)) ||
			(propertyDescriptor.PropertyType == typeof(short)) ||
			(propertyDescriptor.PropertyType == typeof(ushort)) ||
			(propertyDescriptor.PropertyType == typeof(int)) ||
			(propertyDescriptor.PropertyType == typeof(uint)) ||
			(propertyDescriptor.PropertyType == typeof(long)) ||
			(propertyDescriptor.PropertyType == typeof(ulong))
			)
		{
			var incrementAttr = propertyDescriptor.GetCustomAttribute<IntegerChangeAttribute>();

			control.Increment = incrementAttr?.SmallChange ?? 1;

			if (formatAttr != null)
			{
				control.FormatString = formatAttr.ToString()!;
			}
		}
		else
		{
			var precisionAttr = propertyDescriptor.GetCustomAttribute<DecimalPrecisionAttribute>();
			if (precisionAttr != null)
			{
				control.Increment = precisionAttr.Increment;
				control.FormatString = precisionAttr.FormatString;
			}
			else
			{
				control.Increment = 0.01m;
				control.FormatString = "{0:0.00}";

				if (formatAttr != null)
				{
					control.FormatString = formatAttr.ToString()!;
				}
			}
		}

		control.ValueChanged += (_, e) => SetAndRaise(context, e.NewValue.ConvertTo(propertyDescriptor.PropertyType));

		return control;
	}

	/// <summary>
	/// Handles the property changed.
	/// </summary>
	/// <param name="context"> The context. </param>
	/// <returns> True if success, <c> false </c> otherwise. </returns>
	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		var target = context.Target;
		var control = context.EditorControl;

		if (!propertyDescriptor.PropertyType.IsNumericType())
		{
			return false;
		}

		ValidateProperty(control, propertyDescriptor, target);

		if (control is not NumericUpDown nup)
		{
			return false;
		}

		var value = propertyDescriptor.GetValue(target);
		var dValue = (double) System.Convert.ChangeType(value!, typeof(double));

		if (decimal.TryParse(dValue.ToString(CultureInfo.InvariantCulture), out var d))
		{
			nup.Value = d;
		}
		else
		{
			nup.Value = null;
		}

		return true;
	}

	#endregion
}