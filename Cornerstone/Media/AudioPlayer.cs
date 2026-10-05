#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Media;

public partial class AudioPlayerStub : AudioPlayer
{
	#region Constructors

	public AudioPlayerStub(IDateTimeProvider dateTimeProvider, IDispatcher dispatcher)
		: base(dateTimeProvider, dispatcher)
	{
	}

	#endregion

	#region Properties

	public override int CurrentPosition { get; }

	public override int Duration { get; }

	#endregion

	#region Methods

	public override void Pause()
	{
		IsPlaying = false;
	}

	public override void Play()
	{
		IsPlaying = true;
	}

	public override void Seek(double position)
	{
	}

	public override void Speak(string message)
	{
	}

	public override void Stop()
	{
		IsPlaying = false;
	}

	protected override void PlayFile(string audioFilePath)
	{
		IsPlaying = true;
	}

	protected override void PlayUri(Uri uri)
	{
		IsPlaying = true;
	}

	#endregion
}

/// <summary>
/// Provides the ability to play audio.
/// </summary>
public abstract partial class AudioPlayer : CornerstoneObject, IDispatchable, IDisposable
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly IDispatcher _dispatcher;

	#endregion

	#region Constructors

	protected AudioPlayer(IDateTimeProvider dateTimeProvider, IDispatcher dispatcher)
	{
		_dateTimeProvider = dateTimeProvider;
		_dispatcher = dispatcher;
	}

	~AudioPlayer()
	{
		Dispose(false);
	}

	#endregion

	#region Properties

	public abstract int CurrentPosition { get; }

	/// <summary>
	/// Total duration in milliseconds.
	/// </summary>
	public abstract int Duration { get; }

	[Notify]
	public partial bool IsLoopingEnabled { get; set; }

	[Notify]
	public partial bool IsPlaying { get; protected set; }

	[Notify]
	public partial DateTime PlayedOn { get; private set; }

	[Notify]
	protected partial bool IsDisposed { get; set; }

	#endregion

	#region Methods

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public abstract void Pause();

	public void Play(string audioFilePath)
	{
		Stop();
		PlayedOn = _dateTimeProvider.UtcNow;
		PlayFile(audioFilePath);
	}

	public void Play(Uri uri)
	{
		Stop();
		PlayedOn = _dateTimeProvider.UtcNow;
		PlayUri(uri);
	}

	public abstract void Play();

	public abstract void Seek(double position);

	/// <summary>
	/// Speak out a text phrase.
	/// </summary>
	/// <param name="message"> The message to speak. </param>
	public abstract void Speak(string message);

	public abstract void Stop();

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
		IsPlaying = false;
		PlaybackEnded?.Invoke(this, EventArgs.Empty);
	}

	protected abstract void PlayFile(string audioFilePath);

	protected abstract void PlayUri(Uri uri);

	#endregion

	#region Events

	public event EventHandler<MediaButton> MediaButtonPressed;

	public event EventHandler PlaybackEnded;

	#endregion
}
