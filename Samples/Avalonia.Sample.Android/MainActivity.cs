#region References

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Avalonia.Android;
using Avalonia.Controls;
using Cornerstone.Android;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Android;
using Cornerstone.Location;
using AndroidApplication = Android.App.Application;

#endregion

namespace Avalonia.Sample.Android;

[Activity(
	Label = "Cornerstone Sample",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		var dependencyProvider = CornerstoneApplication.DependencyProvider;

		AndroidPlatform.Initialize(this, dependencyProvider);

		var response = base.CustomizeAppBuilder(builder)
			.WithInterFont()
			.UseCornerstoneAndroid();

		return response;
	}

	/// <inheritdoc />
	protected override void OnStart()
	{
		//var intent = new Intent(AndroidApplication.Context, typeof(ForegroundService));
		//AndroidApplication.Context.StartForegroundService(intent);
		base.OnStart();
	}

	#endregion
}