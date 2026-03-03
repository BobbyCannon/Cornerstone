#region References

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cornerstone.Avalonia;

#endregion

namespace Cornerstone.Sample;

public class App : CornerstoneApplication
{
	#region Properties

	public AppViewModel ViewModel { get; set; }

	#endregion

	#region Methods

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
		base.Initialize();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		ViewModel = DependencyProvider.GetInstance<AppViewModel>();
		ViewModel.Initialize();

		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				desktop.MainWindow = new AppWindow(ViewModel);
				break;
			}
			case ISingleViewApplicationLifetime singleViewPlatform:
			{
				singleViewPlatform.MainView = new AppView(ViewModel);
				break;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	public override void RegisterServices()
	{
		DependencyProvider.AddSingleton<ApplicationSettings>();
		DependencyProvider.AddSingleton<AppViewModel>();
		base.RegisterServices();
	}

	#endregion
}