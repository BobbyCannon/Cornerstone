#region References

using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Sample.Tabs;
using Avalonia.Sample.ViewModels;
using Avalonia.Sample.Views;
using Cornerstone.Avalonia;
using Cornerstone.Runtime;

#endregion

namespace Avalonia.Sample;

public class App : CornerstoneApplication
{
	#region Properties

	public static ApplicationSettings Settings { get; private set; }

	#endregion

	#region Methods

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		// Line below is needed to remove Avalonia data validation.
		// Without this line you will get duplicate validations from both Avalonia and CT
		// Removes {Avalonia.Data.Core.Plugins.DataAnnotationsValidationPlugin}
		var found = BindingPlugins.DataValidators.FirstOrDefault(x => x is DataAnnotationsValidationPlugin);
		if (found != null)
		{
			BindingPlugins.DataValidators.Remove(found);
		}

		Settings = GetService<ApplicationSettings>();
		Settings.Load();

		var viewModel = GetService<MainViewModel>();
		viewModel.Initialize();

		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				desktop.MainWindow = new MainWindow(viewModel);
				break;
			}
			case ISingleViewApplicationLifetime singleViewPlatform:
			{
				singleViewPlatform.MainView = new MainView(viewModel);
				break;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	/// <inheritdoc />
	/// <remarks>
	/// <see cref="ViewModelProviderForDesignMode" />
	/// </remarks>
	public override void RegisterServices()
	{
		Dependencies.AddSingleton<ApplicationSettings>();
		Dependencies.AddSingleton<MainViewModel>();
		Dependencies.AddSingleton<TabIconsModel>();

		base.RegisterServices();
	}

	#endregion
}