#region References

using Android.App;
using Android.Content.PM;
using Android.OS;
using Cornerstone.Maui;
using Cornerstone.Maui.Platforms.Android;
using Microsoft.Maui;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

#endregion

namespace Sample.Client.Maui.Platforms.Android;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiActivity
{
	#region Methods

	/// <inheritdoc />
	protected override void OnCreate(Bundle savedInstanceState)
	{
		Microsoft.Maui.Controls.Application.Current?
			.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>()
			.UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Pan);

		base.OnCreate(savedInstanceState);
	}

	#endregion
}