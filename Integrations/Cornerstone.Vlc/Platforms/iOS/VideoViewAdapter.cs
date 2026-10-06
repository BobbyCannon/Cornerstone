#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.iOS;
using Cornerstone.Presentation.Platform;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using LibVLCSharp.Shared;
using LibVlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using LibVlcMedia = LibVLCSharp.Shared.Media;
using UIKit;
using LibVlcVideoView = LibVLCSharp.Platforms.iOS.VideoView;

#endregion

namespace Cornerstone.Vlc.Platforms.iOS;

[SourceReflection]
internal class VideoViewAdapter : CornerstoneObject, IVideoViewAdapter, IDisposable
{
	#region Fields

	private bool _disposed;
	private readonly LibVLC _libVlc;
	private readonly LibVlcMediaPlayer _mediaPlayer;
	private readonly LibVlcVideoView _videoView;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public VideoViewAdapter()
	{
		IsNativeSurfaceVisible = true;
		Core.Initialize();
		_libVlc = new LibVLC();
		_mediaPlayer = new LibVlcMediaPlayer(_libVlc);
		_videoView = new LibVlcVideoView
		{
			AutoresizingMask = UIViewAutoresizing.None,
			MediaPlayer = _mediaPlayer
		};
		PlatformHandle = new UIViewControlHandle(_videoView);
	}

	#endregion

	#region Properties

	public bool IsNativeSurfaceVisible { get; private set; }

	public LibVlcMediaPlayer MediaPlayer => _mediaPlayer;

	public IPlatformHandle PlatformHandle { get; }

	public Uri Source { get; set; }

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_mediaPlayer.Stop();
		_videoView.MediaPlayer = null;
		_videoView.RemoveFromSuperview();
		_videoView.Dispose();
		_mediaPlayer.Dispose();
		_libVlc.Dispose();
	}

	public void HandleResize(int width, int height, float scaling)
	{
	}

	public void Pause()
	{
		_mediaPlayer.Pause();
	}

	public void Play()
	{
		if (Source == null)
		{
			return;
		}

		using var media = new LibVlcMedia(_libVlc, Source);
		_mediaPlayer.Play(media);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		_videoView.Hidden = !visible;
	}

	public void Stop()
	{
		_mediaPlayer.Stop();
	}

	#endregion
}
