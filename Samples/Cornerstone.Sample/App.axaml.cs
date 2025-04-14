#region References

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Presentation;
using Cornerstone.Sample.ViewModels;
using Cornerstone.Sample.Views;

#endregion

namespace Cornerstone.Sample;

public class App : CornerstoneApplication
{
	#region Methods

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		RuntimeInformation.Initialize();

		var viewModel = GetInstance<MainViewModel>();
		viewModel.Initialize();

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
	/// <see cref="ViewDependencyProvider" />
	/// </remarks>
	public override void RegisterServices()
	{
		RegisterServices(DependencyProvider);
		base.RegisterServices();
	}

	public static void RegisterServices(DependencyProvider dependencyProvider)
	{
		dependencyProvider.AddSingleton<IClipboardService, ClipboardService>();
		dependencyProvider.AddSingleton<ApplicationSettings>();
		dependencyProvider.AddSingleton<MainViewModel>();
	}

	#endregion
}