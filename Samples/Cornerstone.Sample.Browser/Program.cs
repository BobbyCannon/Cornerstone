#region References

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Platforms.Browser;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Platforms.Browser;

#endregion

namespace Cornerstone.Sample.Browser;

internal class Program
{
	#region Methods

	private static async Task Main(string[] args)
	{
		var dependencyProvider = CornerstoneApplication.DependencyProvider;

		BrowserPlatform.Initialize(dependencyProvider);
		ApplicationLifecycleExtensions.SetBrowserArgs(args);

		await AppBuilder
			.Configure<App>()
			.WithInterFont()
			.UseCornerstone()
			.StartBrowserAppAsync("out");
	}

	#endregion
}