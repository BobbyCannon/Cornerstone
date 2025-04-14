#region References

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Telecom;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android;

[Activity(Exported = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class CameraActivity : Activity, MediaScannerConnection.IOnScanCompletedListener
{
	#region Methods

	public void OnScanCompleted(string path, AndroidUri uri)
	{
	}

	protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
	{
		base.OnActivityResult(requestCode, resultCode, data);

		Finish();
	}

	protected override void OnCreate(Bundle state)
	{
		base.OnCreate(state);

		state ??= Intent?.Extras;

		if (state == null)
		{
			return;
		}

		Intent intent = null;

		try
		{
			intent = new Intent(state.GetString("action"));
			intent.PutExtra(MediaStore.ExtraVideoQuality, (int) VideoQuality.High);
			StartActivityForResult(intent, state.GetInt("id"));
		}
		catch
		{
			// Close due to error
			Finish();
		}
		finally
		{
			intent?.Dispose();
		}
	}

	#endregion
}