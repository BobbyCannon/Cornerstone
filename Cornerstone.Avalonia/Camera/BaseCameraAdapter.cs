#region References

using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.Camera;

internal abstract class BaseCameraAdapter : Bindable, ICameraAdapter
{
	#region Constructors

	protected BaseCameraAdapter(IDispatcher dispatcher) : base(dispatcher)
	{
		IsRecording = false;
	}

	#endregion

	#region Properties

	public Bitmap Frame { get; protected set; }

	public bool IsPreviewing { get; protected set; }

	public bool IsRecording { get; protected set; }

	#endregion

	#region Methods

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
		return Task.CompletedTask;
	}

	public virtual Task StopRecordingAsync()
	{
		return Task.CompletedTask;
	}

	#endregion
}

public interface ICameraAdapter : INotifyPropertyChanged
{
	#region Properties

	Bitmap Frame { get; }

	bool IsPreviewing { get; }

	bool IsRecording { get; }

	#endregion

	#region Methods

	Task StartPreviewAsync();

	Task StartRecordingAsync(string outputFile);

	Task StopPreviewAsync();

	Task StopRecordingAsync();

	#endregion
}