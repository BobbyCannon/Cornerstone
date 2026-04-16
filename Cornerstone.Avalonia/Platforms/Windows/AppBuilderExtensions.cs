#region References

using Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstone(this AppBuilder builder)
	{
		GlobalAppBuilder.UseCornerstone(builder);

		return builder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = CornerstoneApplication.DependencyProvider;
			dependencyProvider.SetTransient<IWebViewAdapter, WebView2Adapter>();
		});
	}

	#endregion
}