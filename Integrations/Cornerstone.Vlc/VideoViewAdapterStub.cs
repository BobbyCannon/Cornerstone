#region References

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Platform;
using LibVlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;

#endregion

namespace Cornerstone.Vlc;

public class VideoViewAdapterStub : IVideoViewAdapter
{
	#region Constructors

	public VideoViewAdapterStub()
	{
		IsNativeSurfaceVisible = true;
		PlatformHandle = null;
	}

	#endregion

	#region Properties

	public bool IsNativeSurfaceVisible { get; private set; }

	public LibVlcMediaPlayer MediaPlayer => null;

	public IPlatformHandle PlatformHandle { get; }

	public Uri Source { get; set; }

	#endregion

	#region Methods

	public void HandleResize(int width, int height, float scaling)
	{
	}

	public void Pause()
	{
	}

	public void Play()
	{
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
	}

	public void Stop()
	{
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged
	{
		add
		{
		}
		remove
		{
		}
	}

	#endregion
}
