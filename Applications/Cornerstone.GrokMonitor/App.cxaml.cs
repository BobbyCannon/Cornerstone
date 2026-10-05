#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Theme;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.DesignTime;

#endregion

namespace Cornerstone.GrokMonitor;

public partial class App : Application<AppKeystone>
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
		Cornerstone.Presentation.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
		CornerstoneGenerated.RegisterDependencies(dependencyProvider);
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