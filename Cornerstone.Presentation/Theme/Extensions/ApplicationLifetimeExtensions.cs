#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Theme.Extensions;

public static class ApplicationLifetimeExtensions
{
	#region Methods

	public static void BringToFront(this Application application)
	{
		var mainWindow = application?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
			? desktop.MainWindow
			: null;

		if (mainWindow == null)
		{
			return;
		}

		application.Dispatcher.Post(() =>
		{
			if (mainWindow.WindowState == WindowState.Minimized)
			{
				mainWindow.WindowState = WindowState.Normal;
			}

			mainWindow.Activate();
			mainWindow.BringIntoView();
		});
	}

	#endregion
}
