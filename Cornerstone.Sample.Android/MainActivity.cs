#region References

using Android.App;
using Android.Content.PM;
using Android.Renderscripts;
using Avalonia.Android;
using Cornerstone.Avalonia;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Android;

[Activity(
	Label = "Sample",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	ConfigurationChanges =
		ConfigChanges.Orientation
		| ConfigChanges.ScreenSize
		| ConfigChanges.UiMode
		| ConfigChanges.Keyboard)]
public class MainActivity : AvaloniaMainActivity
{
	#region Constructors

	public MainActivity()
	{
		CornerstoneApplication.RuntimeInformation.Initialize(typeof(Program).Assembly);
		CornerstoneApplication.RuntimeInformation.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "Cornerstone.Sample");
	}

	#endregion
}