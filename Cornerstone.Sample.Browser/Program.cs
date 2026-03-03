#region References

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;

#endregion

namespace Cornerstone.Sample.Browser;

internal sealed class Program
{
	#region Methods

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>();
	}

	private static Task Main(string[] args)
	{
		return BuildAvaloniaApp().StartBrowserAppAsync("out");
	}

	#endregion
}