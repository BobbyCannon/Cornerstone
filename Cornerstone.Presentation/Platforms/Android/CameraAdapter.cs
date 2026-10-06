#region References

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.Video;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Core.Util;
using AndroidX.Lifecycle;
using Cornerstone.Presentation.Android;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.Camera;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation;
using Object = Java.Lang.Object;
using Task = System.Threading.Tasks.Task;
using Debug = System.Diagnostics.Debug;
using Color = Android.Graphics.Color;
using View = Android.Views.View;
using Rect = Android.Graphics.Rect;
using Bitmap = Android.Graphics.Bitmap;
using Permission = Android.Content.PM.Permission;
using Cornerstone.Presentation.Controls.NativeHosts;

using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;
using DispatcherPriority = Cornerstone.Presentation.DispatcherPriority;
using AndroidApplication = Android.App.Application;

#endregion

namespace Cornerstone.Presentation.Platforms.Android;

internal class CameraAdapter : BaseCameraAdapter
{
	#region Fields

	private Recording _activeRecording;
	private ProcessCameraProvider _cameraProvider;
	private FrameLayout _hostView;
	private PendingRecording _pendingRecording;
	private IPlatformHandle _platformHandle;
	private PreviewView _previewView;
	private Recorder _recorder;
	private CancellationTokenSource _recordingToken;
	private VideoCapture _videoCapture;


#endregion

	#region Constructors

	public CameraAdapter(IDispatcher dispatcher) : base(dispatcher)
	{
		AvailableModes = new PresentationList<CameraMode>(dispatcher) { CameraMode.Video };

		// Publish AndroidViewControlHandle as soon as possible so CameraNativeHost can attach
		// PreviewView while the tab is open (before the user taps Start).
		if (AndroidHost.Activity != null)
		{
			EnsureHostCreated();
		}
	}


#endregion

	#region Properties

	public override IPresentationList<CameraMode> AvailableModes { get; }

	/// <inheritdoc />
	public override IPlatformHandle PlatformHandle => _platformHandle;


#endregion

	#region Methods

	public override async Task StartPreviewAsync()
	{
		EnsureCameraPermissions();
		await InitializeAsync(false).ConfigureAwait(true);
	}

	public override async Task StartRecordingAsync(string outputPath)
	{
		try
		{
			EnsureCameraPermissions();
			// Bind preview + VideoCapture once the PreviewView is laid out in Cornerstone.Presentation.
			await InitializeAsync(true).ConfigureAwait(true);

			var context = AndroidApplication.Context;
			var outputFile = new Java.IO.File(outputPath);
			var outputOptions = new FileOutputOptions.Builder(outputFile).Build();

			_pendingRecording = _recorder
				.PrepareRecording(context, outputOptions)
				.WithAudioEnabled();

			var recordingConsumer = new RecordingConsumer { OnRecordingEvent = OnRecordingEvent };
			_activeRecording = _pendingRecording.Start(ContextCompat.GetMainExecutor(context), recordingConsumer);
			_recordingToken = new CancellationTokenSource();

			await Task.Run(async () =>
			{
				try
				{
					// Start a task to stop recording after 30s
					await Task.Delay(30000, _recordingToken.Token);
					if (!_recordingToken.Token.IsCancellationRequested)
					{
						Debug.WriteLine("Maximum recording duration reached. Stopping recording.");
						await StopRecordingAsync();
					}
				}
				catch (TaskCanceledException)
				{
					Debug.WriteLine("Recording timeout task was cancelled.");
				}
			}, _recordingToken.Token);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Failed to start recording: {ex.Message}");
			throw;
		}
	}

	public override Task StopPreviewAsync()
	{
		IsPreviewing = false;
		_cameraProvider?.UnbindAll();
		// TextureView often keeps a frozen last frame after unbind. Hide it and paint the host
		// so Cornerstone's cover is not the only line of defense (z-order can still show native).
		if (_previewView != null)
		{
			_previewView.Visibility = ViewStates.Gone;
		}

		if (_hostView != null)
		{
			_hostView.SetBackgroundColor(Color.Rgb(0x1E, 0x1E, 0x1E));
		}

		return base.StopPreviewAsync();
	}

	public override Task StopRecordingAsync()
	{
		if (_activeRecording == null)
		{
			return Task.CompletedTask;
		}

		try
		{
			_activeRecording.Stop();
			_recordingToken?.Cancel();
			_recordingToken?.Dispose();
			_recordingToken = null;
			_activeRecording = null;
			_pendingRecording = null;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error stopping recording: {ex.Message}");
		}

		IsRecording = false;
		return Task.CompletedTask;
	}

	protected override void Dispose(bool disposing)
	{
		if (this.ShouldDispatch())
		{
			this.Dispatch(() => Dispose(disposing));
			return;
		}

		IsPreviewing = false;
		if (_cameraProvider != null)
		{
			_cameraProvider.UnbindAll();
			_cameraProvider.Dispose();
			_cameraProvider = null;
		}

		if (_previewView != null)
		{
			_previewView.Dispose();
			_previewView = null;
		}

		if (_hostView != null)
		{
			_hostView.Dispose();
			_hostView = null;
		}

		SetPlatformHandle(null);
		CapturedData = null;
		base.Dispose(disposing);
	}

	internal void SetImageData(int request, byte[] imageData)
	{
	}

	internal void SetQrCodeData(int request, byte[] imageData)
	{
	}

	internal void SetVideoData(int request, byte[] data)
	{
	}


	private void EnsureHostCreated()
	{
		if (_hostView != null)
		{
			return;
		}

		// Prefer Activity context so the view is compatible with CornerstoneView's window.
		// Preview surface only — record chrome lives in the host app (e.g. TabCamera toolbar).
		var context = (global::Android.Content.Context) AndroidHost.Activity
			?? AndroidApplication.Context;

		_hostView = new FrameLayout(context)
		{
			LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent)
		};

		_previewView = new PreviewView(context)
		{
			LayoutParameters = new FrameLayout.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent)
		};
		// TextureView path: required when hosting inside Cornerstone NativeControlHost.
		// SurfaceView (Performance) often never produces a surface → CameraX 5s timeout.
		_previewView.SetImplementationMode(PreviewView.ImplementationMode.Compatible);
		// FitCenter: one axis wins, aspect preserved (letterbox/pillarbox). Not FillCenter (crop).
		_previewView.SetScaleType(PreviewView.ScaleType.FitCenter);
		_hostView.SetBackgroundColor(Color.Black);
		_hostView.AddView(_previewView);

		// Publish immediately so CameraNativeHost can create the Cornerstone Android attachment
		// before Start/bind (IsCompatibleWith requires AndroidViewControlHandle).
		SetPlatformHandle(new AndroidViewControlHandle(_hostView));
	}

	/// <summary>
	/// Dangerous permissions must be granted at runtime (manifest alone is not enough).
	/// The host app should request CAMERA / RECORD_AUDIO before start; this fails fast with a clear error if not.
	/// </summary>
	private static void EnsureCameraPermissions()
	{
		var context = AndroidApplication.Context;
		if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.Camera) != Permission.Granted)
		{
			throw new UnauthorizedAccessException(
				"Camera permission was not granted. Declare android.permission.CAMERA and request it at runtime.");
		}

		if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.RecordAudio) != Permission.Granted)
		{
			throw new UnauthorizedAccessException(
				"Microphone permission was not granted. Declare android.permission.RECORD_AUDIO and request it at runtime.");
		}
	}

	/// <summary>
	/// Host the PreviewView in Cornerstone first, wait until it is laid out, then bind CameraX.
	/// Binding while the view is unattached/0×0 causes "Future is not done within 5000 ms".
	/// Never block the UI thread on ListenableFuture.Get — that deadlocks Cornerstone's dispatcher.
	/// </summary>
	private async Task InitializeAsync(bool record)
	{
		var context = AndroidApplication.Context;
		var activity = AndroidHost.Activity
			?? throw new InvalidOperationException("Android activity is not available for the camera.");

		if (activity is not ILifecycleOwner lifecycleOwner)
		{
			throw new InvalidOperationException(
				$"Activity {activity.GetType().FullName} does not implement ILifecycleOwner (required by CameraX).");
		}

		EnsureHostCreated();
		IsPreviewing = true;

		// Host must be attached by Cornerstone (AndroidViewControlHandle → CornerstoneView.AddView).
		await WaitForPreviewViewReadyAsync().ConfigureAwait(true);

		if (_cameraProvider == null)
		{
			_cameraProvider = await GetCameraProviderAsync(context).ConfigureAwait(true);
		}

		var preview = new Preview.Builder().Build();
		// SurfaceProvider queues until PreviewView has a surface — only after Cornerstone parents the view.
		preview.SetSurfaceProvider(ContextCompat.GetMainExecutor(context), _previewView.SurfaceProvider);

		var cameraSelector = new CameraSelector.Builder()
			.RequireLensFacing(CameraSelector.LensFacingBack)
			.Build();

		_cameraProvider.UnbindAll();

		if (record)
		{
			_recorder ??= new Recorder.Builder()
				.SetQualitySelector(QualitySelector.From(Quality.Lowest))
				.Build();
			_videoCapture ??= VideoCapture.WithOutput(_recorder);

			_cameraProvider.BindToLifecycle(
				lifecycleOwner,
				cameraSelector,
				preview,
				_videoCapture
			);
		}
		else
		{
			_cameraProvider.BindToLifecycle(
				lifecycleOwner,
				cameraSelector,
				preview
			);
		}

		if (_hostView != null)
		{
			// Black letterbox bars while live (FitCenter may not fill the host).
			_hostView.SetBackgroundColor(Color.Black);
		}

		if (_previewView != null)
		{
			_previewView.Visibility = ViewStates.Visible;
		}

		IsRecording = record;
		Debug.WriteLine(
			$"Camera bound (record={record}, preview={_previewView.Width}x{_previewView.Height}, " +
			$"attached={_previewView.IsAttachedToWindow}, parent={_hostView.Parent != null}).");
	}

	/// <summary>
	/// Resolves ProcessCameraProvider without blocking the UI thread on ListenableFuture.Get().
	/// </summary>
	private static Task<ProcessCameraProvider> GetCameraProviderAsync(global::Android.Content.Context context)
	{
		var tcs = new TaskCompletionSource<ProcessCameraProvider>(TaskCreationOptions.RunContinuationsAsynchronously);
		var future = ProcessCameraProvider.GetInstance(context);
		var executor = ContextCompat.GetMainExecutor(context);

		future.AddListener(new Java.Lang.Runnable(() =>
		{
			try
			{
				var provider = (ProcessCameraProvider) future.Get();
				if (provider == null)
				{
					tcs.TrySetException(new InvalidOperationException("ProcessCameraProvider is not available."));
					return;
				}

				tcs.TrySetResult(provider);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		}), executor);

		return tcs.Task;
	}

	/// <summary>
	/// CameraX needs PreviewView parented under CornerstoneView (via NativeControlHost) with a real size.
	/// Cornerstone's Android host calls AddView + ShowInBounds; IsAttachedToWindow alone is not enough.
	/// </summary>
	private async Task WaitForPreviewViewReadyAsync()
	{
		// Re-sync host visibility so NestedNativeHost recreates with AndroidViewControlHandle.
		NotifyComputedPropertyChanged(nameof(PlatformHandle));

		await Dispatcher.UIThread.InvokeAsync(
			() => { },
			DispatcherPriority.Loaded);

		const int maxAttempts = 120;
		for (var attempt = 0; attempt < maxAttempts; attempt++)
		{
			var parented = (_hostView?.Parent != null) || (_previewView?.Parent != null);
			var attached = _previewView?.IsAttachedToWindow ?? false;
			var width = _previewView?.Width ?? 0;
			var height = _previewView?.Height ?? 0;
			// After ShowInBounds, Width/Height should be > 1; Parent non-null means Cornerstone added the view.
			if (parented && attached && (width > 1) && (height > 1))
			{
				Debug.WriteLine($"Camera PreviewView ready after {attempt} polls ({width}x{height}).");
				return;
			}

			if (_previewView != null)
			{
				_previewView.RequestLayout();
				_hostView?.RequestLayout();
				_previewView.Invalidate();
			}

			await Task.Delay(50).ConfigureAwait(true);
			await Dispatcher.UIThread.InvokeAsync(
				() => { },
				DispatcherPriority.Render);
		}

		var finalParent = _hostView?.Parent != null;
		var finalAttached = _previewView?.IsAttachedToWindow ?? false;
		var finalW = _previewView?.Width ?? 0;
		var finalH = _previewView?.Height ?? 0;
		throw new TimeoutException(
			$"Camera PreviewView was not hosted by Cornerstone (parented={finalParent}, attached={finalAttached}, size={finalW}x{finalH}). " +
			"Ensure CameraNativeHost is visible and returns AndroidViewControlHandle from CreateNativeControlCore.");
	}

	private void OnRecordingEvent(VideoRecordEvent videoRecordEvent)
	{
		if (videoRecordEvent is not VideoRecordEvent.Finalize finalizeEvent)
		{
			return;
		}

		_pendingRecording = null;
		_recordingToken?.Cancel();
		_recordingToken?.Dispose();
		_recordingToken = null;

		if (finalizeEvent.HasError)
		{
			var error = finalizeEvent.Error;
			Debug.WriteLine($"Recording failed with error: {error}");
			if (error == VideoRecordEvent.Finalize.ErrorSourceInactive)
			{
				IsRecording = false;
			}
		}
		else
		{
			// Keep the file for the host (e.g. TabCamera shows the path). Capture bytes for API consumers.
			var path = finalizeEvent.OutputResults.OutputUri?.EncodedPath;
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				CapturedData = File.ReadAllBytes(path);
			}

			IsRecording = false;
		}
	}

	private void SetPlatformHandle(IPlatformHandle handle)
	{
		if (ReferenceEquals(_platformHandle, handle))
		{
			return;
		}

		_platformHandle = handle;
		NotifyComputedPropertyChanged(nameof(PlatformHandle));
	}


#endregion

	#region Classes


	private class RecordingConsumer : Object, IConsumer
	{
		#region Properties

		public Action<VideoRecordEvent> OnRecordingEvent { get; set; }


#endregion

		#region Methods

		/// <inheritdoc />
		public void Accept(Object t)
		{
			OnRecordingEvent?.Invoke((VideoRecordEvent) t);
		}


#endregion
	}


#endregion
}
