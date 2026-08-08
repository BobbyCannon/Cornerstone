#region References

using Android.App;
using Android.Content.PM;
using Avalonia.Android;

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
}