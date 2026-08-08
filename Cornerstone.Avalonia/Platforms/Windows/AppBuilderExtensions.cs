#region References

using Avalonia;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

internal static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstone(AppBuilder builder, string[] args)
	{
		return builder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = AppBootstrap.DependencyProvider;
			dependencyProvider.SetTransient<IWebViewAdapter, WebView2Adapter>();
		});
	}

	#endregion
}