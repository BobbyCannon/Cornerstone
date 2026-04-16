#region References

using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class InkCanvasStroke
{
	#region Properties

	public IBrush Brush { get; set; }

	public string Id { get; set; }

	public List<Point> Points { get; set; }

	#endregion
}