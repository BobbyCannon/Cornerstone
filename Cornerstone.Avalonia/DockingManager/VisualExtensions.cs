#region References

using Avalonia;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

internal static class VisualExtensions
{
	#region Methods

	public static Rect GetBoundsOf(this Visual self, Visual visual)
	{
		var topLeft = visual.TranslatePoint(new Point(0, 0), self)!.Value;
		var bottomRight = visual.TranslatePoint(new Point(visual.Bounds.Width, visual.Bounds.Height), self)!.Value;
		return new Rect(topLeft, bottomRight);
	}

	#endregion
}