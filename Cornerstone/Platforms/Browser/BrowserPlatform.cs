﻿#region References

using Cornerstone.Input;
using Cornerstone.Location;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Browser;

public static class BrowserPlatform
{
	#region Properties

	public static DependencyProvider DependencyProvider { get; private set; }

	#endregion

	#region Methods

	public static void Initialize(DependencyProvider dependencyProvider)
	{
		DependencyProvider = dependencyProvider;

		RuntimeInformation.SetPlatformOverride(x => x.DevicePlatform, DevicePlatform.Browser);

		AddPlatformImplementations();
	}

	private static void AddPlatformImplementations()
	{
		DependencyProvider.AddSingleton<Gamepad, BrowserGamepad>();
		DependencyProvider.AddSingleton<ILocationProvider, BrowserLocationProvider>();
		DependencyProvider.AddSingleton<IPermissions, BrowserPermissions>();
	}

	#endregion
}