#region References

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Extensions;
using Cornerstone.Platforms.Windows;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

internal class CameraAdapter : BaseCameraAdapter
{
	#region Fields

	private MediaFrameReader _frameReader;
	private MediaCapture _mediaCapture;
	private byte[] _pixelData;

	#endregion

	#region Constructors

	public CameraAdapter(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public async Task CleanupAsync()
	{
		IsPreviewing = false;

		if (IsRecording)
		{
			await StopRecordingAsync();
		}

		if (_frameReader != null)
		{
			await _frameReader.StopAsync();
			_frameReader.Dispose();
			_frameReader = null;
		}

		_mediaCapture?.Dispose();
		_mediaCapture = null;
	}

	public override async Task StartPreviewAsync()
	{
		if (_mediaCapture == null)
		{
			await InitializeAsync();
		}

		IsPreviewing = true;

		_frameReader.FrameArrived += (sender, _) =>
		{
			if (!IsPreviewing)
			{
				return;
			}

			using var frame = sender.TryAcquireLatestFrame();
			if (frame == null)
			{
				return;
			}

			ProcessFrame(frame);
		};

		var status = await _frameReader.StartAsync();
		if (status != MediaFrameReaderStartStatus.Success)
		{
			throw new InvalidOperationException($"Failed to start frame reader: {status}");
		}
	}

	public override async Task StartRecordingAsync(string outputPath)
	{
		if (_mediaCapture == null)
		{
			await InitializeAsync();
		}

		if (IsRecording)
		{
			throw new InvalidOperationException("Recording is already in progress.");
		}

		StorageFile outputFile;

		try
		{
			// Convert the string path to a StorageFile
			outputFile = await StorageFile.GetFileFromPathAsync(outputPath);
		}
		catch (Exception)
		{
			// If the file doesn't exist, create it
			var fileName = Path.GetFileName(outputPath);
			var directory = Path.GetDirectoryName(outputPath);
			var folder = await StorageFolder.GetFolderFromPathAsync(directory);
			outputFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
		}

		// Create an MP4 profile with both video and audio
		var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
		profile.Audio = AudioEncodingProperties.CreateAac(44100, 2, 128000);

		// Start recording with the updated profile
		await _mediaCapture?.StartRecordToStorageFileAsync(profile, outputFile);

		IsRecording = true;
	}

	public override async Task StopPreviewAsync()
	{
		IsPreviewing = false;

		if (_frameReader != null)
		{
			await _frameReader.StopAsync();
		}
	}

	public override async Task StopRecordingAsync()
	{
		if (IsRecording)
		{
			await _mediaCapture.StopRecordAsync();
			IsRecording = false;
		}
	}

	private async Task InitializeAsync()
	{
		_mediaCapture = new MediaCapture();

		var settings = new MediaCaptureInitializationSettings
		{
			StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo,
			MediaCategory = MediaCategory.Media
		};

		// Use this because the code below is not guaranteed to find the default audio adapter
		var defaultAudioDeviceId = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default);
		if (!string.IsNullOrEmpty(defaultAudioDeviceId))
		{
			settings.AudioDeviceId = defaultAudioDeviceId;
		}
		else
		{
			// Could not find default, so fall back to the alternative
			var audioDevices = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioCaptureSelector());
			if (audioDevices.Count > 0)
			{
				var device = audioDevices.FirstOrDefault(x => x.IsDefault && x.IsEnabled)
					?? audioDevices.FirstOrDefault(x => x.IsEnabled);

				if (device != null)
				{
					settings.AudioDeviceId = device.Id;
				}
			}
		}

		try
		{
			await _mediaCapture.InitializeAsync(settings);
		}
		catch (UnauthorizedAccessException)
		{
			throw new InvalidOperationException("Camera access denied. Please enable camera permissions in Settings > Privacy > Camera.");
		}

		var frameSource = _mediaCapture.FrameSources.FirstOrDefault(fs => fs.Value.Info.MediaStreamType == MediaStreamType.VideoPreview).Value
			?? _mediaCapture.FrameSources.FirstOrDefault(fs => fs.Value.Info.MediaStreamType == MediaStreamType.VideoRecord).Value;

		if (frameSource == null)
		{
			throw new InvalidOperationException("No suitable video stream available on this device.");
		}

		_frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.Nv12);
	}

	private void ProcessFrame(MediaFrameReference frame)
	{
		var videoFrame = frame.VideoMediaFrame;
		if (videoFrame == null)
		{
			Console.WriteLine("VideoMediaFrame is null.");
			return;
		}

		var softwareBitmap = videoFrame.SoftwareBitmap;
		if ((softwareBitmap == null) && (videoFrame.Direct3DSurface != null))
		{
			softwareBitmap = SoftwareBitmap.CreateCopyFromSurfaceAsync(videoFrame.Direct3DSurface).AwaitResults();
		}

		if (softwareBitmap == null)
		{
			Console.WriteLine("SoftwareBitmap is still null after conversion attempt.");
			return;
		}

		var finalBitmap = softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8
			? SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore)
			: softwareBitmap;

		if (Frame is not WriteableBitmap
			|| !Frame.Size.Height.ComparePrecision(finalBitmap.PixelHeight)
			|| !Frame.Size.Width.ComparePrecision(finalBitmap.PixelWidth))
		{
			Frame?.Dispose();
			Frame = new WriteableBitmap(
				new PixelSize(finalBitmap.PixelWidth, finalBitmap.PixelHeight),
				new Vector(96, 96),
				PixelFormat.Bgra8888,
				AlphaFormat.Premul);
		}

		using (var lockedBitmap = ((WriteableBitmap) Frame).Lock())
		{
			var width = finalBitmap.PixelWidth;
			var height = finalBitmap.PixelHeight;
			var bufferSize = width * height * 4;

			if ((_pixelData == null) || (_pixelData.Length != bufferSize))
			{
				_pixelData = new byte[bufferSize];
			}

			finalBitmap.CopyToBuffer(_pixelData.AsBuffer());
			Marshal.Copy(_pixelData, 0, lockedBitmap.Address, bufferSize);
		}

		if (finalBitmap != softwareBitmap)
		{
			finalBitmap.Dispose();
		}

		if (softwareBitmap != videoFrame.SoftwareBitmap)
		{
			softwareBitmap.Dispose();
		}

		OnPropertyChanged(nameof(Frame));
	}

	#endregion
}