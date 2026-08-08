#region References

using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Cornerstone.Avalonia.Platforms;
using Cornerstone.Runtime;
using SQLitePCL;
using System;

#endregion

namespace Cornerstone.Sample.Android;

[Application]
public class Application : AvaloniaAndroidApplication<App>
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
			.UseAndroid()
			.UseCornerstone([]);
	}

	#endregion
}