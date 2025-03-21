#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Media;

public class AudioPlayerStub : AudioPlayer
{
	#region Constructors

	/// <inheritdoc />
	public AudioPlayerStub(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public override int CurrentPosition { get; }

	public override int Duration { get; }

	/// <inheritdoc />
	public override void Pause()
	{
	}

	/// <inheritdoc />
	public override void Play()
	{
	}

	/// <inheritdoc />
	public override void Seek(double position)
	{
	}

	/// <inheritdoc />
	public override void Stop()
	{
	}

	/// <inheritdoc />
	public override void Speak(string message)
	{

	}

	/// <inheritdoc />
	protected override void PlayFile(string audioFilePath)
	{
	}

	#endregion
}

/// <summary>
/// Provides the ability to play audio.
/// </summary>
public abstract class AudioPlayer : Bindable, IDisposable
{
	#region Constructors

	protected AudioPlayer(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	~AudioPlayer()
	{
		Dispose(false);
	}

	#endregion

	#region Properties

	public bool IsLoopingEnabled { get; set; }

	public bool IsPlaying { get; protected set; }

	public abstract int CurrentPosition { get; }
	
	public abstract int Duration { get; }

	protected bool IsDisposed { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public abstract void Pause();

	public void Play(string audioFilePath)
	{
		Stop();
		PlayFile(audioFilePath);
	}

	public abstract void Play();

	public abstract void Seek(double position);

	public abstract void Stop();

	/// <summary>
	/// Speak out a text phrase.
	/// </summary>
	/// <param name="message"> The message to speak. </param>
	public abstract void Speak(string message);

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		IsDisposed = true;
	}

	protected virtual void OnMediaButtonPressed(MediaButton e)
	{
		MediaButtonPressed?.Invoke(this, e);
	}

	protected virtual void OnPlaybackEnded()
	{
		PlaybackEnded?.Invoke(this, EventArgs.Empty);
	}

	protected abstract void PlayFile(string audioFilePath);

	#endregion

	#region Events

	public event EventHandler<MediaButton> MediaButtonPressed;

	public event EventHandler PlaybackEnded;

	#endregion
}