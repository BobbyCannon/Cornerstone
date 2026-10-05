#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Theme;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

public class App : Application
{
	#region Methods

	public override void Initialize()
	{
		CornerstoneXamlLoader.Load(this);
		base.Initialize();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new AppWindow(AppBootstrap.GetInstance<AppViewModel>());
		}

		base.OnFrameworkInitializationCompleted();
	}

	public override void RegisterServices()
	{
		// Base ensures AppBootstrap (design-time) and registers the UI dispatcher.
		base.RegisterServices();
		RegisterServices(AppBootstrap.DependencyProvider, Design.IsDesignMode);
	}

	public static void RegisterServices(DependencyProvider dependencyProvider, bool designOrUnitTesting)
	{
		dependencyProvider.AddSingleton<AppViewModel>();
		dependencyProvider.AddSingleton<IAppNavigator, AppViewModel>();
		dependencyProvider.AddSingleton<IAppDispatcher, AppViewModel>();
	}

	#endregion
}
