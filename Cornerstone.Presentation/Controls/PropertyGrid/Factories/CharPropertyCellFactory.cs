#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class CharPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		if (context.Property.PropertyType != typeof(char))
		{
			return null;
		}

		var control = new ComboBox
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			ItemsSource = CreateChoices((char) (context.Property.GetValue(context.Target) ?? '\0'))
		};

		control.SelectionChanged += (_, _) =>
		{
			if (control.SelectedItem is CharChoice choice)
			{
				SetAndRaise(context, choice.Value);
			}
		};

		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		if (context.Property.PropertyType != typeof(char))
		{
			return false;
		}

		if (context.EditorControl is not ComboBox comboBox)
		{
			return false;
		}

		ValidateProperty(comboBox, context.Property, context.Target);
		var value = (char) context.Property.GetValue(context.Target);
		comboBox.ItemsSource = CreateChoices(value);
		comboBox.SelectedItem = FindChoice(comboBox.ItemsSource, value);
		return true;
	}

	private static List<CharChoice> CreateChoices(char current)
	{
		var choices = new List<CharChoice>
		{
			new('\t', "Tab"),
			new(' ', "Space")
		};

		if ((current != '\t') && (current != ' ') && (current != '\0'))
		{
			choices.Add(new CharChoice(current, current.ToString()));
		}

		return choices;
	}

	private static CharChoice FindChoice(object itemsSource, char value)
	{
		if (itemsSource is IEnumerable<CharChoice> choices)
		{
			foreach (var choice in choices)
			{
				if (choice.Value == value)
				{
					return choice;
				}
			}
		}

		return null;
	}

	#endregion

	#region Classes

	public sealed class CharChoice
	{
		public CharChoice(char value, string name)
		{
			Value = value;
			Name = name;
		}

		public string Name { get; }

		public char Value { get; }

		public override string ToString()
		{
			return Name;
		}
	}

	#endregion
}