#region References

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Cornerstone.Avalonia;
using Cornerstone.Location;

#endregion

namespace Cornerstone.Sample.Browser;

internal class Program
{
	#region Methods

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>();
	}

	private static async Task Main(string[] args)
	{
		var locationProvider = new LocationProvider();
		var dependencyProvider = CornerstoneApplication.DependencyProvider;

		dependencyProvider.AddSingleton<ILocationProvider>(() => locationProvider);

		await BuildAvaloniaApp()
			.WithInterFont()
			.StartBrowserAppAsync("out");
	}

	#endregion
}