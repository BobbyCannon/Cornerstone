#region References

using System;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.MediaPlayer;

public interface IMediaPlayerAdapter : IDisposable
{
	#region Methods

	void Initialize(NativeControlHost nativeHost);
	void Pause();
	void Play(string uri);
	void Stop();

	#endregion
}