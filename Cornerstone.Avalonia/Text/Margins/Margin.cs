#region References

using System;
using System.IO;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Cornerstone.Avalonia.Resources;

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

		try
		{
			using var stream = ResourceService.GetEmbeddedResource("Cornerstone.Avalonia.Resources.RightArrow.cur");

			if (stream != null)
			{
				using var bitmap = new Bitmap(stream);
				_rightArrowCursor = new Cursor(bitmap, new PixelPoint(12, 0));
			}
			else
			{
				_rightArrowCursor = new Cursor(StandardCursorType.Arrow);
			}
		}
		catch (InvalidOperationException)
		{
			return null;
		}

		return _rightArrowCursor;
	}

	#endregion
}