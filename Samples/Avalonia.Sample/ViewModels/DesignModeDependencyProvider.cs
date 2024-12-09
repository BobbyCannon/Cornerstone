#region References

using System;
using Avalonia.Controls;
using Cornerstone;
using Cornerstone.Avalonia;
using Cornerstone.Location;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security.Vault;

#endregion

namespace Avalonia.Sample.ViewModels;

internal static class DesignModeDependencyProvider
{
	#region Constructors

	static DesignModeDependencyProvider()
	{
		if (!Design.IsDesignMode)
		{
			throw new Exception("Do not use except in design mode.");
		}

		Dependencies = new DependencyProvider("Sample Design Time");
		RuntimeInformation = new RuntimeInformation();
		LocationProvider = new LocationProvider();
		LocationProvider.CurrentValue.HorizontalLocation.Latitude = 33.751599;
		LocationProvider.CurrentValue.HorizontalLocation.Longitude = -84.390641;

		Dependencies.AddSingleton<IDispatcher>(() => null);
		Dependencies.AddSingleton<ILocationProvider>(() => LocationProvider);
		Dependencies.AddSingleton<IRuntimeInformation, RuntimeInformation>(() => RuntimeInformation);
		Dependencies.AddSingleton<IClipboardService, ClipboardService>();
		Dependencies.AddSingleton<IWindowsHelloService, WindowsHelloServiceDummy>();
		Dependencies.AddSingleton(DateTimeProvider.RealTime);

		Dependencies.AddSingleton<ApplicationSettings>();
		Dependencies.AddSingleton<MainViewModel>();
	}

	#endregion

	#region Properties

	public static DependencyProvider Dependencies { get; }

	public static LocationProvider LocationProvider { get; }

	public static RuntimeInformation RuntimeInformation { get; set; }

	#endregion

	#region Methods

	public static T Get<T>()
	{
		return Dependencies.GetInstance<T>();
	}

	#endregion
}