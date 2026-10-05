#region References

using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Browser;

#endregion

namespace CornerstoneApplication.Browser;

internal sealed partial class Program
{
	#region Methods

	public static AppBuilder BuildCornerstoneApp()
	{
		return AppBuilder.Configure<App>();
	}

	private static Task Main(string[] args)
	{
		return BuildCornerstoneApp()
			.StartBrowserAppAsync("out");
	}

	#endregion
}
