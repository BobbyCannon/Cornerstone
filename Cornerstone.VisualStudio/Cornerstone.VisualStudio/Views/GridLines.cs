#region References

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

#endregion

namespace Cornerstone.VisualStudio.Views;

public sealed class GridLines : Control
{
	#region Fields

	private readonly Pen _pen;
	private readonly Pen _penBold;

	#endregion

	#region Constructors

	public GridLines()
	{
		_pen = new Pen(new SolidColorBrush(Color.FromArgb((byte) (255.0 * 0.1), 14, 94, 253)), 1);
		_penBold = new Pen(new SolidColorBrush(Color.FromArgb((byte) (255.0 * 0.3), 14, 94, 253)), 1);
	}

	#endregion

	#region Methods

	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);

		for (var i = 1; i < (ActualHeight / 10); i++)
		{
			drawingContext.DrawLine((i % 10) == 0 ? _penBold : _pen, new Point(0, i * 10), new Point(ActualWidth, i * 10));
		}

		for (var i = 1; i < (ActualWidth / 10); i++)
		{
			drawingContext.DrawLine((i % 10) == 0 ? _penBold : _pen, new Point(i * 10, 0), new Point(i * 10, ActualHeight));
		}
	}

	#endregion
}