#region References

using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Runtime;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

#endregion

namespace Cornerstone.Avalonia.Camera;

public partial class CameraView : CornerstoneUserControl
{
	#region Fields

	private readonly ICameraAdapter _cameraAdapter;
	private readonly IRuntimeInformation _runtimeInformation;

	#endregion

	#region Constructors

	public CameraView()
	{
		_cameraAdapter = GetInstance<ICameraAdapter>();
		_runtimeInformation = GetInstance<IRuntimeInformation>();

		WeakEventManager.AddPropertyChanged(_cameraAdapter, this, AdapterOnPropertyChanged);

		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public Bitmap Frame => _cameraAdapter.Frame;

	public bool IsPreviewing => _cameraAdapter.IsPreviewing;

	public bool IsRecording => _cameraAdapter.IsRecording;

	#endregion

	#region Methods

	public Task StartAsync()
	{
		return _cameraAdapter.StartPreviewAsync();
	}

	public Task StopAsync()
	{
		return _cameraAdapter.StopPreviewAsync();
	}

	private void AdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(Frame):
			{
				if (CameraPreview.Source != _cameraAdapter.Frame)
				{
					CameraPreview.Source = _cameraAdapter.Frame;
				}
				CameraPreview.InvalidateVisual();
				break;
			}
			case nameof(IsPreviewing):
			{
				OnPropertyChanged(e.PropertyName);
				break;
			}
			case nameof(IsRecording):
			{
				RecordButton.Foreground = IsRecording
					? ResourceService.GetColorAsBrush("Red06")
					: ResourceService.GetColorAsBrush("Foreground02");
				OnPropertyChanged(e.PropertyName);
				break;
			}
		}
	}

	private void RecordButtonOnClick(object sender, RoutedEventArgs e)
	{
		if (_cameraAdapter.IsRecording)
		{
			_cameraAdapter.StopRecordingAsync();
			return;
		}

		var outputPath = Path.Combine(_runtimeInformation.ApplicationDataLocation, "camera.mp4");
		_cameraAdapter.StartRecordingAsync(outputPath);
	}

	#endregion
}