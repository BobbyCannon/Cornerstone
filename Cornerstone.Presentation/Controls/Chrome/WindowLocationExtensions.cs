#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.Controls.Chrome;

public static class WindowLocationExtensions
{
	#region Methods

	public static void CaptureWindowLocation(this Window window, WindowLocation target)
	{
		if ((window == null) || (target == null))
		{
			return;
		}

		// Minimized windows report off-screen coordinates (for example -32000,-32000 on Windows).
		if (window.WindowState == WindowState.Minimized)
		{
			return;
		}

		target.UpdateWith(window.GetWindowLocation());
	}

	public static WindowLocation GetWindowLocation(this Window window)
	{
		return new WindowLocation
		{
			Top = window.Position.Y,
			Left = window.Position.X,
			Height = (int) window.Height,
			Width = (int) window.Width,
			Maximized = window.WindowState == WindowState.Maximized
		};
	}

	public static void RestoreWindowLocation(this Window window, WindowLocation location)
	{
		if (location == null)
		{
			return;
		}

		if (location.Height > int.MinValue)
		{
			window.Height = location.Height;
		}

		if (location.Width > int.MinValue)
		{
			window.Width = location.Width;
		}

		if ((location.Top == -1) && (location.Left == -1))
		{
			window.CenterOnScreen();
		}
		else
		{
			window.Position = new PixelPoint(location.Left, location.Top);
		}

		if (!window.IsOnScreen())
		{
			window.CenterOnScreen();
		}

		if (location.Maximized)
		{
			Threading.Dispatcher.UIThread.Post(() => window.WindowState = WindowState.Maximized);
		}
	}

	#endregion
}
