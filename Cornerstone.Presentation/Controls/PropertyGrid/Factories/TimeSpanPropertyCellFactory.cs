#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class TimeSpanPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		if (!IsSupported(context.Property.PropertyType))
		{
			return null;
		}

		var editor = new TimeSpanDurationEditor();
		editor.ValueChanged += () =>
		{
			var value = editor.GetValue();
			if (context.Property.PropertyType == typeof(TimeSpan?))
			{
				SetAndRaise(context, value);
				return;
			}

			SetAndRaise(context, value ?? TimeSpan.Zero);
		};

		return editor;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		if (!IsSupported(context.Property.PropertyType))
		{
			return false;
		}

		if (context.EditorControl is not TimeSpanDurationEditor editor)
		{
			return false;
		}

		ValidateProperty(editor, context.Property, context.Target);
		editor.SetValue(context.Property.GetValue(context.Target) as TimeSpan?);
		return true;
	}

	private static bool IsSupported(Type type)
	{
		return (type == typeof(TimeSpan)) || (type == typeof(TimeSpan?));
	}

	#endregion

	#region Classes

	internal sealed class TimeSpanDurationEditor : StackPanel
	{
		public TimeSpanDurationEditor()
		{
			Orientation = Orientation.Horizontal;
			Spacing = 4;

			Days = CreatePart(0, 99999);
			Hours = CreatePart(0, 23);
			Minutes = CreatePart(0, 59);
			Seconds = CreatePart(0, 59);

			Children.Add(Days);
			Children.Add(Hours);
			Children.Add(Minutes);
			Children.Add(Seconds);
		}

		public event Action ValueChanged;

		public NumericUpDown Days { get; }

		public NumericUpDown Hours { get; }

		public NumericUpDown Minutes { get; }

		public NumericUpDown Seconds { get; }

		public TimeSpan? GetValue()
		{
			return new TimeSpan(
				ToInt(Days.Value),
				ToInt(Hours.Value),
				ToInt(Minutes.Value),
				ToInt(Seconds.Value));
		}

		public void SetValue(TimeSpan? value)
		{
			var span = value ?? TimeSpan.Zero;
			Days.Value = span.Days;
			Hours.Value = span.Hours;
			Minutes.Value = span.Minutes;
			Seconds.Value = span.Seconds;
		}

		private NumericUpDown CreatePart(int minimum, int maximum)
		{
			var control = new NumericUpDown
			{
				Minimum = minimum,
				Maximum = maximum,
				Increment = 1,
				Width = 56,
				FormatString = "0"
			};
			control.ValueChanged += (_, _) => ValueChanged?.Invoke();
			return control;
		}

		private static int ToInt(decimal? value)
		{
			return (int) (value ?? 0);
		}
	}

	#endregion
}