#region References

using Company.AppName.Keystone;
using Company.AppName.Views;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Theme;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

public class App : Application<AppKeystone>
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
				desktop.MainWindow = new MainWindow(Keystone.ViewModel);
				break;
			}
			case ISingleViewApplicationLifetime singleViewPlatform:
			{
				singleViewPlatform.MainView = new MainView
				{
					DataContext = Keystone.ViewModel
				};
				break;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	public override void RegisterServices()
	{
		base.RegisterServices();
		RegisterServices(AppBootstrap.DependencyProvider, Design.IsDesignMode);
	}

	public static void RegisterServices(DependencyProvider dependencyProvider, bool designOrUnitTesting)
	{
		dependencyProvider.AddSingleton<AppState>();
		dependencyProvider.AddSingleton<AppBus>();
		dependencyProvider.AddSingleton<AppEngine>();
		dependencyProvider.AddSingleton<AppKeystone>();
		dependencyProvider.AddSingleton<AppViewModel>();
		dependencyProvider.AddSingleton<IAppNavigator, AppViewModel>();
		dependencyProvider.AddSingleton<IAppDispatcher, AppViewModel>();
	}

	#endregion
}
