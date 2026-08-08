#region References

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AVFoundation;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Platforms.iOS;
using Foundation;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using UIKit;

#endregion

namespace Cornerstone.Avalonia.Platforms.iOS;

internal class CameraAdapter : BaseCameraAdapter
{
	#region Constants

	private const ulong _maxDurationReachedErrorCode = 0xffffffffffffd1de;

	#endregion

	#region Fields

	private AVCaptureDeviceInput _audioInput;

	private RecordingDelegate _callback;
	private DispatchQueue _captureQueue;
	private AVCaptureSession _captureSession;
	private volatile Bitmap _currentFrame;
	private bool _isDisposed;
	private AVCaptureMovieFileOutput _movieOutput;
	private readonly SampleBufferDelegate _sampleBufferDelegate;
	private AVCaptureDeviceInput _videoInput;
	private AVCaptureVideoDataOutput _videoOutput;

	#endregion

	#region Constructors

	public CameraAdapter(IDispatcher dispatcher) : base(dispatcher)
	{
		_sampleBufferDelegate = new SampleBufferDelegate(this);

		AvailableModes = new PresentationList<CameraMode>(dispatcher) { CameraMode.Video, CameraMode.Image };

		Initialize();
		InitializeCaptureSession();
		RequestCameraPermission();
	}

	#endregion

	#region Properties

	public override IPresentationList<CameraMode> AvailableModes { get; }

	#endregion

	#region Methods

	private void Initialize()
	{
		UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
		NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, _ => { UpdateVideoOrientation(); });
	}

	/// <inheritdoc />
	public override IPlatformHandle PlatformHandle { get; }

	public override async Task StartPreviewAsync()
	{
		if (IsPreviewing || _isDisposed)
		{
			return;
		}

		await EnsurePermissionAsync();

		if (!_captureSession.Running)
		{
			try
			{
				_captureSession.StartRunning();
				UpdateVideoOrientation();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to start capture session: {ex.Message}");
				throw;
			}
		}
		else
		{
			Console.WriteLine("Capture session already running.");
		}

		IsPreviewing = true;
	}

	private void UpdateVideoOrientation()
	{
		// Set video orientation to Portrait
		var connection = _videoOutput?.ConnectionFromMediaType(new NSString("vide"));
		if (connection == null)
		{
			return;
		}

		var interfaceOrientation = UIInterfaceOrientation.Portrait;

		if (IOSPlatform.IsVersionOrHigher(18))
		{
			#pragma warning disable CA1422
			// Get the current UI orientation from the key window's scene
			var windowScene = UIApplication.SharedApplication.Windows.FirstOrDefault()?.WindowScene;
			interfaceOrientation = windowScene?.InterfaceOrientation ?? UIInterfaceOrientation.Portrait;
			#pragma warning restore CA1422
		}

		if (IOSPlatform.IsVersionOrHigher(17))
		{
			var rotationAngle = interfaceOrientation switch
			{
				UIInterfaceOrientation.Portrait => 90,
				// Connector Left, Camera Right
				UIInterfaceOrientation.LandscapeLeft => 180,
				UIInterfaceOrientation.PortraitUpsideDown => 180,
				// Camera Left, Connector Right
				UIInterfaceOrientation.LandscapeRight => 0,
				_ => 90
			};

			if (connection.IsVideoRotationAngleSupported(rotationAngle))
			{
				connection.VideoRotationAngle = rotationAngle;
			}
		}
		else
		{
			var orientation = interfaceOrientation switch
			{
				UIInterfaceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
				UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
				UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
				UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
				_ => AVCaptureVideoOrientation.Portrait
			};
			#pragma warning disable CA1422
			if (connection.SupportsVideoOrientation)
			{
				connection.VideoOrientation = orientation;
			}
			#pragma warning restore CA1422
		}
	}

	public override async Task StartRecordingAsync(string outputFile)
	{
		if (IsRecording || _isDisposed)
		{
			return;
		}

		await EnsurePermissionAsync();
		if (!_captureSession.Running)
		{
			_captureSession.StartRunning();
		}

		var fileUrl = NSUrl.FromFilename(outputFile);
		_callback = new RecordingDelegate(this);
		_movieOutput.StartRecordingToOutputFile(fileUrl, _callback);

		IsRecording = true;
	}

	public override Task StopPreviewAsync()
	{
		if (!IsPreviewing || _isDisposed)
		{
			return Task.CompletedTask;
		}

		if (_captureSession.Running)
		{
			_captureSession.StopRunning();
		}

		IsPreviewing = false;
		return base.StopPreviewAsync();
	}

	public override Task StopRecordingAsync()
	{
		if (!IsRecording || _isDisposed)
		{
			return Task.CompletedTask;
		}

		_movieOutput.StopRecording();
		IsRecording = false;
		return Task.CompletedTask;
	}

	protected override void Dispose(bool disposing)
	{
		if (_isDisposed)
		{
			return;
		}

		if (disposing)
		{
			if (_captureSession?.Running ?? false)
			{
				_captureSession.StopRunning();
			}

			_movieOutput?.Dispose();
			_videoOutput?.Dispose();
			_videoInput?.Dispose();
			_captureSession?.Dispose();
			_captureQueue?.Dispose();
			_currentFrame?.Dispose();

			_movieOutput = null;
			_videoOutput = null;
			_videoInput = null;
			_captureSession = null;
			_captureQueue = null;
			_currentFrame = null;
			CapturedData = null;
		}

		_isDisposed = true;

		base.Dispose(disposing);
	}

	private async Task EnsurePermissionAsync()
	{
		var videoStatus = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);
		if (videoStatus != AVAuthorizationStatus.Authorized)
		{
			var granted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
			if (!granted)
			{
				throw new UnauthorizedAccessException("Camera access was not granted.");
			}
		}

		// Recording binds an audio capture device; request mic when not yet determined/authorized.
		var audioStatus = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Audio);
		if (audioStatus != AVAuthorizationStatus.Authorized)
		{
			var granted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Audio);
			if (!granted)
			{
				throw new UnauthorizedAccessException("Microphone access was not granted.");
			}
		}
	}

	// ReSharper disable HeuristicUnreachableCode
	private void InitializeCaptureSession()
	{
		_captureSession = new AVCaptureSession
		{
			SessionPreset = AVCaptureSession.Preset640x480
		};

		_captureQueue = new DispatchQueue("CameraCaptureQueue");

		var device = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
		if (device == null)
		{
			throw new InvalidOperationException("No video capture device available.");
		}

		var audioDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio);
		if (audioDevice == null)
		{
			throw new InvalidOperationException("No audio capture device available.");
		}

		_audioInput = new AVCaptureDeviceInput(audioDevice, out var audioError);
		if (audioError != null)
		{
			throw new InvalidOperationException($"Failed to initialize audio input: {audioError.LocalizedDescription}");
		}

		_videoInput = new AVCaptureDeviceInput(device, out var error);
		if (error != null)
		{
			throw new InvalidOperationException($"Failed to initialize video input: {error.LocalizedDescription}");
		}

		if (_captureSession.CanAddInput(_audioInput))
		{
			_captureSession.AddInput(_audioInput);
		}

		if (_captureSession.CanAddInput(_videoInput))
		{
			_captureSession.AddInput(_videoInput);
		}

		// Setup video output for preview
		_videoOutput = new AVCaptureVideoDataOutput
		{
			AlwaysDiscardsLateVideoFrames = true
		};

		var videoSettings = new NSDictionary(
			CVPixelBuffer.PixelFormatTypeKey,
			NSNumber.FromInt32((int) CVPixelFormatType.CV420YpCbCr8BiPlanarFullRange)
		);

		_videoOutput.WeakVideoSettings = videoSettings;
		_videoOutput.SetSampleBufferDelegate(_sampleBufferDelegate, _captureQueue);

		if (_captureSession.CanAddOutput(_videoOutput))
		{
			_captureSession.AddOutput(_videoOutput);
		}

		// Setup movie file output for recording
		_movieOutput = new AVCaptureMovieFileOutput();

		// Max Duration
		_movieOutput.MaxRecordedDuration = new CMTime(30, 1);

		if (_captureSession.CanAddOutput(_movieOutput))
		{
			try
			{
				// Add the movie output to the session to create a valid connection
				_captureSession.AddOutput(_movieOutput);

				// Get the connection for the movie output
				var connection = _movieOutput.ConnectionFromMediaType(new NSString("vide"));
				if (connection != null)
				{
					// Configure output settings to force H.264
					var outputSettings = new NSMutableDictionary();
					outputSettings[AVVideo.CodecKey] = AVVideoCodecType.H264.GetConstant();

					// Optional: Set compression properties for better control
					var compressionSettings = new NSMutableDictionary();
					compressionSettings[AVVideo.AverageBitRateKey] = NSNumber.FromInt32(2000000);
					compressionSettings[AVVideo.MaxKeyFrameIntervalKey] = NSNumber.FromInt32(30);
					outputSettings[AVVideo.CompressionPropertiesKey] = compressionSettings;

					try
					{
						// Apply output settings using the movie output's connection
						_movieOutput.SetOutputSettings(outputSettings, connection);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Failed to apply H.264 settings: {ex.Message}");
					}
				}
				else
				{
					Console.WriteLine("Failed to get video connection for movie output.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to configure movie output: {ex.Message}");
			}
		}

		_captureSession.CommitConfiguration();
	}

	private void ProcessSampleBuffer(CMSampleBuffer sampleBuffer)
	{
		if (_isDisposed || (sampleBuffer == null))
		{
			return;
		}

		using var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
		if (pixelBuffer == null)
		{
			return;
		}

		pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);

		try
		{
			var width = (int) pixelBuffer.Width;
			var height = (int) pixelBuffer.Height;
			var yBaseAddress = pixelBuffer.GetBaseAddress(0);
			var uvBaseAddress = pixelBuffer.GetBaseAddress(1);

			if ((yBaseAddress == nint.Zero) || (uvBaseAddress == nint.Zero))
			{
				return;
			}

			// Convert YUV to RGBA
			var rgbaData = ConvertFrame(pixelBuffer, width, height);
			if (rgbaData == null)
			{
				return;
			}

			// Create Bitmap for Avalonia
			this.Dispatch(() =>
			{
				if (_isDisposed)
				{
					return;
				}

				try
				{
					// Create WriteableBitmap for raw RGBA pixels
					var pixelFormat = PixelFormats.Rgba8888; // Use Bgra8888 if RGBA fails
					var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), pixelFormat);
					using (var buffer = bitmap.Lock())
					{
						Marshal.Copy(rgbaData, 0, buffer.Address, rgbaData.Length);
					}

					var oldFrame = Frame;
					Frame = bitmap;
					oldFrame?.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to create WriteableBitmap: {ex.Message}");
				}
			});
		}
		finally
		{
			pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
		}
	}

	/// <summary>
	/// ConvertYUVToRGBA
	/// </summary>
	private byte[] ConvertFrame(CVPixelBuffer pixelBuffer, int width, int height)
	{
		try
		{
			var yPlane = pixelBuffer.GetBaseAddress(0);
			var uvPlane = pixelBuffer.GetBaseAddress(1);
			var yBytesPerRow = pixelBuffer.GetBytesPerRowOfPlane(0);
			var uvBytesPerRow = pixelBuffer.GetBytesPerRowOfPlane(1);

			var yData = new byte[height * yBytesPerRow];
			var uvData = new byte[(height / 2) * uvBytesPerRow];

			Marshal.Copy(new IntPtr(yPlane), yData, 0, yData.Length);
			Marshal.Copy(new IntPtr(uvPlane), uvData, 0, uvData.Length);

			var rgbaData = new byte[width * height * 4];

			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var yIndex = (y * yBytesPerRow) + x;
					var uvIndex = ((y / 2) * uvBytesPerRow) + ((x / 2) * 2);

					float dY = yData[yIndex];
					float dU = uvData[uvIndex] - 128;
					float dV = uvData[uvIndex + 1] - 128;

					// YUV to RGB conversion (ITU-R BT.601)
					var dR = dY + (1.402f * dV);
					var dG = dY - (0.344f * dU) - (0.714f * dV);
					var dB = dY + (1.772f * dU);

					// Clamp to [0, 255]
					var r = Math.Clamp((int) dR, 0, 255);
					var g = Math.Clamp((int) dG, 0, 255);
					var b = Math.Clamp((int) dB, 0, 255);

					var rgbaIndex = ((y * width) + x) * 4;
					rgbaData[rgbaIndex] = (byte) r; // R
					rgbaData[rgbaIndex + 1] = (byte) g; // G
					rgbaData[rgbaIndex + 2] = (byte) b; // B
					rgbaData[rgbaIndex + 3] = 255; // A
				}
			}

			return rgbaData;
		}
		catch
		{
			return null;
		}
	}

	private void RequestCameraPermission()
	{
		AVCaptureDevice.RequestAccessForMediaType(AVAuthorizationMediaType.Video,
			granted =>
			{
				if (!granted)
				{
					throw new UnauthorizedAccessException("Camera access was not granted.");
				}
			}
		);
	}

	#endregion

	#region Classes

	public class RecordingDelegate : AVCaptureFileOutputRecordingDelegate
	{
		#region Fields

		private readonly CameraAdapter _adapter;

		#endregion

		#region Constructors

		public RecordingDelegate(CameraAdapter adapter)
		{
			_adapter = adapter;
		}

		#endregion

		#region Methods

		public override void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError error)
		{
			try
			{
				if ((error != null) && (error.Code != unchecked((nint) _maxDurationReachedErrorCode)))
				{
					Console.WriteLine($"Recording failed: {error.LocalizedDescription} (Code: {error.Code})");
					_adapter.CapturedData = Array.Empty<byte>();
					_adapter.IsRecording = false;
					return;
				}

				if (!File.Exists(outputFileUrl.Path))
				{
					Console.WriteLine("Recording failed: Output file URL is invalid or file does not exist.");
					_adapter.CapturedData = Array.Empty<byte>();
					_adapter.IsRecording = false;
					return;
				}

				var data = File.ReadAllBytes(outputFileUrl.Path);
				_adapter.CapturedData = data;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error processing recorded file: {ex.Message}");
				_adapter.CapturedData = Array.Empty<byte>();
				_adapter.IsRecording = false;
			}
			finally
			{
				new FileInfo(outputFileUrl.Path).SafeDelete();
			}
		}

		#endregion
	}

	private class SampleBufferDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
	{
		#region Fields

		private readonly CameraAdapter _adapter;

		#endregion

		#region Constructors

		public SampleBufferDelegate(CameraAdapter adapter)
		{
			_adapter = adapter;
		}

		#endregion

		#region Methods

		public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
		{
			try
			{
				_adapter.ProcessSampleBuffer(sampleBuffer);
			}
			finally
			{
				sampleBuffer.Dispose();
			}
		}

		#endregion
	}

	#endregion
}