#region References

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Avalonia;
using Avalonia.Android;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Platforms.Android;
using Cornerstone.Platforms.Android;

#endregion

namespace Cornerstone.Sample.Android;

[Activity(Label = "Cornerstone Sample",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
[IntentFilter([NfcAdapter.ActionTagDiscovered], Categories = [Intent.CategoryDefault], DataMimeType = DataMimeType)]
public class MainActivity : AvaloniaMainActivity<App>
{
	#region Constants

	private const string DataMimeType = "application/com.cornerstone.sample";

	#endregion

	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		var dependencyProvider = CornerstoneApplication.DependencyProvider;

		AndroidPlatform.Initialize(this, dependencyProvider);

		var response = base
			.CustomizeAppBuilder(builder)
			.WithInterFont()
			.UseCornerstone();

		return response;
	}

	/// <inheritdoc />
	protected override void OnNewIntent(Intent intent)
	{
		AndroidPlatform.OnNewIntent(intent);
		base.OnNewIntent(intent);
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