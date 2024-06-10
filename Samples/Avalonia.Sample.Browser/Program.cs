#region References

using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Browser;

#endregion

[assembly: SupportedOSPlatform("browser")]

namespace Avalonia.Sample.Browser;

internal class Program
{
	#region Methods

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>();
	}

	private static async Task Main(string[] args)
	{
		await BuildAvaloniaApp()
			.WithInterFont()
			.StartBrowserAppAsync("out");
	}

	#endregion
}