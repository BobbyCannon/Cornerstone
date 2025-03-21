#region References

using Avalonia;
using Cornerstone.Avalonia.WebView;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Avalonia.Platforms.Browser;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstone(this AppBuilder builder)
	{
		return builder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = CornerstoneApplication.DependencyProvider;
			dependencyProvider.AddOrUpdateTransient<BrowserInteropProxy, CornerstoneBrowserInteropProxy>();
			dependencyProvider.AddOrUpdateTransient<IWebViewAdapter, WebViewAdapter>();
		});
	}

	#endregion
}