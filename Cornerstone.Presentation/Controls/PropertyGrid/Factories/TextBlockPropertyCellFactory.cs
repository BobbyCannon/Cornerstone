#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid.Factories;

public class TextBlockPropertyCellFactory : PropertyCellFactory
{
	#region Methods

	public override Control HandleNewProperty(PropertyCellContext context)
	{
		return new TextBlock { TextWrapping = TextWrapping.Wrap };
	}

	public override bool HandlePropertyChanged(PropertyCellContext context)
	{
		if (context.EditorControl is not TextBlock textBlock)
		{
			return false;
		}

		var propertyValue = context.Property.GetValue(context.Target);
		textBlock.Text = propertyValue?.ToString() ?? string.Empty;
		return true;
	}

	#endregion
}