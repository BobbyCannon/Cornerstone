#region References

using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Windows.Media.SpeechSynthesis;
using Cornerstone.Attributes;
using Cornerstone.Media;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Windows;

public class WindowsAudioPlayer : AudioPlayer
{
	#region Fields

	private readonly MediaElement _player;
	private readonly IRuntimeInformation _runtimeInformation;
	private readonly SpeechSynthesizer _speech;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public WindowsAudioPlayer(IRuntimeInformation runtimeInformation, IDispatcher dispatcher) : base(dispatcher)
	{
		_runtimeInformation = runtimeInformation;
		_player = new MediaElement
		{
			LoadedBehavior = MediaState.Manual,
			UnloadedBehavior = MediaState.Manual,
			Volume = 1
		};

		_speech = new SpeechSynthesizer();
		_speech.Voice =
			SpeechSynthesizer.AllVoices.FirstOrDefault(x => x.DisplayName == "Microsoft Mark")
			?? SpeechSynthesizer.AllVoices.FirstOrDefault(x => (x.Gender == VoiceGender.Male) && (x.Language == "en-US"))
			?? SpeechSynthesizer.AllVoices.First();
	}

	#endregion

	#region Properties

	public override int CurrentPosition => (int) _player.Position.TotalMilliseconds;

	public override int Duration => _player.NaturalDuration.HasTimeSpan ? (int) _player.NaturalDuration.TimeSpan.TotalMilliseconds : 0;

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
	public override async void Speak(string message)
	{
		try
		{
			Stop();

			using var stream = await _speech.SynthesizeTextToStreamAsync(message);
			var speechFilePath = Path.Join(_runtimeInformation.ApplicationDataLocation, "speech.wav");
			await using var fileStream = new FileStream(speechFilePath, FileMode.Create);
			stream.AsStreamForRead().CopyTo(fileStream);
			fileStream.Close();
			PlayFile(speechFilePath);
		}
		catch
		{
			// Ignore any issues
		}
	}

	/// <inheritdoc />
	public override void Stop()
	{
		this.Dispatch(() =>
		{
			_player.Stop();
			_player.Source = null;

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