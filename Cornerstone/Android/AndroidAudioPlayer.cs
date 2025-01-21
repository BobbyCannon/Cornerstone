#region References

using System;
using Android.Media;
using Cornerstone.Media;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Android;

public class AndroidAudioPlayer : AudioPlayer
{
	#region Fields

	private readonly MediaPlayer _player;

	#endregion

	#region Constructors

	public AndroidAudioPlayer(IDispatcher dispatcher) : base(dispatcher)
	{
		_player = new MediaPlayer();
		_player.Completion += OnPlaybackEnded;
	}

	#endregion

	#region Properties

	public double CurrentPosition => _player.CurrentPosition;

	public double Duration => _player.Duration <= -1 ? -1 : _player.Duration / 1000.0;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Pause()
	{
		if (!IsPlaying)
		{
			return;
		}

		IsPlaying = false;
		_player.Pause();
	}

	/// <inheritdoc />
	public override void Play()
	{
		IsPlaying = true;
		_player.Start();
	}

	/// <inheritdoc />
	public override void Seek(double position)
	{
		_player.Pause();
		_player.SeekTo((int) (position * 1000D));

		if (IsPlaying)
		{
			_player.Start();
		}
	}

	/// <inheritdoc />
	public override void Stop()
	{
		Pause();
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	protected override void OnPropertyChanged(string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(IsLoopingEnabled):
			{
				_player.Looping = IsLoopingEnabled;
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <inheritdoc />
	protected override void PlayFile(string audioFilePath)
	{
		Stop();

		_player.Reset();
		_player.SetDataSource(audioFilePath);
		_player.Prepare();

		Play();
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