#region References

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Cornerstone.Presentation.Android;
using Cornerstone.Presentation.Platforms.Android;

#endregion

namespace Company.AppName.Android;

[Activity(
	Label = "Company.AppName",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/Icon",
	MainLauncher = true,
	ConfigurationChanges =
		ConfigChanges.Orientation
		| ConfigChanges.ScreenSize
		| ConfigChanges.UiMode,
	WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : CornerstoneMainActivity
{
	#region Methods

	protected override void OnCreate(Bundle savedInstanceState)
	{
		AndroidHost.Initialize(this);
		base.OnCreate(savedInstanceState);
	}

	#endregion
}
