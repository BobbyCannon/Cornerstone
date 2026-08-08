#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.LogicalTree;
using Cornerstone;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Presentation;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

#endregion

namespace Cornerstone.Avalonia.Camera;

public partial class CameraView : CornerstoneUserControl, INativeHostPausable
{
	#region Fields

	public static readonly StyledProperty<bool> IsPausedProperty =
		AvaloniaProperty.Register<CameraView, bool>(nameof(IsPaused));

	public static readonly StyledProperty<CameraMode> ModeProperty =
		AvaloniaProperty.Register<CameraView, CameraMode>(nameof(Mode), CameraMode.Video);

	#endregion

	#region Constructors

	public CameraView()
	{
		CameraAdapter = GetInstance<ICameraAdapter>();
		WeakEventManager.AddPropertyChanged(CameraAdapter, this, AdapterOnPropertyChanged);

		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public IPresentationList<CameraMode> AvailableModes => CameraAdapter.AvailableModes;

	public ICameraAdapter CameraAdapter { get; set; }

	public byte[] CapturedData => CameraAdapter.CapturedData;

	public Bitmap Frame => CameraAdapter.Frame;

	/// <summary>
	/// When true, freezes the camera surface as a snapshot underlay and hides the native host
	/// so Avalonia content can paint over this region. Does not stop an in-progress recording.
	/// </summary>
	public bool IsPaused
	{
		get => GetValue(IsPausedProperty);
		set => SetValue(IsPausedProperty, value);
	}

	public bool IsPreviewing => CameraAdapter.IsPreviewing;

	public bool IsRecording => CameraAdapter.IsRecording;

	public CameraMode Mode
	{
		get => GetValue(ModeProperty);
		set => SetValue(ModeProperty, value);
	}

	#endregion

	#region Methods

	public async Task StartAsync()
	{
		if (CameraAdapter.IsPreviewing
			|| CameraAdapter.IsRecording)
		{
			return;
		}

		IsPaused = false;
		await CameraAdapter.StartPreviewAsync();
	}

	/// <summary>
	/// Starts recording to a temp MP4 path (or the given path). Preview should already be running.
	/// </summary>
	public async Task StartRecordingAsync(string outputPath = null)
	{
		if (CameraAdapter.IsRecording)
		{
			return;
		}

		if (!CameraAdapter.IsPreviewing)
		{
			await StartAsync();
		}

		outputPath ??= Path.Combine(Path.GetTempPath(), $"Camera_{Guid.NewGuid()}.mp4");
		await CameraAdapter.StartRecordingAsync(outputPath);
	}

	public async Task StopAsync()
	{
		IsPaused = false;

		if (CameraAdapter.IsRecording)
		{
			await CameraAdapter.StopRecordingAsync();
		}

		await CameraAdapter.StopPreviewAsync();
		// Clear Avalonia Image; stopped cover is bound to !IsPreviewing in XAML.
		ClearPreviewDisplay();
		// Ensure bindings refresh even if a late frame notification races with stop.
		OnPropertyChanged(nameof(IsPreviewing));
	}

	/// <summary>
	/// Clears the on-screen preview (frame image and pause underlay). Native hosts unbind separately.
	/// </summary>
	public void ClearPreviewDisplay()
	{
		if (CameraPreview != null)
		{
			CameraPreview.Source = null;
			CameraPreview.InvalidateVisual();
		}

		if (NativeHost != null)
		{
			NativeHost.IsPaused = false;
		}

		InvalidateVisual();
	}

	public async Task StopRecordingAsync()
	{
		if (!CameraAdapter.IsRecording)
		{
			return;
		}

		await CameraAdapter.StopRecordingAsync();
	}

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		if (CameraAdapter == null)
		{
			CameraAdapter = GetInstance<ICameraAdapter>();
			WeakEventManager.AddPropertyChanged(CameraAdapter, this, AdapterOnPropertyChanged);
		}
		base.OnAttachedToLogicalTree(e);
	}

	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromLogicalTree(e);

		CameraAdapter?.Dispose();
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if ((change.Property == ModeProperty)
			&& (change.NewValue != null))
		{
			CameraAdapter.Mode = (CameraMode) change.NewValue;
		}
		else if ((change.Property == IsPausedProperty) && (NativeHost != null))
		{
			NativeHost.IsPaused = change.GetNewValue<bool>();
		}

		base.OnPropertyChanged(change);
	}

	private void AdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(CameraAdapter.Frame):
			{
				// Do not RequestWarmUnderlay every frame — snapshot capture kills preview FPS.
				if (CameraPreview.Source != CameraAdapter.Frame)
				{
					CameraPreview.Source = CameraAdapter.Frame;
				}

				CameraPreview.InvalidateVisual();
				break;
			}
			case nameof(CameraAdapter.IsPreviewing):
			{
				if (!CameraAdapter.IsPreviewing)
				{
					ClearPreviewDisplay();
				}

				OnPropertyChanged(e.PropertyName);
				break;
			}
			case nameof(CameraAdapter.CapturedData):
			case nameof(CameraAdapter.IsRecording):
			case nameof(CameraAdapter.Mode):
			{
				OnPropertyChanged(e.PropertyName);
				break;
			}
		}
	}

	#endregion
}