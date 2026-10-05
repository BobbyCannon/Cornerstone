#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class ColorPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		if (!IsSupported(context.Property.PropertyType))
		{
			return null;
		}

		var editor = new ColorValueEditor();
		editor.Hex.LostFocus += (_, _) =>
		{
			if (!Color.TryParse(editor.Hex.Text, out var color))
			{
				return;
			}

			editor.Swatch.Background = new SolidColorBrush(color);
			SetAndRaise(context, ToPropertyValue(context.Property.PropertyType, color));
		};

		return editor;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		if (!IsSupported(context.Property.PropertyType))
		{
			return false;
		}

		if (context.EditorControl is not ColorValueEditor editor)
		{
			return false;
		}

		ValidateProperty(editor, context.Property, context.Target);
		var color = ToColor(context.Property.GetValue(context.Target));
		editor.Swatch.Background = new SolidColorBrush(color);
		editor.Hex.Text = color.ToString();
		return true;
	}

	private static bool IsSupported(Type type)
	{
		return (type == typeof(Color))
			|| (type == typeof(Color?))
			|| typeof(IBrush).IsAssignableFrom(type);
	}

	private static Color ToColor(object value)
	{
		return value switch
		{
			Color color => color,
			SolidColorBrush brush => brush.Color,
			IBrush => Colors.Transparent,
			_ => Colors.Transparent
		};
	}

	private static object ToPropertyValue(Type type, Color color)
	{
		if ((type == typeof(Color)) || (type == typeof(Color?)))
		{
			return color;
		}

		return new SolidColorBrush(color);
	}

	#endregion

	#region Classes

	internal sealed class ColorValueEditor : DockPanel
	{
		public ColorValueEditor()
		{
			Swatch = new Border
			{
				Width = 20,
				Height = 20,
				Margin = new Thickness(0, 0, 6, 0),
				CornerRadius = new CornerRadius(3),
				BorderThickness = new Thickness(1),
				VerticalAlignment = VerticalAlignment.Center
			};
			Hex = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch };
			SetDock(Swatch, Dock.Left);
			Children.Add(Swatch);
			Children.Add(Hex);
		}

		public TextBox Hex { get; }

		public Border Swatch { get; }
	}

	#endregion
}