#region References

using Avalonia.Controls;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.MediaPlayer;

internal class MediaPlayerAdapterStub : Bindable, IMediaPlayerAdapter
{
	#region Constructors

	public MediaPlayerAdapterStub(IDispatcher dispatcher) : base(dispatcher)
	{
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
	}

	public void Play(string uri)
	{
	}

	public void Stop()
	{
	}

	#endregion
}