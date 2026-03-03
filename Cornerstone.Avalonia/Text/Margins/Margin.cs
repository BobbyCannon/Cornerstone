#region References

using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

#endregion

namespace Cornerstone.Avalonia.Text.Margins;

public class Margin : CornerstoneControl
{
	#region Fields

	private static Cursor _rightArrowCursor;

	#endregion

	#region Methods

	public static Cursor GetRightArrowCursor()
	{
		if (_rightArrowCursor != null)
		{
			return _rightArrowCursor;
		}

		using var stream = AssetLoader.Open(new Uri("avares://Cornerstone.Avalonia/Resources/RightArrow.cur"));
		using var bitmap = new Bitmap(stream);

		return _rightArrowCursor = new Cursor(bitmap, new PixelPoint(12, 0));
	}

	#endregion
}