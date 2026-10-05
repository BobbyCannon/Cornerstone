#region References

using Android.App;
using Android.Runtime;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Android;
using Cornerstone.Esri;
using Cornerstone.Vlc;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;
using Cornerstone.Sample;
using SQLitePCL;
using System;

#endregion

namespace Cornerstone.Sample.Android;

[Application]
public class Application : CornerstoneAndroidApplication<App>
{
	#region Constructors

	protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
	{
		AppBootstrap.Initialize("Cornerstone.Sample", typeof(Application).Assembly);
		Batteries.Init();
	}

	#endregion

	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		// https://github.com/dotnet/efcore/issues/32346
		AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);
		return base.CustomizeAppBuilder(builder)
			.With(new AndroidPlatformOptions { NativeBehindComposition = SampleNativeAirspace.IsEnabled })
			.UseAndroid()
			.UseCornerstone([])
			.UseCornerstoneEsri()
			.UseCornerstoneVlc();
	}

	#endregion
}