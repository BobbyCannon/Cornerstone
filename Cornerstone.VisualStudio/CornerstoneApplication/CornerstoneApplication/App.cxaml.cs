#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Runtime;
using CornerstoneApplication.ViewModels;
using CornerstoneApplication.Views;

#endregion

namespace CornerstoneApplication;

public class App : global::Cornerstone.Presentation.Application
{
	#region Methods

	public override void Initialize()
	{
		CornerstoneXamlLoader.Load(this);
		base.Initialize();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		var viewModel = AppBootstrap.GetInstance<MainViewModel>();

        switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				desktop.MainWindow = new MainWindow
				{
					DataContext = viewModel
				};
				break;
			}
			case ISingleViewApplicationLifetime singleViewPlatform:
			{
				singleViewPlatform.MainView = new MainView
				{
					DataContext = viewModel
				};
				break;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

    public static void RegisterServices(DependencyProvider dependencyProvider, bool designOrUnitTesting)
    {
        if (designOrUnitTesting)
        {
            dependencyProvider.AddDesignStubs();
        }

        Cornerstone.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
        Cornerstone.Presentation.CornerstoneGenerated.RegisterDependencies(dependencyProvider);
        //CornerstoneGenerated.RegisterDependencies(dependencyProvider);
    }

    #endregion
}
