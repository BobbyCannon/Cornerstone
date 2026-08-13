#region References

using System;
using System.Linq;
using System.Web;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Rendering;
using Avalonia.Threading;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.MediaPlayer;
using Cornerstone.Platforms.Browser;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia.Platforms.Browser;

internal static class AppBuilderExtensions
{
	#region Methods

	/// <summary>
	/// Ex.
	/// ?DebugOverlays=Fps, DirtyRects, LayoutTimeGraph, RenderTimeGraph
	/// </summary>
	public static RendererDebugOverlays ParseBrowserPlatformOptions(string[] args, out BrowserPlatformOptions options)
	{
		options = new BrowserPlatformOptions();
		var overlays = RendererDebugOverlays.None;

		try
		{
			if ((args.Length == 0)
				|| !Uri.TryCreate(args[0], UriKind.Absolute, out var uri)
				|| (uri.Query.Length <= 1))
			{
				return overlays;
			}

			var queryParams = HttpUtility.ParseQueryString(uri.Query);

			if (bool.TryParse(queryParams[nameof(options.PreferFileDialogPolyfill)], out var preferDialogsPolyfill))
			{
				options.PreferFileDialogPolyfill = preferDialogsPolyfill;
			}

			if (bool.TryParse(queryParams[nameof(options.PreferManagedThreadDispatcher)], out var preferManagedThreadDispatcher))
			{
				options.PreferManagedThreadDispatcher = preferManagedThreadDispatcher;
			}

			if (queryParams[nameof(options.RenderingMode)] is { } renderingModePairs)
			{
				options.RenderingMode = renderingModePairs
					.Split(';', StringSplitOptions.RemoveEmptyEntries)
					.Select(entry => Enum.Parse<BrowserRenderingMode>(entry, true))
					.ToArray();
			}

			Enum.TryParse(queryParams[nameof(RendererDiagnostics.DebugOverlays)], out overlays);

			//Console.WriteLine("DebugOverlays: " + overlays);
			//Console.WriteLine("PreferFileDialogPolyfill: " + options.PreferFileDialogPolyfill);
			//Console.WriteLine("PreferManagedThreadDispatcher: " + options.PreferManagedThreadDispatcher);
			//Console.WriteLine("RenderingMode: " + string.Join(";", options.RenderingMode));

			return overlays;
		}
		catch (Exception ex)
		{
			Console.WriteLine("ParseArgs of BrowserPlatformOptions failed: " + ex);
			return overlays;
		}
	}

	public static AppBuilder UseCornerstone<T>(AppBuilder builder, string[] args, out T options) where T : class
	{
		var overlays = ParseBrowserPlatformOptions(args, out var platformOptions);
		options = platformOptions as T;

		// Avalonia (11.x) has issues with responsiveness with WASM MT
		// This will probably be fixed eventually
		// An alternative is to run a small infinite animation
		//options.PreferManagedThreadDispatcher = false;

		var dependencyProvider = AppBootstrap.DependencyProvider;
		dependencyProvider.SetTransient<BrowserInteropProxy, CornerstoneBrowserInteropProxy>();
		// Factory avoids SourceReflector constructor discovery (can fail for internal types on WASM).
		dependencyProvider.SetTransient<IWebViewAdapter, WebViewAdapter>(() => new WebViewAdapter());
		dependencyProvider.SetTransient<ICameraAdapter, CameraAdapterStub>(() => new CameraAdapterStub(CornerstoneApplication.CornerstoneDispatcher));
		dependencyProvider.SetTransient<BaseMediaPlayerAdapter, MediaPlayerAdapterStub>(() => new MediaPlayerAdapterStub());

		return builder
			.AfterSetup(_ =>
			{
				Dispatcher.UIThread.InvokeAsync(
					() =>
					{
						if (Application.Current!.ApplicationLifetime is ISingleViewApplicationLifetime lifetime
							&& (overlays != default))
						{
							TopLevel.GetTopLevel(lifetime.MainView)!.RendererDiagnostics.DebugOverlays = overlays;
						}
					},
					DispatcherPriority.Background
				);
			});
	}

	#endregion
}