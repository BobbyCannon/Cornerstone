#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Presentation;
using Cornerstone;

#endregion

namespace Cornerstone.Avalonia.Camera;

internal abstract class BaseCameraAdapter : CornerstoneObject, ICameraAdapter, IDispatchable
{
	#region Fields

	private byte[] _capturedData;
	private Bitmap _frame;
	private bool _isNativeSurfaceVisible = true;
	private bool _isPreviewing;
	private bool _isRecording;
	private CameraMode _mode;
	private readonly IDispatcher _dispatcher;

	#endregion

	#region Constructors

	protected BaseCameraAdapter(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher ?? CornerstoneApplication.CornerstoneDispatcher;
	}

	#endregion

	#region Properties

	public abstract IPresentationList<CameraMode> AvailableModes { get; }

	public byte[] CapturedData
	{
		get => _capturedData;
		protected set
		{
			if (ReferenceEquals(_capturedData, value))
			{
				return;
			}

			_capturedData = value;
			NotifyComputedPropertyChanged(nameof(CapturedData));
		}
	}

	public Bitmap Frame
	{
		get => _frame;
		protected set
		{
			if (ReferenceEquals(_frame, value))
			{
				return;
			}

			_frame = value;
			NotifyComputedPropertyChanged(nameof(Frame));
		}
	}

	/// <inheritdoc />
	public bool IsNativeSurfaceVisible
	{
		get => _isNativeSurfaceVisible;
		private set
		{
			if (_isNativeSurfaceVisible == value)
			{
				return;
			}

			_isNativeSurfaceVisible = value;
			NotifyComputedPropertyChanged(nameof(IsNativeSurfaceVisible));
		}
	}

	public bool IsPreviewing
	{
		get => _isPreviewing;
		protected set
		{
			if (_isPreviewing == value)
			{
				return;
			}

			_isPreviewing = value;
			NotifyComputedPropertyChanged(nameof(IsPreviewing));
		}
	}

	public bool IsRecording
	{
		get => _isRecording;
		protected set
		{
			if (_isRecording == value)
			{
				return;
			}

			_isRecording = value;
			NotifyComputedPropertyChanged(nameof(IsRecording));
		}
	}

	/// <inheritdoc />
	public CameraMode Mode
	{
		get => _mode;
		set
		{
			if (_mode == value)
			{
				return;
			}

			_mode = value;
			NotifyComputedPropertyChanged(nameof(Mode));
		}
	}

	public abstract IPlatformHandle PlatformHandle { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		try
		{
			if (Frame == null)
			{
				return Task.FromResult(NativeSurfaceSnapshot.Failed("Camera frame is not available."));
			}

			using var stream = new MemoryStream();
			Frame.Save(stream);
			var png = stream.ToArray();
			var width = Math.Max(1, (int) Frame.Size.Width);
			var height = Math.Max(1, (int) Frame.Size.Height);
			return Task.FromResult(NativeSurfaceSnapshotHelper.ProcessPng(png, width, height, options));
		}
		catch (Exception ex)
		{
			return Task.FromResult(NativeSurfaceSnapshot.Failed(ex.Message));
		}
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		return false;
	}

	public virtual void HandleResize(int width, int height, float scaling)
	{
	}

	/// <inheritdoc />
	public virtual void SetNativeSurfaceVisible(bool visible)
	{
		// Logical flag only — do not stop recording when the host freezes the surface for overlays.
		IsNativeSurfaceVisible = visible;
	}

	public virtual Task StartPreviewAsync()
	{
		return Task.CompletedTask;
	}

	public virtual Task StartRecordingAsync(string outputFile)
	{
		return Task.CompletedTask;
	}

	public virtual Task StopPreviewAsync()
	{
		ClearPreviewFrame();
		return Task.CompletedTask;
	}

	public virtual Task StopRecordingAsync()
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Drops the last preview bitmap so the UI does not keep a frozen frame after stop.
	/// </summary>
	protected void ClearPreviewFrame()
	{
		var previous = Frame;
		if (previous == null)
		{
			return;
		}

		Frame = null;
		previous.Dispose();
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			ClearPreviewFrame();
		}
	}

	#endregion
}

public interface ICameraAdapter : INotifyPropertyChanged, IDisposable, IPausableNativeSurface
{
	#region Properties

	IPresentationList<CameraMode> AvailableModes { get; }

	byte[] CapturedData { get; }

	Bitmap Frame { get; }

	bool IsPreviewing { get; }

	bool IsRecording { get; }

	CameraMode Mode { get; set; }

	#endregion

	#region Methods

	bool HandleKeyDown(Key key, KeyModifiers keyModifiers);

	Task StartPreviewAsync();

	Task StartRecordingAsync(string outputFile);

	Task StopPreviewAsync();

	Task StopRecordingAsync();

	#endregion
}