#region References

using System;
using System.Windows.Controls;
using Cornerstone.Media;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Windows;

public class WindowsAudioPlayer : AudioPlayer
{
	#region Fields

	private readonly MediaElement _player;

	#endregion

	#region Constructors

	public WindowsAudioPlayer(IDispatcher dispatcher) : base(dispatcher)
	{
		_player = new MediaElement
		{
			LoadedBehavior = MediaState.Manual,
			UnloadedBehavior = MediaState.Manual
		};
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Pause()
	{
		this.Dispatch(() =>
		{
			_player.Pause();

			IsPlaying = false;
		});
	}

	/// <inheritdoc />
	public override void Play()
	{
		this.Dispatch(() =>
		{
			_player.Play();

			IsPlaying = true;
		});
	}

	/// <inheritdoc />
	public override void Seek(double position)
	{
		this.Dispatch(() =>
		{
			var isPlaying = IsPlaying;
			Pause();

			_player.Position = TimeSpan.FromSeconds(position);

			if (isPlaying)
			{
				Play();
			}
		});
	}

	/// <inheritdoc />
	public override void Stop()
	{
		this.Dispatch(() =>
		{
			_player.Stop();

			IsPlaying = false;
		});
	}

	/// <inheritdoc />
	protected override void PlayFile(string audioFilePath)
	{
		this.Dispatch(() =>
		{
			Stop();

			_player.Position = new TimeSpan(0);
			_player.Source = new Uri(audioFilePath);

			Play();
		});
	}

	#endregion
}