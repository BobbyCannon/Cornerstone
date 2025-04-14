#region References

using Android.Content;
using Avalonia.Controls;
using Cornerstone.Avalonia.MediaPlayer;
using Cornerstone.Platforms.Android;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android;

public class MediaPlayerAdapter : IMediaPlayerAdapter
{
	#region Fields

	private readonly Context _context;

	#endregion

	#region Constructors

	public MediaPlayerAdapter()
	{
		_context = AndroidPlatform.ApplicationContext;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
	}

	public void Initialize(NativeControlHost nativeHost)
	{
	}

	public void Pause()
	{
		// Pause not directly supported via Intent; user controls via system player
	}

	public void Play(string uri)
	{
		var intent = new Intent(Intent.ActionView);
		intent.SetDataAndType(AndroidUri.Parse(uri), "video/*");
		intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

		_context.StartActivity(intent);
	}

	public void Stop()
	{
		// Stop not directly supported via Intent; user controls via system player
	}

	#endregion
}