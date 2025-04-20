#region References

using Android.Content;
using Android.Provider;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Platforms.Android;
using Cornerstone.Presentation;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android;

internal class CameraAdapter : BaseCameraAdapter
{
	#region Fields

	private int _currentRequest;

	#endregion

	#region Constructors

	public CameraAdapter(IDispatcher dispatcher) : base(dispatcher)
	{
		CameraActivity.Adapters.Add(this);
	}

	#endregion

	#region Methods

	public override Task StartPreviewAsync()
	{
		_currentRequest = AndroidPlatform.GetRequestId();
		AndroidPlatform.ApplicationContext.StartActivity(CreateMediaIntent(_currentRequest));
		return Task.CompletedTask;
	}

	public override Task StartRecordingAsync(string outputFile)
	{
		_currentRequest = AndroidPlatform.GetRequestId();
		AndroidPlatform.ApplicationContext.StartActivity(CreateMediaIntent(_currentRequest));
		return Task.CompletedTask;
	}

	protected override void Dispose(bool disposing)
	{
		CameraActivity.Adapters.Remove(this);
		base.Dispose(disposing);
	}

	internal void SetVideoData(int request, byte[] data)
	{
		if (request == _currentRequest)
		{
			base.FrameData = data;
		}
	}

	private Intent CreateMediaIntent(int id)
	{
		var response = new Intent(AndroidPlatform.ApplicationContext, typeof(CameraActivity));

		response.SetType("video/*");
		response.SetFlags(ActivityFlags.NewTask);

		response.PutExtra("id", id);
		response.PutExtra("type", "video/*");
		response.PutExtra("action", MediaStore.ActionVideoCapture);
		response.PutExtra("tasked", true);

		response.PutExtra(MediaStore.IMediaColumns.Title, "camera.mp4");

		return response;
	}

	#endregion
}