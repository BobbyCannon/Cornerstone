#region References

using Avalonia;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Represents the layer that renders the text in a hex view.
/// </summary>
public class TextLayer : Layer
{
	#region Methods

	/// <inheritdoc />
	public override void Render(DrawingContext context)
	{
		base.Render(context);

		if (HexView is null)
		{
			return;
		}

		double currentY = 0;
		for (var i = 0; i < HexView.VisualLines.Count; i++)
		{
			var line = HexView.VisualLines[i];
			foreach (var column in HexView.Columns)
			{
				if (column.IsVisible)
				{
					line.ColumnTextLines[column.Index]?.Draw(context, new Point(column.Bounds.Left, currentY));
				}
			}

			currentY += line.Bounds.Height;
		}
	}

	#endregion
}