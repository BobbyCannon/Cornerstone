#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Media;

#endregion

namespace Cornerstone.Presentation.Controls.Ink;

public class InkCanvasStroke
{
	#region Properties

	public IBrush Brush { get; set; }

	public string Id { get; set; }

	public List<Point> Points { get; set; }

	#endregion
}