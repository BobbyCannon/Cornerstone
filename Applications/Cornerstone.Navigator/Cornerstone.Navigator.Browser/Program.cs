#region References

using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Browser;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Navigator.Browser;

internal sealed class Program
{
	#region Methods

	public static AppBuilder BuildCornerstoneApp()
	{
		return AppBuilder.Configure<App>();
	}

	private static async Task Main(string[] args)
	{
		AppBootstrap.Initialize("Cornerstone.Navigator", typeof(Program).Assembly, args);
		await BuildCornerstoneApp()
			.UseCornerstone(args)
			.StartBrowserAppAsync("out");
	}

	#endregion
}