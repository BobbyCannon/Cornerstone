#region References

using System;
using Android.Media;
using Cornerstone.Media;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Android;

public class AndroidAudioPlayer : AudioPlayer
{
	#region Fields

	private readonly MediaPlayer _player;

	#endregion

	#region Constructors

	public AndroidAudioPlayer(IDateTimeProvider dateTimeProvider, IDispatcher dispatcher)
		: base(dateTimeProvider, dispatcher)
	{
		_player = new MediaPlayer();
		_player.Completion += OnPlaybackEnded;
	}

	#endregion

	#region Properties

	public override int CurrentPosition => _player.CurrentPosition;

	public override int Duration => !_player.IsPlaying || (_player.Duration <= -1) ? 0 : _player.Duration;

	#endregion

	#region Methods

	public override void Pause()
	{
		if (!IsPlaying)
		{
			return;
		}

		IsPlaying = false;
		_player.Pause();
	}

	public override void Play()
	{
		IsPlaying = true;
		_player.Start();
	}

	public override void Seek(double position)
	{
		_player.Pause();
		_player.SeekTo((int) (position * 1000D));

		if (IsPlaying)
		{
			_player.Start();
		}
	}

	public override void Speak(string message)
	{
		//TextToSpeech.Default.SpeakAsync(message);
	}

	public override void Stop()
	{
		_player.Stop();
		_player.SeekTo(0);
	}

	protected override void Dispose(bool disposing)
	{
		if (IsDisposed)
		{
			return;
		}

		if (disposing)
		{
			_player.Completion -= OnPlaybackEnded;
			_player.Reset();
			_player.Release();
			_player.Dispose();
		}

		base.Dispose(disposing);
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		if (propertyName == nameof(IsLoopingEnabled))
		{
			_player.Looping = IsLoopingEnabled;
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	protected override void PlayFile(string audioFilePath)
	{
		Stop();

		_player.Reset();
		_player.SetDataSource(audioFilePath);
		_player.Prepare();

		Play();
	}

	protected override void PlayUri(Uri uri)
	{
		// todo:
	}

	private void OnPlaybackEnded(object sender, EventArgs e)
	{
		IsPlaying = _player.IsPlaying;

		// This improves stability on older devices but has minor performance impact
		// We need to check whether the player is null or not as the user might have
		// disposed it in an event handler to PlaybackEnded above.
		if (!OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			_player.SeekTo(0);
			_player.Stop();
			_player.Prepare();
		}

		OnPlaybackEnded();
	}

	#endregion
}