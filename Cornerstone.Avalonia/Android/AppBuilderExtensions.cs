#region References

using Avalonia;
using Cornerstone.Avalonia.AvaloniaWebView;

#endregion

namespace Cornerstone.Avalonia.Android;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstoneAndroid(this AppBuilder builder)
	{
		return builder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = CornerstoneApplication.DependencyProvider;
			dependencyProvider.AddTransient<IWebViewAdapter, WebViewAdapter>();
		});
	}

	#endregion
}