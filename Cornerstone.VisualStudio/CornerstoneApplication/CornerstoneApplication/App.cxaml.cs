#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Markup.Xaml;
using CornerstoneApplication.ViewModels;
using CornerstoneApplication.Views;

#endregion

namespace CornerstoneApplication;

public class App : global::Cornerstone.Presentation.Application
{
	#region Methods

	public override void Initialize()
	{
		CornerstoneXamlLoader.Load(this);
		base.Initialize();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				desktop.MainWindow = new MainWindow
				{
					DataContext = new MainViewModel()
				};
				break;
			}
			case ISingleViewApplicationLifetime singleViewPlatform:
			{
				singleViewPlatform.MainView = new MainView
				{
					DataContext = new MainViewModel()
				};
				break;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	#endregion
}
