#region References

using System;
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

	public virtual byte[] FrameData { get; protected set; }

	public bool IsPreviewing { get; protected set; }

	public bool IsRecording { get; protected set; }

	#endregion

	#region Methods

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
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
		return Task.CompletedTask;
	}

	public virtual Task StopRecordingAsync()
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
	}

	#endregion
}

public interface ICameraAdapter : INotifyPropertyChanged, IDisposable
{
	#region Properties

	Bitmap Frame { get; }

	byte[] FrameData { get; }

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