#region References

using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Telecom;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Debug = System.Diagnostics.Debug;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android;

[Activity(Exported = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class CameraActivity : Activity, MediaScannerConnection.IOnScanCompletedListener
{
	#region Fields

	internal static readonly SpeedyList<CameraAdapter> Adapters;

	#endregion

	#region Constructors

	static CameraActivity()
	{
		Adapters = new SpeedyList<CameraAdapter>();
	}

	#endregion

	#region Methods

	public void OnScanCompleted(string path, AndroidUri uri)
	{
		// Handle media scan completion if needed
	}

	protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
	{
		base.OnActivityResult(requestCode, resultCode, data);

		try
		{
			if ((resultCode != Result.Ok) || (data?.Data == null))
			{
				return;
			}

			try
			{
				// Read the video data from the URI
				var videoData = ReadVideoDataFromUri(data.Data);
				Adapters.ForEach(x => x.SetVideoData(requestCode, videoData));
			}
			catch
			{
				// Ignore any errors.
			}
		}
		finally
		{
			Finish();
		}
	}

	protected override void OnCreate(Bundle state)
	{
		base.OnCreate(state);

		state ??= Intent?.Extras;

		if (state == null)
		{
			Finish();
			return;
		}

		Intent intent = null;

		try
		{
			var requestCode = state.GetInt("id");
			intent = new Intent(state.GetString("action"));
			intent.PutExtra(MediaStore.ExtraVideoQuality, (int) VideoQuality.High);
			StartActivityForResult(intent, requestCode);
		}
		catch
		{
			Finish();
		}
		finally
		{
			intent?.Dispose();
		}
	}

	private byte[] ReadVideoDataFromUri(AndroidUri uri)
	{
		try
		{
			using var inputStream = ContentResolver?.OpenInputStream(uri);
			if (inputStream == null)
			{
				return null;
			}

			using var memoryStream = new MemoryStream();
			inputStream.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error reading URI stream: {ex.Message}");
			throw;
		}
	}

	#endregion
}