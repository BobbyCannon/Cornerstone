#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.Navigator.Keystone;
using Cornerstone.Navigator.Keystone.State;
using Cornerstone.Navigator.Views;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Navigator;

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
		dependencyProvider.AddSingleton<AppSettings>();
		dependencyProvider.AddSingleton<AppState>();
		dependencyProvider.AddSingleton<AppBus>();
		dependencyProvider.AddSingleton<AppEngine>();
		dependencyProvider.AddSingleton<AppKeystone>();
		dependencyProvider.AddSingleton<AppViewModel>();
		dependencyProvider.AddSingleton<IAppNavigator, AppViewModel>();
		dependencyProvider.AddSingleton<IAppDispatcher, AppViewModel>();
	}

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "SettingsManager.Save serializes app settings; members are preserved by generated source reflection.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "SettingsManager.Save serializes app settings; members are preserved by generated source reflection.")]
	protected override void OnShutdown()
	{
		Keystone.State.Settings.Save();
		base.OnShutdown();
	}

	#endregion
}