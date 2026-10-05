#region References

using System;
using System.IO;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls;

public class Margin : Control
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
			using var stream = typeof(Margin).Assembly.GetManifestResourceStream("Cornerstone.Presentation.Controls.Text.Resources.RightArrow.cur");

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