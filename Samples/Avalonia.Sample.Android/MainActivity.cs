#region References

using Android.App;
using Android.Content.PM;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Sample.ViewModels;
using Cornerstone.Android;
using Cornerstone.Avalonia;
using Cornerstone.Location;

#endregion

namespace Avalonia.Sample.Android;

[Activity(
	Label = "Avalonia.Sample.Android",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		var locationProvider = new AndroidLocationProvider(this);
		CornerstoneApplication.PlatformDependencies.AddSingleton<ILocationProvider>(() => locationProvider);
		var response = base.CustomizeAppBuilder(builder).WithInterFont();
		return response;
	}

	#endregion
}