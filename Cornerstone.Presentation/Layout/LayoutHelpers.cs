#region References

#endregion

namespace Cornerstone.Presentation.Layout;

public static class LayoutHelpers
{
	#region Methods

	public static double GetBestSingle(CornerRadius t)
	{
		if (t.IsUniform)
		{
			return t.TopLeft;
		}

		return (t.TopLeft + t.TopRight + t.BottomLeft + t.BottomRight) / 4.0;
	}

	public static double GetBestSingle(Thickness t)
	{
		if (t.IsUniform)
		{
			return t.Left;
		}

		return (t.Left + t.Top + t.Right + t.Bottom) / 4.0;
	}

	#endregion
}