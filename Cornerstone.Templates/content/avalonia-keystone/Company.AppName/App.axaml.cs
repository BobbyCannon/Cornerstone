#region References

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Company.AppName.Keystone;

#endregion

namespace Company.AppName;

public class App : CornerstoneApplication<AppKeystone>
{
	#region Methods

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
		base.Initialize();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new AppWindow(Keystone.ViewModel);
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
