#region References

using System;
using System.ComponentModel;
using LibVlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;

#endregion

namespace Cornerstone.Vlc;

/// <summary>
/// Pass-through video surface. Playback types stay LibVLCSharp's.
/// </summary>
public interface IVideoView : INotifyPropertyChanged
{
	#region Properties

	LibVlcMediaPlayer MediaPlayer { get; }

	Uri Source { get; set; }

	#endregion

	#region Methods

	void Pause();

	void Play();

	void Stop();

	#endregion
}
