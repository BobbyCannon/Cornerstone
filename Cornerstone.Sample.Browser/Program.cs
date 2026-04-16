#region References

using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Platforms.Browser;
using Cornerstone.Extensions;
using Cornerstone.Platforms.Browser;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Browser;

internal sealed class Program
{
	#region Fields

	private static AppViewModel _applicationViewModel;

	#endregion

	#region Methods

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>();
	}

	private static void AppViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(AppViewModel.SelectedTab):
			{
				var browser = CornerstoneApplication.DependencyProvider.GetInstance<BrowserInteropProxy>();
				var location = browser.WindowsLocation;
				location = location.UpdateQueryParameter("Tab", _applicationViewModel.SelectedTab.TabName);
				browser.WindowsLocation = location;
				break;
			}
		}
	}

	private static Task Main(string[] args)
	{
		CornerstoneApplication.RuntimeInformation.Initialize(typeof(Program).Assembly);
		CornerstoneApplication.RuntimeInformation.SetPlatformOverride(nameof(RuntimeInformation.ApplicationName), "Cornerstone.Sample");
		CornerstoneApplication.ApplicationArguments.ParseFromBrowser(args);

		return BuildAvaloniaApp()
			.UseCornerstone(args, out var options)
			.StartBrowserAppAsync("out", options)
			.ContinueWith(_ =>
			{
				_applicationViewModel ??= CornerstoneApplication.DependencyProvider.GetInstance<AppViewModel>();
				_applicationViewModel.PropertyChanged += AppViewModelOnPropertyChanged;
			});
	}

	#endregion
}