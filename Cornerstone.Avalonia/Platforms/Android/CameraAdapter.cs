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
	#region Constructors

	public CameraAdapter(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public override Task StartPreviewAsync()
	{
		var id = AndroidPlatform.GetRequestId();
		AndroidPlatform.ApplicationContext.StartActivity(CreateMediaIntent(id));
		return Task.CompletedTask;
	}

	public override Task StartRecordingAsync(string outputFile)
	{
		var id = AndroidPlatform.GetRequestId();
		AndroidPlatform.ApplicationContext.StartActivity(CreateMediaIntent(id));
		return Task.CompletedTask;
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