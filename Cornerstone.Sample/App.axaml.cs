#region References

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.DockingManager;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone;
using Cornerstone.Sample.Keystone.Channels;
using Cornerstone.Sample.Keystone.Processors;

#endregion

namespace Cornerstone.Sample;

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
		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				desktop.MainWindow = new AppWindow(Keystone.ViewModel);
				break;
			}
			case ISingleViewApplicationLifetime singleViewPlatform:
			{
				singleViewPlatform.MainView = new AppView(Keystone.ViewModel);
				break;
			}
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
		if (designOrUnitTesting)
		{
			dependencyProvider.AddDesignStubs();
		}

		Cornerstone.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
		Cornerstone.Avalonia.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
		CornerstoneGenerated.RegisterDependencies(dependencyProvider);
	}

	protected override void OnShutdown()
	{
		Keystone.State.Settings.Save();
		base.OnShutdown();
	}

	#endregion
}