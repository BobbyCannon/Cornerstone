#region References

using System.Collections.Generic;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Avalonia.Android;
using Cornerstone.Avalonia.Platforms.Android;
using Permission = Android.Content.PM.Permission;

#endregion

namespace Cornerstone.Sample.Android;

[Activity(
	Label = "Sample",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	ConfigurationChanges =
		ConfigChanges.Orientation
		| ConfigChanges.ScreenSize
		| ConfigChanges.UiMode
		| ConfigChanges.Keyboard)]
public class MainActivity : AvaloniaMainActivity
{
	#region Constants

	private const int CameraPermissionsRequestCode = 1001;

	#endregion

	#region Methods

	protected override void OnCreate(Bundle savedInstanceState)
	{
		AndroidHost.Initialize(this);
		base.OnCreate(savedInstanceState);
		RequestCameraPermissionsIfNeeded();
	}

	/// <summary>
	/// CAMERA and RECORD_AUDIO are dangerous permissions (API 23+). Manifest entries alone are not enough.
	/// </summary>
	private void RequestCameraPermissionsIfNeeded()
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

		if (needed.Count > 0)
		{
			ActivityCompat.RequestPermissions(this, needed.ToArray(), CameraPermissionsRequestCode);
		}
	}

	#endregion
}