#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone;
using Cornerstone.Runtime;
using Bitmap = Cornerstone.Presentation.Media.Imaging.Bitmap;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Camera;

#endregion

namespace Cornerstone.Presentation.Controls;

[TemplatePart(PartCameraPreview, typeof(Image))]
[TemplatePart(PartNativeHost, typeof(CameraNativeHost))]
public class CameraView : TemplatedControl
{
	#region Fields

	public const string PartCameraPreview = "PART_CameraPreview";
	public const string PartNativeHost = "PART_NativeHost";

	public static readonly StyledProperty<CameraMode> ModeProperty;

	private Image _cameraPreview;
	private CameraNativeHost _nativeHost;

	#endregion

	#region Constructors

	public CameraView()
	{
		_cameraPreview = null;
		_nativeHost = null;
		CameraAdapter = AppBootstrap.GetInstance<ICameraAdapter>();
		WeakEventManager.AddPropertyChanged(CameraAdapter, this, AdapterOnPropertyChanged);
	}

	static CameraView()
	{
		ModeProperty = PresentationProperty.Register<CameraView, CameraMode>(nameof(Mode), CameraMode.Video);
	}

	#endregion

	#region Properties

	public IPresentationList<CameraMode> AvailableModes => CameraAdapter.AvailableModes;

	public ICameraAdapter CameraAdapter { get; set; }

	public byte[] CapturedData => CameraAdapter.CapturedData;

	public Bitmap Frame => CameraAdapter.Frame;

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

		outputPath ??= System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Camera_{Guid.NewGuid()}.mp4");
		await CameraAdapter.StartRecordingAsync(outputPath);
	}

	public async Task StopAsync()
	{
		if (CameraAdapter.IsRecording)
		{
			await CameraAdapter.StopRecordingAsync();
		}

		await CameraAdapter.StopPreviewAsync();
		// Clear Cornerstone Image; stopped cover is bound to !IsPreviewing in XAML.
		ClearPreviewDisplay();
		// Ensure bindings refresh even if a late frame notification races with stop.
		OnPropertyChanged(nameof(IsPreviewing));
	}

	/// <summary>
	/// Clears the on-screen preview image. Native hosts unbind separately.
	/// </summary>
	public void ClearPreviewDisplay()
	{
		if (_cameraPreview != null)
		{
			_cameraPreview.Source = null;
			_cameraPreview.InvalidateVisual();
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

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		_cameraPreview = e.NameScope.Find<Image>(PartCameraPreview);
		_nativeHost = e.NameScope.Find<CameraNativeHost>(PartNativeHost);

		if ((_cameraPreview != null) && (CameraAdapter != null))
		{
			_cameraPreview.Source = CameraAdapter.Frame;
		}
	}

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		if (CameraAdapter == null)
		{
			CameraAdapter = AppBootstrap.GetInstance<ICameraAdapter>();
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
	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if ((change.Property == ModeProperty)
			&& (change.NewValue != null))
		{
			CameraAdapter.Mode = (CameraMode) change.NewValue;
		}

		base.OnPropertyChanged(change);
	}

	private void AdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(CameraAdapter.Frame):
			{
				if (_cameraPreview == null)
				{
					break;
				}

				if (_cameraPreview.Source != CameraAdapter.Frame)
				{
					_cameraPreview.Source = CameraAdapter.Frame;
				}

				_cameraPreview.InvalidateVisual();
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
