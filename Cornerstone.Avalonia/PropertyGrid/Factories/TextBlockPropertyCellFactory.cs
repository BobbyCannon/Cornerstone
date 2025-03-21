#region References

using Avalonia.Controls;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid.Factories;

public class TextBlockPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		var control = new TextBlock
		{
			TextWrapping = TextWrapping.Wrap
		};
		return control;
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		var propertyDescriptor = context.Property;
		var target = context.Target;
		var control = context.EditorControl!;

		if (control is not TextBlock textBlock)
		{
			return false;
		}

		var propertyValue = propertyDescriptor.GetValue(target);
		textBlock.Text = propertyValue?.ToString() ?? string.Empty;
		return true;
	}

	#endregion
}