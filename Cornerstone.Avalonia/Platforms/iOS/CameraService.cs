#region References

using Cornerstone.Avalonia.Camera;
using Cornerstone.Presentation;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.Avalonia.Platforms.iOS;

internal class CameraAdapter : BaseCameraAdapter
{
	#region Constructors

	public CameraAdapter(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public override Task StartRecordingAsync(string outputFile)
	{
		return Task.CompletedTask;
	}

	#endregion
}