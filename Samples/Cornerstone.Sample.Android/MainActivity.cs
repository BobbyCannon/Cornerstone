#region References

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using AndroidX.Core.App;
using Avalonia;
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
public class MainActivity : CornerstoneActivity<App>
{
	#region Constants

	private const string DataMimeType = "application/com.cornerstone.sample";

	#endregion

	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		var dependencyProvider = CornerstoneApplication.DependencyProvider;

		AndroidPlatform.Initialize(this, dependencyProvider);

		// In your Activity or where appropriate
		ActivityCompat.RequestPermissions(this, [
			Manifest.Permission.AccessMediaLocation,
			Manifest.Permission.Camera,
			Manifest.Permission.AccessFineLocation,
			Manifest.Permission.ManageExternalStorage,
			Manifest.Permission.ReadExternalStorage,
			Manifest.Permission.ReadMediaAudio,
			Manifest.Permission.WriteExternalStorage
		], 0);

		var response = base
			.CustomizeAppBuilder(builder)
			.WithInterFont()
			.UseCornerstone();

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