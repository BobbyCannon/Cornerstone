#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Camera;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Media;

/// <summary>
/// Sample for CameraView: start/stop preview, start/stop record, and mode.
/// </summary>
[SourceReflection]
public partial class TabCamera : UserControl
{
	#region Constants

	public const string HeaderName = "Camera";

	#endregion

	#region Fields

	private string _lastRecordingPath;

	#endregion

	#region Constructors

	public TabCamera() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	[DependencyInjectionConstructor]
	public TabCamera(AppViewModel viewModel)
	{
		StatusText = "Stopped";
		ViewModel = viewModel;

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial string StatusText { get; set; }

	public AppViewModel ViewModel { get; }

	#endregion

	#region Methods

	[RelayCommand]
	public async Task StartPreview()
	{
		try
		{
			StatusText = "Starting preview…";
			await Camera.StartAsync();
			RefreshStatus();
		}
		catch (Exception ex)
		{
			StatusText = $"Preview failed: {ex.Message}";
		}
	}

	[RelayCommand]
	public async Task StartRecord()
	{
		try
		{
			StatusText = "Starting record…";
			_lastRecordingPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Camera_{Guid.NewGuid()}.mp4");
			await Camera.StartRecordingAsync(_lastRecordingPath);
			RefreshStatus();
		}
		catch (Exception ex)
		{
			StatusText = $"Record failed: {ex.Message}";
		}
	}

	[RelayCommand]
	public async Task StopPreview()
	{
		try
		{
			await Camera.StopAsync();
			StatusText = "Stopped";
		}
		catch (Exception ex)
		{
			StatusText = $"Stop preview failed: {ex.Message}";
		}
	}

	[RelayCommand]
	public async Task StopRecord()
	{
		try
		{
			await Camera.StopRecordingAsync();
			if (!string.IsNullOrEmpty(_lastRecordingPath) && File.Exists(_lastRecordingPath))
			{
				StatusText = $"Recorded: {_lastRecordingPath}";
			}
			else
			{
				RefreshStatus();
			}
		}
		catch (Exception ex)
		{
			StatusText = $"Stop record failed: {ex.Message}";
		}
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (Camera.CameraAdapter != null)
		{
			WeakEventManager.AddPropertyChanged<ICameraAdapter, TabCamera>(Camera.CameraAdapter, this, AdapterOnPropertyChanged);
		}

		base.OnAttachedToVisualTree(e);
		RefreshStatus();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_ = Camera.StopAsync();
		base.OnDetachedFromVisualTree(e);
	}

	private void AdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ICameraAdapter.IsPreviewing):
			case nameof(ICameraAdapter.IsRecording):
			case nameof(ICameraAdapter.Mode):
			{
				RefreshStatus();
				break;
			}
		}
	}

	private void RefreshStatus()
	{
		if (Camera.IsRecording)
		{
			StatusText = string.IsNullOrEmpty(_lastRecordingPath)
				? $"Recording ({Camera.Mode})"
				: $"Recording ({Camera.Mode}) → {_lastRecordingPath}";
			return;
		}

		if (Camera.IsPreviewing)
		{
			StatusText = $"Previewing ({Camera.Mode})";
			return;
		}

		if (StatusText.StartsWith("Preview failed", StringComparison.Ordinal)
			|| StatusText.StartsWith("Record failed", StringComparison.Ordinal)
			|| StatusText.StartsWith("Stop ", StringComparison.Ordinal)
			|| StatusText.StartsWith("Recorded:", StringComparison.Ordinal))
		{
			return;
		}

		StatusText = "Stopped";
	}

	#endregion
}