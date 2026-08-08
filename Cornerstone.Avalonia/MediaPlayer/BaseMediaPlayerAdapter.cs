#region References

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.MediaPlayer;

public abstract class BaseMediaPlayerAdapter : IDisposable, IPausableNativeSurface
{
	#region Properties

	/// <summary>
	/// The total duration of the currently loaded media or zero if unknown.
	/// </summary>
	public virtual TimeSpan Duration => TimeSpan.Zero;

	public virtual bool IsMuted { get; set; }

	/// <inheritdoc />
	public bool IsNativeSurfaceVisible { get; private set; } = true;

	/// <inheritdoc />
	public virtual IPlatformHandle PlatformHandle => null;

	/// <summary>
	/// The current playback position.
	/// </summary>
	public virtual TimeSpan Position { get; set; }

	/// <summary>
	/// The current playback state, default stopped.
	/// </summary>
	public virtual MediaPlaybackState State => MediaPlaybackState.Stopped;

	/// <summary>
	/// The playback volume in the range 0.0 to 1.0.
	/// </summary>
	public virtual double Volume { get; set; } = 1.0;

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		return Task.FromResult(NativeSurfaceSnapshot.Failed("Media player snapshot is not supported on this platform."));
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	public virtual void HandleResize(int width, int height, float scaling)
	{
		UpdateVideoLayout();
	}

	public virtual void Initialize(NativeControlHost nativeHost)
	{
		OnInitialized();
	}

	public abstract void Pause();

	public abstract void Play(string uri);

	public abstract void PlayFile(string filePath);

	public virtual void Resume()
	{
	}

	/// <inheritdoc />
	public virtual void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;

		// Freeze decoding/audio when the host hides the surface for Avalonia overlays.
		if (!visible)
		{
			Pause();
		}
	}

	/// <summary>
	/// Sets how the video is scaled within the host.
	/// False (default): preserve aspect ratio — width or height wins (letterbox/pillarbox).
	/// True: stretch to fill the host (may distort).
	/// </summary>
	/// <param name="fill"> True to fill the host, false to preserve the aspect ratio. </param>
	public virtual void SetVideoStretch(bool fill)
	{
	}

	public abstract void Stop();

	/// <summary>
	/// Requests the native video surface re-layout itself to match the current host bounds.
	/// </summary>
	public virtual void UpdateVideoLayout()
	{
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
	}

	protected virtual void OnClosed()
	{
		Closed?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnInitialized()
	{
		Initialized?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnMediaOpened()
	{
		MediaOpened?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnPlaybackEnded()
	{
		PlaybackEnded?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnStateChanged()
	{
		StateChanged?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	#region Events

	public event EventHandler Closed;

	public event EventHandler Initialized;

	public event EventHandler MediaOpened;

	public event EventHandler PlaybackEnded;

	public event EventHandler StateChanged;

	#endregion
}