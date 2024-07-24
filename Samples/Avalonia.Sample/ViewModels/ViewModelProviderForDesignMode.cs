#region References

using Cornerstone;
using Cornerstone.Avalonia;
using Cornerstone.Location;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security.Vault;

#endregion

namespace Avalonia.Sample.ViewModels;

internal static class ViewModelProviderForDesignMode
{
	#region Constructors

	static ViewModelProviderForDesignMode()
	{
		Dependencies = new DependencyInjector();
		RuntimeInformation = new RuntimeInformation();
		LocationProvider = new LocationProvider();
		LocationProvider.CurrentValue.HorizontalLocation.Latitude = 33.751599;
		LocationProvider.CurrentValue.HorizontalLocation.Longitude = -84.390641;

		Dependencies.AddSingleton<IDispatcher>(() => null);
		Dependencies.AddSingleton<ILocationProvider>(() => LocationProvider);
		Dependencies.AddSingleton<IRuntimeInformation, RuntimeInformation>(() => RuntimeInformation);
		Dependencies.AddSingleton<IClipboardService, ClipboardService>();
		Dependencies.AddSingleton<IWindowsHelloService, WindowsHelloServiceDummy>();

		Dependencies.AddSingleton<ApplicationSettings>();
		Dependencies.AddSingleton<MainViewModel>();
	}

	#endregion

	#region Properties

	public static DependencyInjector Dependencies { get; }

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