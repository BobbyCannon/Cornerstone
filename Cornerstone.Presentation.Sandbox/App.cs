#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Media;

#endregion

namespace Cornerstone.Presentation.Sandbox;

public class App : Application
{
	#region Methods

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new Window
			{
				Title = "Cornerstone.Presentation sandbox",
				Width = 640,
				Height = 400,
				Content = new TextBlock
				{
					Text = "Code-only window on Cornerstone.Presentation (Win32 + Skia + HarfBuzz). Unstyled until CornerstoneTheme is applied.",
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(16)
				}
			};
		}

		base.OnFrameworkInitializationCompleted();
	}

	#endregion
}