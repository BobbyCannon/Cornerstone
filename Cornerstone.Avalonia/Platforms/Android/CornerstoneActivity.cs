#region References

using System;
using Android.Content;
using Avalonia;
using Avalonia.Android;
using Cornerstone.Platforms.Android;
using Cornerstone.Runtime;
using Permission = Android.Content.PM.Permission;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android;

public class CornerstoneActivity<TApplication> : AvaloniaMainActivity<TApplication>, IDependencyProvider
	where TApplication : Application, new()
{
	#region Fields

	private readonly DependencyProvider _dp;

	#endregion

	#region Constructors

	public CornerstoneActivity()
	{
		_dp = CornerstoneApplication.DependencyProvider;
	}

	#endregion

	#region Methods

	public T GetInstance<T>()
	{
		return _dp.GetInstance<T>();
	}

	public object GetInstance(Type type)
	{
		return _dp.GetInstance(type);
	}

	public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
	{
		base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		if (_dp.GetInstance<IPermissions>() is AndroidPermissions androidPermissions)
		{
			androidPermissions.OnRequestPermissionResult(requestCode, permissions, grantResults);
		}
	}

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		// https://github.com/dotnet/efcore/issues/32346
		AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);
		return base.CustomizeAppBuilder(builder);
	}

	/// <inheritdoc />
	protected override void OnNewIntent(Intent intent)
	{
		AndroidPlatform.OnNewIntent(intent);
		base.OnNewIntent(intent);
	}

	#endregion
}