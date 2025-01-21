#region References

using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Input;
using Cornerstone.Sample.Tabs;
using Cornerstone.Sample.ViewModels;
using Cornerstone.Sample.Views;

#endregion

namespace Cornerstone.Sample;

public class App : CornerstoneApplication
{
	#region Properties

	public GamepadInput GamepadInput { get; private set; }

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

		GamepadInput = GetInstance<GamepadInput>();
		GamepadInput.StartWorking();

		Settings = GetInstance<ApplicationSettings>();
		Settings.PropertyChanged += SettingsOnPropertyChanged;
		Settings.Load();
		Settings.NotifyOfPropertyChanged(nameof(Settings.UseGamepadForInput));

		var viewModel = GetInstance<MainViewModel>();
		viewModel.Initialize();

		if ((viewModel.RuntimeInformation.DotNetRuntimeVersion.Major >= 9)
			&& viewModel.ApplicationSettings.SelectedTabName
				is TabMapsui.HeaderName
				or TabLocationProvider.HeaderName)
		{
			viewModel.ApplicationSettings.SelectedTabName = TabThemes.HeaderName;
		}

		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				desktop.MainWindow = new MainWindow(viewModel, GetDispatcher());
				break;
			}
			case ISingleViewApplicationLifetime singleViewPlatform:
			{
				singleViewPlatform.MainView = new MainView(viewModel, GetDispatcher());
				break;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	/// <inheritdoc />
	/// <remarks>
	/// <see cref="DesignModeDependencyProvider" />
	/// </remarks>
	public override void RegisterServices()
	{
		DependencyProvider.AddSingleton<ApplicationSettings>();
		DependencyProvider.AddSingleton<MainViewModel>();
		DependencyProvider.AddSingleton<TabIconsModel>();
		base.RegisterServices();
	}

	private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ApplicationSettings.UseGamepadForInput):
			{
				if (Settings.UseGamepadForInput)
				{
					GamepadInput.Gamepad.StartWorking();
				}
				else
				{
					GamepadInput.Gamepad.StopWorking();
				}
				break;
			}
		}
	}

	#endregion
}