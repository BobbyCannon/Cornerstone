#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class DateTimePropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		if (!IsSupported(context.Property.PropertyType))
		{
			return null;
		}

		var control = new DatePicker();
		control.SelectedDateChanged += (_, args) => ApplySelectedDate(context, args.NewDate);
		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		if (!IsSupported(context.Property.PropertyType))
		{
			return false;
		}

		if (context.EditorControl is not DatePicker picker)
		{
			return false;
		}

		ValidateProperty(picker, context.Property, context.Target);
		picker.SelectedDate = ToDateTimeOffset(context.Property.GetValue(context.Target));
		return true;
	}

	private void ApplySelectedDate(PropertyCellContext context, DateTimeOffset? selected)
	{
		var type = context.Property.PropertyType;
		var current = context.Property.GetValue(context.Target);

		if ((type == typeof(DateTime?)) && (selected == null))
		{
			SetAndRaise(context, null);
			return;
		}

		if ((type == typeof(DateTimeOffset?)) && (selected == null))
		{
			SetAndRaise(context, null);
			return;
		}

		if (selected == null)
		{
			return;
		}

		var time = TimeSpan.Zero;
		var offset = selected.Value.Offset;
		if (current is DateTime dateTime)
		{
			time = dateTime.TimeOfDay;
		}
		else if (current is DateTimeOffset dateTimeOffset)
		{
			time = dateTimeOffset.TimeOfDay;
			offset = dateTimeOffset.Offset;
		}

		var date = selected.Value.Date.Add(time);
		if ((type == typeof(DateTimeOffset)) || (type == typeof(DateTimeOffset?)))
		{
			SetAndRaise(context, new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Unspecified), offset));
			return;
		}

		SetAndRaise(context, date);
	}

	private static bool IsSupported(Type type)
	{
		return (type == typeof(DateTime))
			|| (type == typeof(DateTime?))
			|| (type == typeof(DateTimeOffset))
			|| (type == typeof(DateTimeOffset?));
	}

	private static DateTimeOffset? ToDateTimeOffset(object value)
	{
		return value switch
		{
			DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), TimeSpan.Zero),
			DateTimeOffset dateTimeOffset => dateTimeOffset,
			_ => null
		};
	}

	#endregion
}