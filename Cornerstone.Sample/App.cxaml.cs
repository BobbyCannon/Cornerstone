#region References

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample;

public class App : Application<AppKeystone>
{
	#region Methods

	public override void Initialize()
	{
		AppBootstrap.StartupProfiler?.RecordMark();
		using (AppBootstrap.StartupProfiler.Start("App.XamlAndSerializers"))
		{
			using (AppBootstrap.StartupProfiler.Start("CornerstoneXamlLoader"))
			{
				CornerstoneXamlLoader.Load(this);
			}
		}

		base.Initialize();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		RecordAfterInitializeGap();
		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				using (AppBootstrap.StartupProfiler.Start("MainWindow.Create"))
				{
					desktop.MainWindow = new AppWindow(Keystone.ViewModel);
				}
				break;
			}
			case ISingleViewApplicationLifetime singleViewPlatform:
			{
				using (AppBootstrap.StartupProfiler.Start("MainView.Create"))
				{
					singleViewPlatform.MainView = new AppView(Keystone.ViewModel);
				}
				break;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	public override void RegisterServices()
	{
		// Base ensures AppBootstrap (design-time) and registers the UI dispatcher.
		base.RegisterServices();
		using (AppBootstrap.StartupProfiler.Start("App.RegisterApplicationServices"))
		{
			RegisterServices(AppBootstrap.DependencyProvider, Design.IsDesignMode);
		}

		AppBootstrap.StartupProfiler?.Mark("Cornerstone.Presentation.BeforeInitialize");
	}

	public static void RegisterServices(DependencyProvider dependencyProvider, bool designOrUnitTesting)
	{
		if (designOrUnitTesting)
		{
			dependencyProvider.AddDesignStubs();
		}

		Cornerstone.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
		Presentation.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
		CornerstoneGenerated.RegisterDependencies(dependencyProvider);
	}

	/// <inheritdoc />
	protected override void CompleteStartupProfiling()
	{
		base.CompleteStartupProfiling();

		var profiler = AppBootstrap.StartupProfiler;
		if (profiler is not { IsCompleted: true })
		{
			return;
		}

		var report = profiler.ToReport();
		Debug.WriteLine(report);
		Console.WriteLine(report);
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