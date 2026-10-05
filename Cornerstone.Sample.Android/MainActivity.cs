#region References

using System;
using System.Collections.Generic;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Cornerstone.Platforms.Android;
using Cornerstone.Presentation.Android;
using Cornerstone.Presentation.Platforms.Android;
using Cornerstone.Runtime;
using Permission = Android.Content.PM.Permission;

#endregion

namespace Cornerstone.Sample.Android;

[Activity(
	Label = "Sample",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	LaunchMode = LaunchMode.SingleTop,
	ConfigurationChanges =
		ConfigChanges.Orientation
		| ConfigChanges.ScreenSize
		| ConfigChanges.UiMode
		| ConfigChanges.Keyboard)]
[IntentFilter(
	[NfcAdapter.ActionTagDiscovered, NfcAdapter.ActionNdefDiscovered, NfcAdapter.ActionTechDiscovered],
	Categories = [Intent.CategoryDefault])]
public class MainActivity : CornerstoneMainActivity
{
	#region Constants

	private const int CameraPermissionsRequestCode = 1001;

	#endregion

	#region Methods

	protected override void OnCreate(Bundle savedInstanceState)
	{
		AndroidHost.Initialize(this);
		base.OnCreate(savedInstanceState);
		RequestRuntimePermissionsIfNeeded();
		TryGetAndroidPlatform()?.OnNewIntent(Intent);
	}

	protected override void OnNewIntent(Intent intent)
	{
		base.OnNewIntent(intent);
		Intent = intent;
		TryGetAndroidPlatform()?.OnNewIntent(intent);
	}

	public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
	{
		base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		TryGetAndroidPlatform()?.OnRequestPermissionsResult(requestCode, permissions, grantResults);
	}

	protected override void OnResume()
	{
		base.OnResume();
		AndroidHost.Initialize(this);
		TryGetAndroidPlatform()?.OnResume();
	}

	/// <summary>
	/// CAMERA and RECORD_AUDIO are dangerous permissions (API 23+). Manifest entries alone are not enough.
	/// </summary>
	private void RequestRuntimePermissionsIfNeeded()
	{
		var needed = new List<string>();

		if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
		{
			needed.Add(Manifest.Permission.Camera);
		}

		if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.RecordAudio) != Permission.Granted)
		{
			needed.Add(Manifest.Permission.RecordAudio);
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(33)
			&& (ContextCompat.CheckSelfPermission(this, Manifest.Permission.PostNotifications) != Permission.Granted))
		{
			needed.Add(Manifest.Permission.PostNotifications);
		}

		if (needed.Count > 0)
		{
			ActivityCompat.RequestPermissions(this, needed.ToArray(), CameraPermissionsRequestCode);
		}
	}

	private static AndroidPlatform TryGetAndroidPlatform()
	{
		return AppBootstrap.TryGetPlatform(out var platform) ? platform as AndroidPlatform : null;
	}

	#endregion
}