#region References

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cornerstone.Avalonia;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor;

public partial class App : CornerstoneApplication<AppKeystone>
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
		if (designOrUnitTesting)
		{
			dependencyProvider.AddDesignStubs();
		}

		Cornerstone.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
		Avalonia.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
		CornerstoneGenerated.RegisterDependencies(dependencyProvider);
	}

	protected override void OnShutdown()
	{
		Keystone.State.Settings.Save();
		base.OnShutdown();
	}

	#endregion
}