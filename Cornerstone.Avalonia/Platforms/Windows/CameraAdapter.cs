#region References

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

[SourceReflection]
[SupportedOSPlatform("Windows")]
internal class CameraAdapter : BaseCameraAdapter
{
	#region Fields

	/// <summary> Target pixel count for preview (~720p). Lower = better UI performance. </summary>
	private const int PreferredPreviewPixels = 1280 * 720;

	private MediaFrameReader _frameReader;
	private TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> _frameArrivedHandler;
	/// <summary> 0 = free to accept a frame; 1 = UI still applying the previous frame (drop new ones). </summary>
	private int _frameApplyPending;
	private int _frameHeight;
	private int _frameWidth;
	/// <summary> Bumped on stop so late DispatchPost applies are ignored after clear. </summary>
	private int _previewGeneration;
	private MediaCapture _mediaCapture;
	private StorageFile _outputFile;
	private byte[] _pixelData;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public CameraAdapter(IDispatcher dispatcher) : base(dispatcher)
	{
		AvailableModes = new PresentationList<CameraMode>(dispatcher) { CameraMode.Video };
	}

	#endregion

	#region Properties

	public override IPresentationList<CameraMode> AvailableModes { get; }

	/// <inheritdoc />
	public override IPlatformHandle PlatformHandle { get; }

	#endregion

	#region Methods

	public override async Task StartPreviewAsync()
	{
		if (_mediaCapture == null)
		{
			await InitializeAsync();
		}

		IsPreviewing = true;
		Interlocked.Exchange(ref _frameApplyPending, 0);

		// Process pixels off the UI thread; drop if UI is busy; DispatchPost only the bitmap write.
		_frameArrivedHandler ??= OnFrameArrived;
		_frameReader.FrameArrived -= _frameArrivedHandler;
		_frameReader.FrameArrived += _frameArrivedHandler;

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

		try
		{
			_outputFile = await StorageFile.GetFileFromPathAsync(outputPath);
		}
		catch (Exception)
		{
			var fileName = Path.GetFileName(outputPath);
			var directory = Path.GetDirectoryName(outputPath);
			var folder = await StorageFolder.GetFolderFromPathAsync(directory);
			_outputFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
		}

		var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
		profile.Audio = AudioEncodingProperties.CreateAac(44100, 2, 128000);

		try
		{
			await _mediaCapture.StartRecordToStorageFileAsync(profile, _outputFile);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("The camera is unavailable or in use by another application.", ex);
		}

		IsRecording = true;
	}

	public override async Task StopPreviewAsync()
	{
		IsPreviewing = false;
		Interlocked.Increment(ref _previewGeneration);
		Interlocked.Exchange(ref _frameApplyPending, 0);

		if (_frameReader != null)
		{
			if (_frameArrivedHandler != null)
			{
				_frameReader.FrameArrived -= _frameArrivedHandler;
			}

			await _frameReader.StopAsync();
		}

		await base.StopPreviewAsync();

		this.DispatchPost(() =>
		{
			ClearPreviewFrame();
			NotifyComputedPropertyChanged(nameof(IsPreviewing));
		}, DispatcherPriority.Render);
	}

	public override async Task StopRecordingAsync()
	{
		if (IsRecording)
		{
			await _mediaCapture.StopRecordAsync();
			IsRecording = false;

			if (_outputFile.IsAvailable)
			{
				using (var stream = await _outputFile.OpenReadAsync())
				using (var memoryStream = new MemoryStream())
				{
					await stream.AsStreamForRead().CopyToAsync(memoryStream);
					CapturedData = memoryStream.ToArray();
				}
			}
		}
	}

	protected override async void Dispose(bool disposing)
	{
		IsPreviewing = false;

		if (IsRecording)
		{
			await StopRecordingAsync();
		}

		if (_frameReader != null)
		{
			if (_frameArrivedHandler != null)
			{
				_frameReader.FrameArrived -= _frameArrivedHandler;
			}

			await _frameReader.StopAsync();
			_frameReader.Dispose();
			_frameReader = null;
		}

		if (_mediaCapture != null)
		{
			_mediaCapture.CaptureDeviceExclusiveControlStatusChanged -= OnExclusiveControlStatusChanged;
			_mediaCapture.Dispose();
			_mediaCapture = null;
		}

		base.Dispose(disposing);
	}

	private async Task InitializeAsync()
	{
		_mediaCapture = new MediaCapture();

		var settings = new MediaCaptureInitializationSettings
		{
			StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo,
			MediaCategory = MediaCategory.Media
		};

		var defaultAudioDeviceId = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default);
		if (!string.IsNullOrEmpty(defaultAudioDeviceId))
		{
			settings.AudioDeviceId = defaultAudioDeviceId;
		}
		else
		{
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
			_mediaCapture.CaptureDeviceExclusiveControlStatusChanged += OnExclusiveControlStatusChanged;
		}
		catch (UnauthorizedAccessException)
		{
			_mediaCapture.Dispose();
			_mediaCapture = null;
			throw new InvalidOperationException("Camera access denied. Please enable camera permissions in Settings > Privacy > Camera.");
		}
		catch (Exception ex)
		{
			_mediaCapture.Dispose();
			_mediaCapture = null;
			throw new InvalidOperationException("The camera is unavailable or in use by another application.", ex);
		}

		var frameSource = _mediaCapture.FrameSources.FirstOrDefault(fs => fs.Value.Info.MediaStreamType == MediaStreamType.VideoPreview).Value
			?? _mediaCapture.FrameSources.FirstOrDefault(fs => fs.Value.Info.MediaStreamType == MediaStreamType.VideoRecord).Value;

		if (frameSource == null)
		{
			throw new InvalidOperationException("No suitable video stream available on this device.");
		}

		// Prefer a modest resolution (near 720p) for UI preview — full sensor is too heavy for Avalonia Image.
		var format = SelectPreviewFormat(frameSource);
		if (format != null)
		{
			await frameSource.SetFormatAsync(format);
		}

		// Prefer BGRA so we avoid NV12→BGRA convert every frame.
		var subtype = format?.Subtype;
		if (string.IsNullOrEmpty(subtype)
			|| (!string.Equals(subtype, MediaEncodingSubtypes.Bgra8, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(subtype, MediaEncodingSubtypes.Argb32, StringComparison.OrdinalIgnoreCase)))
		{
			subtype = MediaEncodingSubtypes.Bgra8;
		}

		try
		{
			_frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource, subtype);
		}
		catch
		{
			// Fall back to NV12 if BGRA is unavailable for this device.
			_frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.Nv12);
		}
	}

	private async void OnExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
	{
		if (args.Status != MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
		{
			return;
		}

		if (IsRecording)
		{
			await StopRecordingAsync();
		}

		if (IsPreviewing)
		{
			await StopPreviewAsync();
		}
	}

	/// <summary>
	/// Pick a supported format closest to PreferredPreviewPixels (prefer BGRA, then NV12).
	/// </summary>
	private static MediaFrameFormat SelectPreviewFormat(MediaFrameSource frameSource)
	{
		var formats = frameSource.SupportedFormats;
		if ((formats == null) || (formats.Count == 0))
		{
			return null;
		}

		static int Score(MediaFrameFormat format)
		{
			var video = format.VideoFormat;
			if (video == null)
			{
				return int.MaxValue;
			}

			var pixels = (int) (video.Width * video.Height);
			var sizeScore = Math.Abs(pixels - PreferredPreviewPixels);
			// Prefer BGRA (no convert). Slight preference over NV12.
			var subtypePenalty = string.Equals(format.Subtype, MediaEncodingSubtypes.Bgra8, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(format.Subtype, MediaEncodingSubtypes.Argb32, StringComparison.OrdinalIgnoreCase)
					? 0
					: 50_000;
			return sizeScore + subtypePenalty;
		}

		return formats.OrderBy(Score).FirstOrDefault();
	}

	/// <summary>
	/// Copies BGRA pixels into the WriteableBitmap on the UI thread (row-stride aware).
	/// </summary>
	private void ApplyFrameOnUiThread(int generation)
	{
		try
		{
			if (!IsPreviewing
				|| (generation != Volatile.Read(ref _previewGeneration))
				|| (_pixelData == null)
				|| (_frameWidth <= 0)
				|| (_frameHeight <= 0))
			{
				return;
			}

			if (Frame is not WriteableBitmap writeable
				|| ((int) writeable.PixelSize.Width != _frameWidth)
				|| ((int) writeable.PixelSize.Height != _frameHeight))
			{
				Frame?.Dispose();
				// Opaque BGRA — camera frames are not premultiplied; wrong alpha format can look washed/odd.
				Frame = new WriteableBitmap(
					new PixelSize(_frameWidth, _frameHeight),
					new Vector(96, 96),
					PixelFormat.Bgra8888,
					AlphaFormat.Opaque);
				writeable = (WriteableBitmap) Frame;
			}

			using (var lockedBitmap = writeable.Lock())
			{
				var srcStride = _frameWidth * 4;
				var dstStride = lockedBitmap.RowBytes;
				var height = _frameHeight;

				if (dstStride == srcStride)
				{
					Marshal.Copy(_pixelData, 0, lockedBitmap.Address, srcStride * height);
				}
				else
				{
					// RowBytes may exceed width*4 — bulk copy would skew/distort the image.
					for (var y = 0; y < height; y++)
					{
						Marshal.Copy(
							_pixelData,
							y * srcStride,
							lockedBitmap.Address + (y * dstStride),
							srcStride);
					}
				}
			}

			// Same WriteableBitmap instance — notify so CameraView invalidates without rebinding Source.
			NotifyComputedPropertyChanged(nameof(Frame));
		}
		finally
		{
			Interlocked.Exchange(ref _frameApplyPending, 0);
		}
	}

	private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
	{
		if (!IsPreviewing)
		{
			return;
		}

		// Drop frames while a UI apply is outstanding so the dispatcher never queues a backlog.
		if (Interlocked.CompareExchange(ref _frameApplyPending, 1, 0) != 0)
		{
			using var ignored = sender.TryAcquireLatestFrame();
			return;
		}

		var generation = Volatile.Read(ref _previewGeneration);

		try
		{
			using var frame = sender.TryAcquireLatestFrame();
			if ((frame == null) || !IsPreviewing || !TryCopyFramePixels(frame))
			{
				Interlocked.Exchange(ref _frameApplyPending, 0);
				return;
			}

			this.DispatchPost(() => ApplyFrameOnUiThread(generation), DispatcherPriority.Render);
		}
		catch
		{
			Interlocked.Exchange(ref _frameApplyPending, 0);
			throw;
		}
	}

	/// <summary>
	/// Heavy pixel work on the frame-callback thread. Returns false if the frame cannot be used.
	/// </summary>
	private bool TryCopyFramePixels(MediaFrameReference frame)
	{
		var videoFrame = frame.VideoMediaFrame;
		if (videoFrame == null)
		{
			return false;
		}

		var softwareBitmap = videoFrame.SoftwareBitmap;
		var ownsSoftwareBitmap = false;

		if ((softwareBitmap == null) && (videoFrame.Direct3DSurface != null))
		{
			softwareBitmap = SoftwareBitmap.CreateCopyFromSurfaceAsync(videoFrame.Direct3DSurface)
				.AsTask()
				.GetAwaiter()
				.GetResult();
			ownsSoftwareBitmap = softwareBitmap != null;
		}

		if (softwareBitmap == null)
		{
			return false;
		}

		SoftwareBitmap finalBitmap = softwareBitmap;
		var ownsFinalBitmap = false;

		if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
		{
			finalBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
			ownsFinalBitmap = true;
		}

		try
		{
			var width = finalBitmap.PixelWidth;
			var height = finalBitmap.PixelHeight;
			if ((width <= 0) || (height <= 0))
			{
				return false;
			}

			var bufferSize = width * height * 4;
			if ((_pixelData == null) || (_pixelData.Length != bufferSize))
			{
				_pixelData = new byte[bufferSize];
			}

			finalBitmap.CopyToBuffer(_pixelData.AsBuffer());
			_frameWidth = width;
			_frameHeight = height;
			return true;
		}
		finally
		{
			if (ownsFinalBitmap)
			{
				finalBitmap.Dispose();
			}

			if (ownsSoftwareBitmap)
			{
				softwareBitmap.Dispose();
			}
		}
	}

	#endregion
}